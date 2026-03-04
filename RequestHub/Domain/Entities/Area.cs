namespace RequestHub.Domain.Entities;

public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();
}
