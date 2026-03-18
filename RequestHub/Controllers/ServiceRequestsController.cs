using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Application.DTOs;
using RequestHub.Application.Services;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ServiceRequestsController(AppDbContext dbContext, ICurrentUserAccessor currentUser, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List([FromQuery] ServiceRequestFilterDto filter)
    {
        var query = dbContext.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .AsQueryable();

        query = ApplyVisibilityScope(query);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.AreaId.HasValue)
            query = query.Where(x => x.AreaId == filter.AreaId.Value);

        if (filter.PriorityId.HasValue)
            query = query.Where(x => x.PriorityId == filter.PriorityId.Value);

        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= filter.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Number.Contains(search) || x.Subject.Contains(search));
        }

        var requests = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        var result = requests.Select(x => new
        {
            id = x.Id,
            number = x.Number,
            subject = x.Subject,
            description = x.Description,
            status = x.Status.ToString(),
            statusId = (int)x.Status,
            statusName = x.Status.ToString(),
            rejectionReason = x.RejectionReason,
            createdAtUtc = x.CreatedAtUtc,
            areaId = x.AreaId,
            requestTypeId = x.RequestTypeId,
            priorityId = x.PriorityId,
            area = x.Area != null ? x.Area.Name : null,
            priority = x.Priority != null ? x.Priority.Name : null,
            requestType = x.RequestType != null ? x.RequestType.Name : null,
            assignedToUserId = x.AssignedToUserId,
            createdByUserId = x.CreatedByUserId,
            attachmentPath = x.AttachmentPath,
            attachmentName = x.AttachmentName,
            canEdit = CanEditRequest(x),
            canDelete = CanDeleteRequest(x),
            canTake = CanTakeRequest(x),
            canAssign = CanAssignRequest(x),
            canChangeStatus = CanChangeStatus(x),
            canComment = CanComment(x),
            canClose = CanCloseRequest(x)
        });

        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> Create([FromForm] CreateServiceRequestDto dto, IFormFile? attachment)
    {
        if (dto.AreaId <= 0)
            return BadRequest(new { message = "Área inválida." });

        if (dto.RequestTypeId <= 0)
            return BadRequest(new { message = "Tipo de solicitud inválido." });

        if (dto.PriorityId <= 0)
            return BadRequest(new { message = "Prioridad inválida." });

        if (string.IsNullOrWhiteSpace(dto.Subject))
            return BadRequest(new { message = "El asunto es requerido." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "La descripción es requerida." });

        var areaExists = await dbContext.Areas.AnyAsync(x => x.Id == dto.AreaId);
        if (!areaExists)
            return BadRequest(new { message = "Área inválida." });

        var typeExists = await dbContext.RequestTypes.AnyAsync(x => x.Id == dto.RequestTypeId && x.AreaId == dto.AreaId);
        if (!typeExists)
            return BadRequest(new { message = "Tipo de solicitud inválido para el área seleccionada." });

        var priorityExists = await dbContext.Priorities.AnyAsync(x => x.Id == dto.PriorityId);
        if (!priorityExists)
            return BadRequest(new { message = "Prioridad inválida." });

        var uploaded = await SaveAttachmentAsync(attachment);

        var request = new ServiceRequest
        {
            Number = BuildTemporaryRequestNumber(),
            AreaId = dto.AreaId,
            RequestTypeId = dto.RequestTypeId,
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            PriorityId = dto.PriorityId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = currentUser.UserId,
            Status = TicketStatus.Nueva,
            RejectionReason = null,
            AttachmentPath = uploaded.Path,
            AttachmentName = uploaded.Name,
            AssignedToUserId = null
        };

        dbContext.ServiceRequests.Add(request);
        await dbContext.SaveChangesAsync();

        request.Number = BuildFinalRequestNumber(request.CreatedAtUtc, request.Id);
        await dbContext.SaveChangesAsync();

        await dbContext.Entry(request).Reference(x => x.Area).LoadAsync();
        await dbContext.Entry(request).Reference(x => x.Priority).LoadAsync();
        await dbContext.Entry(request).Reference(x => x.RequestType).LoadAsync();

        return CreatedAtAction(nameof(Detail), new { id = request.Id }, new
        {
            id = request.Id,
            number = request.Number,
            subject = request.Subject,
            description = request.Description,
            status = request.Status.ToString(),
            statusId = (int)request.Status,
            statusName = request.Status.ToString(),
            rejectionReason = request.RejectionReason,
            createdAtUtc = request.CreatedAtUtc,
            areaId = request.AreaId,
            requestTypeId = request.RequestTypeId,
            priorityId = request.PriorityId,
            area = request.Area?.Name,
            priority = request.Priority?.Name,
            requestType = request.RequestType?.Name,
            assignedToUserId = request.AssignedToUserId,
            createdByUserId = request.CreatedByUserId,
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            canEdit = CanEditRequest(request),
            canDelete = CanDeleteRequest(request),
            canTake = CanTakeRequest(request),
            canAssign = CanAssignRequest(request),
            canChangeStatus = CanChangeStatus(request),
            canComment = CanComment(request),
            canClose = CanCloseRequest(request)
        });
    }

    [HttpPost("{id:int}/take")]
    public async Task<ActionResult> Take(int id)
    {
        var request = await dbContext.ServiceRequests
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanTakeRequest(request))
            return Forbid();

        request.AssignedToUserId = currentUser.UserId;

        if (IsNewStatus(request.Status))
            request.Status = TicketStatus.EnProceso;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            id = request.Id,
            number = request.Number,
            subject = request.Subject,
            description = request.Description,
            status = request.Status.ToString(),
            statusId = (int)request.Status,
            statusName = request.Status.ToString(),
            rejectionReason = request.RejectionReason,
            createdAtUtc = request.CreatedAtUtc,
            areaId = request.AreaId,
            requestTypeId = request.RequestTypeId,
            priorityId = request.PriorityId,
            area = request.Area?.Name,
            priority = request.Priority?.Name,
            requestType = request.RequestType?.Name,
            assignedToUserId = request.AssignedToUserId,
            createdByUserId = request.CreatedByUserId,
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            canEdit = CanEditRequest(request),
            canDelete = CanDeleteRequest(request),
            canTake = CanTakeRequest(request),
            canAssign = CanAssignRequest(request),
            canChangeStatus = CanChangeStatus(request),
            canComment = CanComment(request),
            canClose = CanCloseRequest(request)
        });
    }

    [HttpPut("{id:int}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> Update(int id, [FromForm] UpdateServiceRequestDto dto, IFormFile? attachment)
    {
        var request = await dbContext.ServiceRequests
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanEditRequest(request))
            return Forbid();

        if (dto.AreaId <= 0)
            return BadRequest(new { message = "Área inválida." });

        if (dto.RequestTypeId <= 0)
            return BadRequest(new { message = "Tipo de solicitud inválido." });

        if (dto.PriorityId <= 0)
            return BadRequest(new { message = "Prioridad inválida." });

        if (string.IsNullOrWhiteSpace(dto.Subject))
            return BadRequest(new { message = "El asunto es requerido." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "La descripción es requerida." });

        var areaExists = await dbContext.Areas.AnyAsync(x => x.Id == dto.AreaId);
        if (!areaExists)
            return BadRequest(new { message = "Área inválida." });

        var typeExists = await dbContext.RequestTypes.AnyAsync(x => x.Id == dto.RequestTypeId && x.AreaId == dto.AreaId);
        if (!typeExists)
            return BadRequest(new { message = "Tipo de solicitud inválido para el área seleccionada." });

        var priorityExists = await dbContext.Priorities.AnyAsync(x => x.Id == dto.PriorityId);
        if (!priorityExists)
            return BadRequest(new { message = "Prioridad inválida." });

        request.AreaId = dto.AreaId;
        request.RequestTypeId = dto.RequestTypeId;
        request.Subject = dto.Subject.Trim();
        request.Description = dto.Description.Trim();
        request.PriorityId = dto.PriorityId;

        if (CanChangeStatus(request) && Enum.IsDefined(typeof(TicketStatus), dto.StatusId))
        {
            var newStatus = (TicketStatus)dto.StatusId;

            if (currentUser.Role == UserRole.Gestor)
            {
                if (request.AssignedToUserId != currentUser.UserId)
                    return BadRequest(new { message = "Solo el gestor asignado puede editar el estado de esta solicitud." });

                if (IsRejectedStatus(newStatus) || IsClosedStatus(newStatus))
                    return BadRequest(new { message = "El gestor no puede establecer ese estado." });

                request.Status = newStatus;
                request.RejectionReason = null;
            }
            else if (currentUser.Role == UserRole.Admin)
            {
                request.Status = newStatus;
                request.RejectionReason = IsRejectedStatus(newStatus) ? dto.RejectionReason?.Trim() : null;
            }
        }

        if (attachment != null)
        {
            DeleteAttachmentIfExists(request.AttachmentPath);
            var uploaded = await SaveAttachmentAsync(attachment);
            request.AttachmentPath = uploaded.Path;
            request.AttachmentName = uploaded.Name;
        }

        await dbContext.SaveChangesAsync();

        await dbContext.Entry(request).Reference(x => x.Area).LoadAsync();
        await dbContext.Entry(request).Reference(x => x.Priority).LoadAsync();
        await dbContext.Entry(request).Reference(x => x.RequestType).LoadAsync();

        return Ok(new
        {
            id = request.Id,
            number = request.Number,
            subject = request.Subject,
            description = request.Description,
            status = request.Status.ToString(),
            statusId = (int)request.Status,
            statusName = request.Status.ToString(),
            rejectionReason = request.RejectionReason,
            createdAtUtc = request.CreatedAtUtc,
            areaId = request.AreaId,
            requestTypeId = request.RequestTypeId,
            priorityId = request.PriorityId,
            area = request.Area?.Name,
            priority = request.Priority?.Name,
            requestType = request.RequestType?.Name,
            assignedToUserId = request.AssignedToUserId,
            createdByUserId = request.CreatedByUserId,
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            canEdit = CanEditRequest(request),
            canDelete = CanDeleteRequest(request),
            canTake = CanTakeRequest(request),
            canAssign = CanAssignRequest(request),
            canChangeStatus = CanChangeStatus(request),
            canComment = CanComment(request),
            canClose = CanCloseRequest(request)
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var request = await dbContext.ServiceRequests
            .Include(x => x.Comments)
            .Include(x => x.HistoryEntries)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanDeleteRequest(request))
            return Forbid();

        DeleteAttachmentIfExists(request.AttachmentPath);

        if (request.Comments.Count != 0)
            dbContext.RequestComments.RemoveRange(request.Comments);

        if (request.HistoryEntries.Count != 0)
            dbContext.RequestHistories.RemoveRange(request.HistoryEntries);

        dbContext.ServiceRequests.Remove(request);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> Detail(int id)
    {
        var request = await dbContext.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanAccessRequest(request))
            return Forbid();

        return Ok(new
        {
            id = request.Id,
            number = request.Number,
            subject = request.Subject,
            description = request.Description,
            status = request.Status.ToString(),
            statusId = (int)request.Status,
            statusName = request.Status.ToString(),
            rejectionReason = request.RejectionReason,
            createdAtUtc = request.CreatedAtUtc,
            areaId = request.AreaId,
            requestTypeId = request.RequestTypeId,
            priorityId = request.PriorityId,
            area = request.Area?.Name,
            priority = request.Priority?.Name,
            requestType = request.RequestType?.Name,
            assignedToUserId = request.AssignedToUserId,
            createdByUserId = request.CreatedByUserId,
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            canEdit = CanEditRequest(request),
            canDelete = CanDeleteRequest(request),
            canTake = CanTakeRequest(request),
            canAssign = CanAssignRequest(request),
            canChangeStatus = CanChangeStatus(request),
            canComment = CanComment(request),
            canClose = CanCloseRequest(request)
        });
    }

    private IQueryable<ServiceRequest> ApplyVisibilityScope(IQueryable<ServiceRequest> query)
    {
        return currentUser.Role switch
        {
            UserRole.SuperAdmin => query,
            UserRole.Admin => currentUser.AreaId.HasValue
                ? query.Where(x => x.AreaId == currentUser.AreaId.Value)
                : query.Where(x => false),
            UserRole.Gestor => currentUser.AreaId.HasValue
                ? query.Where(x => x.AreaId == currentUser.AreaId.Value)
                : query.Where(x => false),
            UserRole.Solicitante => query.Where(x => x.CreatedByUserId == currentUser.UserId),
            _ => query.Where(x => false)
        };
    }

    private bool CanAccessRequest(ServiceRequest request)
    {
        return currentUser.Role switch
        {
            UserRole.SuperAdmin => true,
            UserRole.Admin => currentUser.AreaId.HasValue && request.AreaId == currentUser.AreaId.Value,
            UserRole.Gestor => currentUser.AreaId.HasValue && request.AreaId == currentUser.AreaId.Value,
            UserRole.Solicitante => request.CreatedByUserId == currentUser.UserId,
            _ => false
        };
    }

    private bool CanEditRequest(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        if (request.AssignedToUserId.HasValue)
        {
            if (request.AssignedToUserId.Value != currentUser.UserId)
                return false;

            return currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.Gestor;
        }

        if (!IsNewStatus(request.Status))
            return false;

        return currentUser.Role switch
        {
            UserRole.SuperAdmin => true,
            UserRole.Admin => true,
            UserRole.Gestor => true,
            UserRole.Solicitante => request.CreatedByUserId == currentUser.UserId,
            _ => false
        };
    }

    private bool CanDeleteRequest(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        if (request.AssignedToUserId.HasValue && request.AssignedToUserId.Value != currentUser.UserId)
            return false;

        if (!IsNewStatus(request.Status))
            return false;

        return currentUser.Role == UserRole.SuperAdmin;
    }

    private bool CanTakeRequest(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        if (request.AssignedToUserId.HasValue)
            return false;

        if (currentUser.Role == UserRole.SuperAdmin)
            return false;

        return currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.Gestor;
    }

    private bool CanAssignRequest(ServiceRequest request)
    {
        return currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.SuperAdmin;
    }

    private bool CanChangeStatus(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        if (currentUser.Role == UserRole.Admin)
            return true;

        if (currentUser.Role == UserRole.Gestor)
            return request.AssignedToUserId.HasValue && request.AssignedToUserId.Value == currentUser.UserId;

        return false;
    }

    private bool CanComment(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        return currentUser.Role == UserRole.Admin
            || currentUser.Role == UserRole.Gestor
            || currentUser.Role == UserRole.SuperAdmin;
    }

    private bool CanCloseRequest(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        return currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.SuperAdmin;
    }

    private static bool IsNewStatus(TicketStatus status)
    {
        return string.Equals(status.ToString(), nameof(TicketStatus.Nueva), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRejectedStatus(TicketStatus status)
    {
        return string.Equals(status.ToString(), nameof(TicketStatus.Rechazada), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClosedStatus(TicketStatus status)
    {
        return string.Equals(status.ToString(), nameof(TicketStatus.Cerrada), StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(string? Path, string? Name)> SaveAttachmentAsync(IFormFile? attachment)
    {
        if (attachment is null || attachment.Length == 0)
            return (null, null);

        var uploads = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploads);

        var originalName = Path.GetFileName(attachment.FileName);
        var extension = Path.GetExtension(originalName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploads, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await attachment.CopyToAsync(stream);

        return ($"uploads/{fileName}", originalName);
    }

    private void DeleteAttachmentIfExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var normalized = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(env.ContentRootPath, normalized);

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }

    private static string BuildTemporaryRequestNumber()
    {
        return $"TMP-{DateTime.UtcNow.Ticks}";
    }

    private static string BuildFinalRequestNumber(DateTime createdAtUtc, int id)
    {
        return $"SOL-{createdAtUtc.Year}-{id:D4}";
    }
}