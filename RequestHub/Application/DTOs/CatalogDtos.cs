namespace RequestHub.Application.DTOs;

public record AreaDto(int Id, string Name);
public record RequestTypeDto(int Id, string Name, int AreaId);
public record PriorityDto(int Id, string Name);
public record UpsertAreaDto(string Name);
public record UpsertRequestTypeDto(string Name, int AreaId);
public record UpsertPriorityDto(string Name);
