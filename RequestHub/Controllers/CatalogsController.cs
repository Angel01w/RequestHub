using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Application.DTOs;
using RequestHub.Domain.Entities;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CatalogsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("areas")]
    public async Task<ActionResult<IEnumerable<AreaDto>>> GetAreas() => Ok(await dbContext.Areas
        .Select(x => new AreaDto(x.Id, x.Name)).ToListAsync());

    [HttpPost("areas")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateArea(UpsertAreaDto dto)
    {
        var area = new Area { Name = dto.Name };
        dbContext.Areas.Add(area);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAreas), new { id = area.Id }, area);
    }

    [HttpGet("request-types")]
    public async Task<ActionResult<IEnumerable<RequestTypeDto>>> GetRequestTypes() => Ok(await dbContext.RequestTypes
        .Select(x => new RequestTypeDto(x.Id, x.Name, x.AreaId)).ToListAsync());

    [HttpPost("request-types")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateRequestType(UpsertRequestTypeDto dto)
    {
        var areaExists = await dbContext.Areas.AnyAsync(x => x.Id == dto.AreaId);
        if (!areaExists) return NotFound("Área no encontrada.");

        var entity = new RequestType { Name = dto.Name, AreaId = dto.AreaId };
        dbContext.RequestTypes.Add(entity);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRequestTypes), new { id = entity.Id }, entity);
    }

    [HttpGet("priorities")]
    public async Task<ActionResult<IEnumerable<PriorityDto>>> GetPriorities() => Ok(await dbContext.Priorities
        .Select(x => new PriorityDto(x.Id, x.Name)).ToListAsync());

    [HttpPost("priorities")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreatePriority(UpsertPriorityDto dto)
    {
        var entity = new Priority { Name = dto.Name };
        dbContext.Priorities.Add(entity);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPriorities), new { id = entity.Id }, entity);
    }
}
