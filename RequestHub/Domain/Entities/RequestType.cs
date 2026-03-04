namespace RequestHub.Domain.Entities;

public class RequestType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public Area? Area { get; set; }
}
