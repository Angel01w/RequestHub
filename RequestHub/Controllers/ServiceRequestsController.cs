using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Application.DTOs;
using RequestHub.Application.Services;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using RequestHub.Infrastructure.Persistence;
using System;

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
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .AsQueryable();

        if (currentUser.Role == UserRole.Solicitante)
            query = query.Where(x => x.CreatedByUserId == currentUser.UserId);
        else if (currentUser.Role == UserRole.Gestor && currentUser.AreaId.HasValue)
            query = query.Where(x => x.AreaId == currentUser.AreaId.Value);

        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.AreaId.HasValue) query = query.Where(x => x.AreaId == filter.AreaId.Value);
        if (filter.PriorityId.HasValue) query = query.Where(x => x.PriorityId == filter.PriorityId.Value);
        if (filter.FromUtc.HasValue) query = query.Where(x => x.CreatedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) query = query.Where(x => x.CreatedAtUtc <= filter.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(x => x.Number.Contains(filter.Search) || x.Subject.Contains(filter.Search));

        var result = await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new
        {
            x.Id,
            x.Number,
            x.Subject,
            x.Status,
            x.CreatedAtUtc,
            Area = x.Area!.Name,
            Priority = x.Priority!.Name,
            RequestType = x.RequestType!.Name,
            x.AssignedToUserId
        }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> Detail(int id)
    {
        var request = await dbContext.ServiceRequests
            .Include(x => x.Area)
            .Include(x => x.Priority)
            .Include(x => x.RequestType)
            .Include(x => x.Comments).ThenInclude(c => c.User)
            .Include(x => x.HistoryEntries).ThenInclude(h => h.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null) return NotFound();
        if (!CanView(request)) return Forbid();

        return Ok(new
        {
            request.Id,
            request.Number,
            request.Subject,
            request.Description,
            request.Status,
            request.RejectionReason,
            request.CreatedAtUtc,
            Area = request.Area?.Name,
            Priority = request.Priority?.Name,
            RequestType = request.RequestType?.Name,
            request.AttachmentPath,
            request.AssignedToUserId,
            Comments = request.Comments.OrderBy(c => c.CreatedAtUtc).Select(c => new { c.Id, c.Text, c.CreatedAtUtc, User = c.User!.FullName }),
            History = request.HistoryEntries.OrderBy(c => c.CreatedAtUtc).Select(c => new { c.Id, c.Action, c.CreatedAtUtc, User = c.User!.FullName })
        });
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> Create([FromForm] CreateServiceRequestDto dto, IFormFile? attachment)
    {
        if (currentUser.Role != UserRole.Solicitante && currentUser.Role != UserRole.Admin) return Forbid();
        var type = await dbContext.RequestTypes.FirstOrDefaultAsync(x => x.Id == dto.RequestTypeId && x.AreaId == dto.AreaId);
        if (type is null) return BadRequest("Tipo de solicitud inválido para el área seleccionada.");

        var currentYear = DateTime.UtcNow.Year;
        var count = await dbContext.ServiceRequests.CountAsync(x => x.CreatedAtUtc.Year == currentYear);
        var number = $"SOL-{currentYear}-{(count + 1):D4}";

        string? attachmentPath = null;
        if (attachment is not null)
        {
            var uploads = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(attachment.FileName)}";
            var fullPath = Path.Combine(uploads, fileName);
            await using var stream = System.IO.File.Create(fullPath);
            await attachment.CopyToAsync(stream);
            attachmentPath = $"uploads/{fileName}";
        }

        var request = new ServiceRequest
        {
            Number = number,
            AreaId = dto.AreaId,
            RequestTypeId = dto.RequestTypeId,
            Subject = dto.Subject,
            Description = dto.Description,
            PriorityId = dto.PriorityId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = currentUser.UserId,
            Status = TicketStatus.Nueva,
            AttachmentPath = attachmentPath
        };

        dbContext.ServiceRequests.Add(request);
        await dbContext.SaveChangesAsync();
        await AddHistory(request.Id, $"Solicitud creada ({request.Number}).");
        return CreatedAtAction(nameof(Detail), new { id = request.Id }, new { request.Id, request.Number });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateServiceRequestDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (currentUser.Role != UserRole.Solicitante || request.CreatedByUserId != currentUser.UserId || request.Status != TicketStatus.Nueva)
            return Forbid();

        request.AreaId = dto.AreaId;
        request.RequestTypeId = dto.RequestTypeId;
        request.Subject = dto.Subject;
        request.Description = dto.Description;
        request.PriorityId = dto.PriorityId;
        await dbContext.SaveChangesAsync();
        await AddHistory(request.Id, "Solicitud editada por solicitante.");
        return NoContent();
    }

    [HttpPost("{id:int}/take")]
    [Authorize(Roles = "Gestor,Admin")]
    public async Task<ActionResult> Take(int id)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null) return NotFound();

        if (currentUser.Role == UserRole.Gestor && currentUser.AreaId != request.AreaId) return Forbid();

        request.AssignedToUserId = currentUser.UserId;
        request.Status = request.Status == TicketStatus.Nueva ? TicketStatus.EnProceso : request.Status;
        await dbContext.SaveChangesAsync();
        await AddHistory(request.Id, "Solicitud tomada por gestor.");
        return NoContent();
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Assign(int id, [FromBody] AssignRequestDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null) return NotFound();

        if (dto.UserId.HasValue)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == dto.UserId.Value && x.Role == UserRole.Gestor);
            if (user is null) return BadRequest("Gestor no válido.");
            request.AssignedToUserId = user.Id;
        }
        else
        {
            request.AssignedToUserId = null;
        }

        await dbContext.SaveChangesAsync();
        await AddHistory(request.Id, $"Asignación actualizada a usuario {dto.UserId?.ToString() ?? "sin asignar"}.");
        return NoContent();
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "Gestor,Admin")]
    public async Task<ActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (currentUser.Role == UserRole.Gestor && currentUser.AreaId != request.AreaId) return Forbid();

        if (dto.Status == TicketStatus.Rechazada && string.IsNullOrWhiteSpace(dto.RejectionReason))
            return BadRequest("Debe especificar motivo de rechazo.");

        if (dto.Status == TicketStatus.Cerrada && currentUser.Role != UserRole.Admin)
            return Forbid();

        request.Status = dto.Status;
        request.RejectionReason = dto.Status == TicketStatus.Rechazada ? dto.RejectionReason : null;
        await dbContext.SaveChangesAsync();
        await AddHistory(request.Id, $"Estado cambiado a {dto.Status}.");
        return NoContent();
    }

    [HttpPost("{id:int}/comments")]
    [Authorize(Roles = "Gestor,Admin")]
    public async Task<ActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (currentUser.Role == UserRole.Gestor && currentUser.AreaId != request.AreaId) return Forbid();

        dbContext.RequestComments.Add(new RequestComment
        {
            ServiceRequestId = id,
            UserId = currentUser.UserId,
            Text = dto.Text,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        await AddHistory(id, "Comentario agregado.");
        return NoContent();
    }

    private bool CanView(ServiceRequest request)
    {
        return currentUser.Role switch
        {
            UserRole.Admin => true,
            UserRole.Gestor => currentUser.AreaId == request.AreaId,
            _ => request.CreatedByUserId == currentUser.UserId
        };
    }

    private async Task AddHistory(int requestId, string action)
    {
        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = requestId,
            UserId = currentUser.UserId,
            Action = action,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}
