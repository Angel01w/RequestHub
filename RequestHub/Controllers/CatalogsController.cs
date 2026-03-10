using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Application.DTOs;
using RequestHub.Domain.Entities;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class CatalogsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("areas")]
    public async Task<ActionResult<IEnumerable<AreaDto>>> GetAreas()
    {
        var areas = await dbContext.Areas
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AreaDto(x.Id, x.Name))
            .ToListAsync();

        return Ok(areas);
    }

    [HttpPost("areas")]
    public async Task<IActionResult> CreateArea([FromBody] UpsertAreaDto dto)
    {
        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Nombre requerido" });

        var exists = await dbContext.Areas.AnyAsync(x => x.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "El área ya existe" });

        var area = new Area
        {
            Name = name
        };

        dbContext.Areas.Add(area);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Área creada",
            area = new AreaDto(area.Id, area.Name)
        });
    }

    [HttpPut("areas/{id:int}")]
    public async Task<IActionResult> UpdateArea(int id, [FromBody] UpsertAreaDto dto)
    {
        var area = await dbContext.Areas.FirstOrDefaultAsync(x => x.Id == id);

        if (area == null)
            return NotFound(new { message = "Área no encontrada" });

        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Nombre requerido" });

        var exists = await dbContext.Areas
            .AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "Ya existe un área con ese nombre" });

        area.Name = name;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Área actualizada",
            area = new AreaDto(area.Id, area.Name)
        });
    }

    [HttpDelete("areas/{id:int}")]
    public async Task<IActionResult> DeleteArea(int id)
    {
        var area = await dbContext.Areas.FirstOrDefaultAsync(x => x.Id == id);

        if (area == null)
            return NotFound(new { message = "Área no encontrada" });

        var used = await dbContext.RequestTypes.AnyAsync(x => x.AreaId == id);

        if (used)
            return Conflict(new { message = "No se puede eliminar el área porque tiene tipos asociados" });

        dbContext.Areas.Remove(area);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Área eliminada"
        });
    }

    [HttpGet("request-types")]
    public async Task<ActionResult<IEnumerable<RequestTypeDto>>> GetRequestTypes()
    {
        var types = await dbContext.RequestTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RequestTypeDto(x.Id, x.Name, x.AreaId))
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost("request-types")]
    public async Task<IActionResult> CreateRequestType([FromBody] UpsertRequestTypeDto dto)
    {
        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Nombre requerido" });

        var areaExists = await dbContext.Areas.AnyAsync(x => x.Id == dto.AreaId);

        if (!areaExists)
            return NotFound(new { message = "Área no encontrada" });

        var exists = await dbContext.RequestTypes
            .AnyAsync(x => x.AreaId == dto.AreaId && x.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "El tipo ya existe en esa área" });

        var entity = new RequestType
        {
            Name = name,
            AreaId = dto.AreaId
        };

        dbContext.RequestTypes.Add(entity);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Tipo creado",
            type = new RequestTypeDto(entity.Id, entity.Name, entity.AreaId)
        });
    }

    [HttpPut("request-types/{id:int}")]
    public async Task<IActionResult> UpdateRequestType(int id, [FromBody] UpsertRequestTypeDto dto)
    {
        var entity = await dbContext.RequestTypes.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return NotFound(new { message = "Tipo no encontrado" });

        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Nombre requerido" });

        var areaExists = await dbContext.Areas.AnyAsync(x => x.Id == dto.AreaId);

        if (!areaExists)
            return NotFound(new { message = "Área no encontrada" });

        var exists = await dbContext.RequestTypes
            .AnyAsync(x => x.Id != id && x.AreaId == dto.AreaId && x.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "Ya existe un tipo de solicitud con ese nombre en esa área" });

        entity.Name = name;
        entity.AreaId = dto.AreaId;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Tipo actualizado",
            type = new RequestTypeDto(entity.Id, entity.Name, entity.AreaId)
        });
    }

    [HttpDelete("request-types/{id:int}")]
    public async Task<IActionResult> DeleteRequestType(int id)
    {
        var type = await dbContext.RequestTypes.FirstOrDefaultAsync(x => x.Id == id);

        if (type == null)
            return NotFound(new { message = "Tipo no encontrado" });

        dbContext.RequestTypes.Remove(type);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Tipo eliminado"
        });
    }
}