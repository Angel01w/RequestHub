using Microsoft.AspNetCore.Authorization;
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
            query = query.Where(x => x.Number.Contains(filter.Search) || x.Subject.Contains(filter.Search));

        var result = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.Subject,
                x.Description,
                x.Status,
                x.CreatedAtUtc,
                x.AreaId,
                x.RequestTypeId,
                x.PriorityId,
                Area = x.Area != null ? x.Area.Name : null,
                Priority = x.Priority != null ? x.Priority.Name : null,
                RequestType = x.RequestType != null ? x.RequestType.Name : null,
                x.AssignedToUserId
            })
            .ToListAsync();

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

        if (request is null)
            return NotFound();

        return Ok(new
        {
            request.Id,
            request.Number,
            request.Subject,
            request.Description,
            request.Status,
            request.RejectionReason,
            request.CreatedAtUtc,
            request.AreaId,
            request.RequestTypeId,
            request.PriorityId,
            Area = request.Area?.Name,
            Priority = request.Priority?.Name,
            RequestType = request.RequestType?.Name,
            request.AttachmentPath,
            request.AssignedToUserId,
            Comments = request.Comments
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    c.Id,
                    c.Text,
                    c.CreatedAtUtc,
                    User = c.User != null ? c.User.FullName : "Sistema"
                }),
            History = request.HistoryEntries
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    c.Id,
                    c.Action,
                    c.CreatedAtUtc,
                    User = c.User != null ? c.User.FullName : "Sistema"
                })
        });
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> Create([FromForm] CreateServiceRequestDto dto, IFormFile? attachment)
    {
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

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is null)
            return BadRequest(new { message = "No hay usuarios registrados en la base de datos." });

        var currentYear = DateTime.UtcNow.Year;
        var count = await dbContext.ServiceRequests.CountAsync(x => x.CreatedAtUtc.Year == currentYear);
        var number = $"SOL-{currentYear}-{(count + 1):D4}";

        string? attachmentPath = null;

        if (attachment is not null && attachment.Length > 0)
        {
            var uploads = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid():N}_{Path.GetFileName(attachment.FileName)}";
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
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            PriorityId = dto.PriorityId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fallbackUser.Id,
            Status = TicketStatus.Nueva,
            AttachmentPath = attachmentPath
        };

        dbContext.ServiceRequests.Add(request);
        await dbContext.SaveChangesAsync();

        dbContext.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = request.Id,
            UserId = fallbackUser.Id,
            Action = $"Solicitud creada ({request.Number}).",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Detail), new { id = request.Id }, new
        {
            request.Id,
            request.Number
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateServiceRequestDto dto)
    {
        var request = await dbContext.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request is null)
            return NotFound(new { message = "Solicitud no encontrada." });

        var areaExists = await dbContext.Areas.AnyAsync(x => x.Id == dto.AreaId);
        if (!areaExists)
            return BadRequest(new { message = "Área inválida." });

        var type = await dbContext.RequestTypes
            .FirstOrDefaultAsync(x => x.Id == dto.RequestTypeId && x.AreaId == dto.AreaId);
        if (type is null)
            return BadRequest(new { message = "Tipo de solicitud inválido para el área seleccionada." });

        var priorityExists = await dbContext.Priorities.AnyAsync(x => x.Id == dto.PriorityId);
        if (!priorityExists)
            return BadRequest(new { message = "Prioridad inválida." });

        request.AreaId = dto.AreaId;
        request.RequestTypeId = dto.RequestTypeId;
        request.Subject = dto.Subject.Trim();
        request.Description = dto.Description.Trim();
        request.PriorityId = dto.PriorityId;

        await dbContext.SaveChangesAsync();

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is not null)
        {
            dbContext.RequestHistories.Add(new RequestHistory
            {
                ServiceRequestId = request.Id,
                UserId = fallbackUser.Id,
                Action = "Solicitud actualizada.",
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        return Ok(new
        {
            message = "Solicitud actualizada correctamente.",
            request.Id
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
            Action = "Solicitud tomada.",
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
        request.RejectionReason = dto.Status == TicketStatus.Rechazada ? dto.RejectionReason : null;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
    {
        var request = await dbContext.ServiceRequests.FindAsync(id);
        if (request is null)
            return NotFound();

        var fallbackUser = await dbContext.Users.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (fallbackUser is null)
            return BadRequest("No hay usuarios registrados en la base de datos.");

        dbContext.RequestComments.Add(new RequestComment
        {
            ServiceRequestId = id,
            UserId = fallbackUser.Id,
            Text = dto.Text,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}