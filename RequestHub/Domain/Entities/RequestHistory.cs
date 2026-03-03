namespace RequestHub.Domain.Entities;

public class RequestHistory
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
