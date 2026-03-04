namespace RequestHub.Domain.Entities;

public class RequestComment
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
