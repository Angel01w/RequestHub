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
[AllowAnonymous]
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

        var result = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
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
                attachmentPath = x.AttachmentPath,
                attachmentName = x.AttachmentName
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> Detail(int id)
    {
        var request = await dbContext.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .Include(x => x.Comments).ThenInclude(c => c.User)
            .Include(x => x.HistoryEntries).ThenInclude(h => h.User)
            .Include(x => x.CreatedByUser)
            .Include(x => x.AssignedToUser)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

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
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            assignedToUserId = request.AssignedToUserId,
            createdByName = request.CreatedByUser != null ? request.CreatedByUser.FullName : null,
            assignedToName = request.AssignedToUser != null ? request.AssignedToUser.FullName : null,
            comments = request.Comments
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    id = c.Id,
                    text = c.Text,
                    createdAtUtc = c.CreatedAtUtc,
                    user = c.User != null ? c.User.FullName : "Sistema",
                    userName = c.User != null ? c.User.FullName : "Sistema"
                }),
            history = request.HistoryEntries
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    id = c.Id,
                    action = c.Action,
                    createdAtUtc = c.CreatedAtUtc,
                    user = c.User != null ? c.User.FullName : "Sistema"
                })
        });
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

        var area = await dbContext.Areas.FirstOrDefaultAsync(x => x.Id == dto.AreaId);
        if (area is null)
            return BadRequest(new { message = "Área inválida." });

        var type = await dbContext.RequestTypes
            .FirstOrDefaultAsync(x => x.Id == dto.RequestTypeId && x.AreaId == dto.AreaId);
        if (type is null)
            return BadRequest(new { message = "Tipo de solicitud inválido para el área seleccionada." });

        var priority = await dbContext.Priorities.FirstOrDefaultAsync(x => x.Id == dto.PriorityId);
        if (priority is null)
            return BadRequest(new { message = "Prioridad inválida." });

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is null)
            return BadRequest(new { message = "No hay usuarios registrados en la base de datos." });

        string? attachmentPath = null;
        string? attachmentName = null;

        if (attachment is not null && attachment.Length > 0)
        {
            var uploads = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var originalName = Path.GetFileName(attachment.FileName);
            var extension = Path.GetExtension(originalName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploads, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await attachment.CopyToAsync(stream);

            attachmentPath = $"uploads/{fileName}";
            attachmentName = originalName;
        }

        var request = new ServiceRequest
        {
            Number = BuildTemporaryRequestNumber(),
            AreaId = dto.AreaId,
            RequestTypeId = dto.RequestTypeId,
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            PriorityId = dto.PriorityId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fallbackUser.Id,
            Status = TicketStatus.Nueva,
            RejectionReason = null,
            AttachmentPath = attachmentPath,
            AttachmentName = attachmentName
        };

        dbContext.ServiceRequests.Add(request);
        await dbContext.SaveChangesAsync();

        request.Number = BuildFinalRequestNumber(request.CreatedAtUtc, request.Id);
        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = fallbackUser.Id,
            Action = $"Solicitud creada ({request.Number}). Estado: {request.Status}.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

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
            area = area.Name,
            priority = priority.Name,
            requestType = type.Name,
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            assignedToUserId = request.AssignedToUserId
        });
    }

    [HttpPut("{id:int}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> Update(int id, [FromForm] UpdateServiceRequestDto dto)
    {
        var request = await dbContext.ServiceRequests
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        var areaExists = await dbContext.Areas.AnyAsync(x => x.Id == dto.AreaId);
        if (!areaExists)
            return BadRequest(new { message = "Área inválida." });

        var type = await dbContext.RequestTypes
            .FirstOrDefaultAsync(x => x.Id == dto.RequestTypeId && x.AreaId == dto.AreaId);
        if (type is null)
            return BadRequest(new { message = "Tipo de solicitud inválido para el área seleccionada." });

        var priority = await dbContext.Priorities.FirstOrDefaultAsync(x => x.Id == dto.PriorityId);
        if (priority is null)
            return BadRequest(new { message = "Prioridad inválida." });

        if (!Enum.IsDefined(typeof(TicketStatus), dto.StatusId))
            return BadRequest(new { message = "Estado inválido." });

        if (string.IsNullOrWhiteSpace(dto.Subject))
            return BadRequest(new { message = "El asunto es requerido." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "La descripción es requerida." });

        var newStatus = (TicketStatus)dto.StatusId;

        if (newStatus == TicketStatus.Rechazada && string.IsNullOrWhiteSpace(dto.RejectionReason))
            return BadRequest(new { message = "Debe especificar motivo de rechazo." });

        var attachmentPath = request.AttachmentPath;
        var attachmentName = request.AttachmentName;

        if (dto.RemoveAttachment)
        {
            attachmentPath = null;
            attachmentName = null;
        }
        else if (dto.Attachment is not null && dto.Attachment.Length > 0)
        {
            var uploads = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var originalName = Path.GetFileName(dto.Attachment.FileName);
            var extension = Path.GetExtension(originalName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploads, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await dto.Attachment.CopyToAsync(stream);

            attachmentPath = $"uploads/{fileName}";
            attachmentName = originalName;
        }

        request.AreaId = dto.AreaId;
        request.RequestTypeId = dto.RequestTypeId;
        request.Subject = dto.Subject.Trim();
        request.Description = dto.Description.Trim();
        request.PriorityId = dto.PriorityId;
        request.Status = newStatus;
        request.RejectionReason = newStatus == TicketStatus.Rechazada ? dto.RejectionReason?.Trim() : null;
        request.AttachmentPath = attachmentPath;
        request.AttachmentName = attachmentName;

        await dbContext.SaveChangesAsync();

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is not null)
        {
            dbContext.RequestHistories.Add(new RequestHistory
            {
                ServiceRequestId = request.Id,
                UserId = fallbackUser.Id,
                Action = $"Solicitud actualizada. Estado: {request.Status}.",
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

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
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            assignedToUserId = request.AssignedToUserId,
            message = "Solicitud actualizada correctamente."
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

        if (request.Comments.Count != 0)
            dbContext.RequestComments.RemoveRange(request.Comments);

        if (request.HistoryEntries.Count != 0)
            dbContext.RequestHistories.RemoveRange(request.HistoryEntries);

        dbContext.ServiceRequests.Remove(request);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/take")]
    public async Task<ActionResult> Take(int id)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound();

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is null)
            return BadRequest("No hay usuarios registrados en la base de datos.");

        request.AssignedToUserId = fallbackUser.Id;
        request.Status = request.Status == TicketStatus.Nueva ? TicketStatus.EnProceso : request.Status;

        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = fallbackUser.Id,
            Action = $"Solicitud tomada. Estado: {request.Status}.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult> Assign(int id, [FromBody] AssignRequestDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound();

        if (dto.UserId.HasValue)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == dto.UserId.Value);
            if (user is null)
                return BadRequest("Usuario no válido.");

            request.AssignedToUserId = user.Id;
        }
        else
        {
            request.AssignedToUserId = null;
        }

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound();

        if (dto.Status == TicketStatus.Rechazada && string.IsNullOrWhiteSpace(dto.RejectionReason))
            return BadRequest("Debe especificar motivo de rechazo.");

        request.Status = dto.Status;
        request.RejectionReason = dto.Status == TicketStatus.Rechazada ? dto.RejectionReason?.Trim() : null;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            id = request.Id,
            status = request.Status.ToString(),
            statusId = (int)request.Status,
            statusName = request.Status.ToString(),
            rejectionReason = request.RejectionReason
        });
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { message = "El comentario es requerido." });

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is null)
            return BadRequest(new { message = "No hay usuarios registrados en la base de datos." });

        var comment = new RequestComment
        {
            ServiceRequestId = id,
            UserId = fallbackUser.Id,
            Text = dto.Text.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.RequestComments.Add(comment);
        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = id,
            UserId = fallbackUser.Id,
            Action = "Comentario agregado.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            id = comment.Id,
            text = comment.Text,
            createdAtUtc = comment.CreatedAtUtc,
            user = fallbackUser.FullName,
            userName = fallbackUser.FullName
        });
    }

    [HttpPut("{id:int}/comments/{commentId:int}")]
    public async Task<ActionResult> UpdateComment(int id, int commentId, [FromBody] AddCommentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { message = "El comentario es requerido." });

        var requestExists = await dbContext.ServiceRequests.AnyAsync(x => x.Id == id);
        if (!requestExists)
            return NotFound(new { message = "Solicitud no encontrada." });

        var comment = await dbContext.RequestComments
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == commentId && x.ServiceRequestId == id);

        if (comment is null)
            return NotFound(new { message = "Comentario no encontrado." });

        comment.Text = dto.Text.Trim();

        await dbContext.SaveChangesAsync();

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is not null)
        {
            dbContext.RequestHistories.Add(new RequestHistory
            {
                ServiceRequestId = id,
                UserId = fallbackUser.Id,
                Action = "Comentario editado.",
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        return Ok(new
        {
            id = comment.Id,
            text = comment.Text,
            createdAtUtc = comment.CreatedAtUtc,
            user = comment.User != null ? comment.User.FullName : "Sistema",
            userName = comment.User != null ? comment.User.FullName : "Sistema"
        });
    }

    [HttpDelete("{id:int}/comments/{commentId:int}")]
    public async Task<ActionResult> DeleteComment(int id, int commentId)
    {
        var requestExists = await dbContext.ServiceRequests.AnyAsync(x => x.Id == id);
        if (!requestExists)
            return NotFound(new { message = "Solicitud no encontrada." });

        var comment = await dbContext.RequestComments
            .FirstOrDefaultAsync(x => x.Id == commentId && x.ServiceRequestId == id);

        if (comment is null)
            return NotFound(new { message = "Comentario no encontrado." });

        dbContext.RequestComments.Remove(comment);
        await dbContext.SaveChangesAsync();

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is not null)
        {
            dbContext.RequestHistories.Add(new RequestHistory
            {
                ServiceRequestId = id,
                UserId = fallbackUser.Id,
                Action = "Comentario eliminado.",
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        return NoContent();
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