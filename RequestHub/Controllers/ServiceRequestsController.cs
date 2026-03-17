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
            canDelete = CanDeleteRequest(),
            canTake = CanTakeRequest(x),
            canAssign = CanAssignRequest(x),
            canChangeStatus = CanChangeStatus(x),
            canComment = CanComment(x),
            canClose = CanCloseRequest(x)
        });

        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<object>>> Mine([FromQuery] ServiceRequestFilterDto filter)
    {
        var query = dbContext.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .Where(x => x.CreatedByUserId == currentUser.UserId);

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

        if (currentUser.Role == UserRole.Solicitante)
            query = query.Where(x => x.CreatedByUserId == currentUser.UserId);
        else if (currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.Gestor)
            return Forbid();

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
            canDelete = CanDeleteRequest(),
            canTake = CanTakeRequest(x),
            canAssign = CanAssignRequest(x),
            canChangeStatus = CanChangeStatus(x),
            canComment = CanComment(x),
            canClose = CanCloseRequest(x)
        });

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
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            assignedToUserId = request.AssignedToUserId,
            createdByUserId = request.CreatedByUserId,
            createdByName = request.CreatedByUser != null ? request.CreatedByUser.FullName : null,
            assignedToName = request.AssignedToUser != null ? request.AssignedToUser.FullName : null,
            canEdit = CanEditRequest(request),
            canDelete = CanDeleteRequest(),
            canTake = CanTakeRequest(request),
            canAssign = CanAssignRequest(request),
            canChangeStatus = CanChangeStatus(request),
            canComment = CanComment(request),
            canClose = CanCloseRequest(request),
            comments = request.Comments
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    id = c.Id,
                    text = c.Text,
                    createdAtUtc = c.CreatedAtUtc,
                    user = c.User != null ? c.User.FullName : "Sistema",
                    userName = c.User != null ? c.User.FullName : "Sistema",
                    userId = c.UserId
                }),
            history = request.HistoryEntries
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    id = c.Id,
                    action = c.Action,
                    createdAtUtc = c.CreatedAtUtc,
                    user = c.User != null ? c.User.FullName : "Sistema",
                    userId = c.UserId
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

        var creator = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUser.UserId);
        if (creator is null)
            return Unauthorized(new { message = "Usuario autenticado no válido." });

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
            AttachmentName = uploaded.Name
        };

        dbContext.ServiceRequests.Add(request);
        await dbContext.SaveChangesAsync();

        request.Number = BuildFinalRequestNumber(request.CreatedAtUtc, request.Id);
        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = currentUser.UserId,
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
            createdByUserId = request.CreatedByUserId,
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

        if (!CanEditRequest(request))
            return Forbid();

        var isSolicitante = currentUser.Role == UserRole.Solicitante;
        var isSuperAdmin = currentUser.Role == UserRole.SuperAdmin;

        if (!isSolicitante && !isSuperAdmin)
            return Forbid();

        if (isSolicitante)
        {
            if (request.CreatedByUserId != currentUser.UserId)
                return Forbid();

            if (request.Status != TicketStatus.Nueva)
                return BadRequest(new { message = "Solo puedes editar solicitudes en estado Nueva." });
        }

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

        if (string.IsNullOrWhiteSpace(dto.Subject))
            return BadRequest(new { message = "El asunto es requerido." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "La descripción es requerida." });

        var attachmentPath = request.AttachmentPath;
        var attachmentName = request.AttachmentName;

        if (dto.RemoveAttachment)
        {
            DeleteAttachmentIfExists(request.AttachmentPath);
            attachmentPath = null;
            attachmentName = null;
        }
        else if (dto.Attachment is not null && dto.Attachment.Length > 0)
        {
            DeleteAttachmentIfExists(request.AttachmentPath);
            var uploaded = await SaveAttachmentAsync(dto.Attachment);
            attachmentPath = uploaded.Path;
            attachmentName = uploaded.Name;
        }

        request.AreaId = dto.AreaId;
        request.RequestTypeId = dto.RequestTypeId;
        request.Subject = dto.Subject.Trim();
        request.Description = dto.Description.Trim();
        request.PriorityId = dto.PriorityId;
        request.AttachmentPath = attachmentPath;
        request.AttachmentName = attachmentName;

        if (isSolicitante)
        {
            request.Status = TicketStatus.Nueva;
            request.RejectionReason = null;
        }
        else if (isSuperAdmin)
        {
            if (Enum.IsDefined(typeof(TicketStatus), dto.StatusId))
            {
                var newStatus = (TicketStatus)dto.StatusId;

                if (IsCloseStatus(newStatus))
                    return BadRequest(new { message = "Solo un administrador puede cerrar solicitudes." });

                if (IsRejectedStatus(newStatus) && string.IsNullOrWhiteSpace(dto.RejectionReason))
                    return BadRequest(new { message = "Debe especificar motivo de rechazo." });

                request.Status = newStatus;
                request.RejectionReason = IsRejectedStatus(newStatus) ? dto.RejectionReason?.Trim() : null;
            }
        }

        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = currentUser.UserId,
            Action = $"Solicitud actualizada. Estado: {request.Status}.",
            CreatedAtUtc = DateTime.UtcNow
        });

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
            createdByUserId = request.CreatedByUserId,
            attachmentPath = request.AttachmentPath,
            attachmentName = request.AttachmentName,
            assignedToUserId = request.AssignedToUserId,
            message = "Solicitud actualizada correctamente."
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!CanDeleteRequest())
            return Forbid();

        var request = await dbContext.ServiceRequests
            .Include(x => x.Comments)
            .Include(x => x.HistoryEntries)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        DeleteAttachmentIfExists(request.AttachmentPath);

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
        if (currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.Gestor && currentUser.Role != UserRole.SuperAdmin)
            return Forbid();

        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanAccessRequest(request))
            return Forbid();

        request.AssignedToUserId = currentUser.UserId;
        request.Status = IsNewStatus(request.Status) ? TicketStatus.EnProceso : request.Status;

        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = currentUser.UserId,
            Action = $"Solicitud tomada. Estado: {request.Status}.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult> Assign(int id, [FromBody] AssignRequestDto dto)
    {
        if (currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.SuperAdmin)
            return Forbid();

        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanAccessRequest(request))
            return Forbid();

        if (dto.UserId.HasValue)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == dto.UserId.Value);
            if (user is null)
                return BadRequest(new { message = "Usuario no válido." });

            if (currentUser.Role == UserRole.Admin)
            {
                if (!currentUser.AreaId.HasValue || user.AreaId != currentUser.AreaId.Value)
                    return BadRequest(new { message = "El usuario asignado no pertenece al área permitida." });
            }

            request.AssignedToUserId = user.Id;
        }
        else
        {
            request.AssignedToUserId = null;
        }

        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = currentUser.UserId,
            Action = request.AssignedToUserId.HasValue ? $"Solicitud asignada al usuario {request.AssignedToUserId}." : "Asignación removida.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        if (!CanAttemptStatusChange())
            return Forbid();

        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanAccessRequest(request))
            return Forbid();

        if (!CanChangeStatus(request))
            return Forbid();

        if (IsCloseStatus(dto.Status))
        {
            if (currentUser.Role != UserRole.Admin)
                return BadRequest(new { message = "Solo un administrador puede cerrar solicitudes." });

            if (!IsCompletedForClose(request.Status))
                return BadRequest(new { message = "Solo se puede cerrar una solicitud cuando está completa." });
        }

        if (IsRejectedStatus(dto.Status) && string.IsNullOrWhiteSpace(dto.RejectionReason))
            return BadRequest(new { message = "Debe especificar motivo de rechazo." });

        request.Status = dto.Status;
        request.RejectionReason = IsRejectedStatus(dto.Status) ? dto.RejectionReason?.Trim() : null;

        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = currentUser.UserId,
            Action = $"Estado cambiado a {request.Status}.",
            CreatedAtUtc = DateTime.UtcNow
        });

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
        if (!CanAttemptComment())
            return Forbid();

        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanAccessRequest(request))
            return Forbid();

        if (!CanComment(request))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { message = "El comentario es requerido." });

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUser.UserId);
        if (user is null)
            return Unauthorized(new { message = "Usuario autenticado no válido." });

        var comment = new RequestComment
        {
            ServiceRequestId = id,
            UserId = currentUser.UserId,
            Text = dto.Text.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.RequestComments.Add(comment);
        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = id,
            UserId = currentUser.UserId,
            Action = "Comentario agregado.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            id = comment.Id,
            text = comment.Text,
            createdAtUtc = comment.CreatedAtUtc,
            user = user.FullName,
            userName = user.FullName,
            userId = currentUser.UserId
        });
    }

    [HttpPut("{id:int}/comments/{commentId:int}")]
    public async Task<ActionResult> UpdateComment(int id, int commentId, [FromBody] AddCommentDto dto)
    {
        if (!CanAttemptComment())
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { message = "El comentario es requerido." });

        var request = await dbContext.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanAccessRequest(request))
            return Forbid();

        if (!CanComment(request))
            return Forbid();

        var comment = await dbContext.RequestComments
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == commentId && x.ServiceRequestId == id);

        if (comment is null)
            return NotFound(new { message = "Comentario no encontrado." });

        comment.Text = dto.Text.Trim();

        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = id,
            UserId = currentUser.UserId,
            Action = "Comentario editado.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            id = comment.Id,
            text = comment.Text,
            createdAtUtc = comment.CreatedAtUtc,
            user = comment.User != null ? comment.User.FullName : "Sistema",
            userName = comment.User != null ? comment.User.FullName : "Sistema",
            userId = comment.UserId
        });
    }

    [HttpDelete("{id:int}/comments/{commentId:int}")]
    public async Task<ActionResult> DeleteComment(int id, int commentId)
    {
        if (!CanAttemptComment())
            return Forbid();

        var request = await dbContext.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        if (!CanAccessRequest(request))
            return Forbid();

        if (!CanComment(request))
            return Forbid();

        var comment = await dbContext.RequestComments
            .FirstOrDefaultAsync(x => x.Id == commentId && x.ServiceRequestId == id);

        if (comment is null)
            return NotFound(new { message = "Comentario no encontrado." });

        dbContext.RequestComments.Remove(comment);
        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = id,
            UserId = currentUser.UserId,
            Action = "Comentario eliminado.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return NoContent();
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
        return currentUser.Role switch
        {
            UserRole.SuperAdmin => true,
            UserRole.Solicitante => request.CreatedByUserId == currentUser.UserId && IsNewStatus(request.Status),
            _ => false
        };
    }

    private bool CanDeleteRequest()
    {
        return currentUser.Role == UserRole.SuperAdmin;
    }

    private bool CanTakeRequest(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        return currentUser.Role == UserRole.Admin
            || currentUser.Role == UserRole.Gestor
            || currentUser.Role == UserRole.SuperAdmin;
    }

    private bool CanAssignRequest(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        return currentUser.Role == UserRole.Admin
            || currentUser.Role == UserRole.SuperAdmin;
    }

    private bool CanChangeStatus(ServiceRequest request)
    {
        if (!CanAccessRequest(request))
            return false;

        return currentUser.Role == UserRole.Admin
            || currentUser.Role == UserRole.Gestor
            || currentUser.Role == UserRole.SuperAdmin;
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
        return currentUser.Role == UserRole.Admin
            && CanAccessRequest(request)
            && IsCompletedForClose(request.Status);
    }

    private bool CanAttemptStatusChange()
    {
        return currentUser.Role == UserRole.Admin
            || currentUser.Role == UserRole.Gestor
            || currentUser.Role == UserRole.SuperAdmin;
    }

    private bool CanAttemptComment()
    {
        return currentUser.Role == UserRole.Admin
            || currentUser.Role == UserRole.Gestor
            || currentUser.Role == UserRole.SuperAdmin;
    }

    private static bool IsNewStatus(TicketStatus status)
    {
        return string.Equals(status.ToString(), nameof(TicketStatus.Nueva), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRejectedStatus(TicketStatus status)
    {
        return string.Equals(status.ToString(), nameof(TicketStatus.Rechazada), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCloseStatus(TicketStatus status)
    {
        return string.Equals(status.ToString(), nameof(TicketStatus.Cerrada), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCompletedForClose(TicketStatus status)
    {
        var name = status.ToString();
        return string.Equals(name, "Completada", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Completa", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Completed", StringComparison.OrdinalIgnoreCase);
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