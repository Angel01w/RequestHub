using RequestHub.Domain.Enums;

namespace RequestHub.Domain.Entities;

public class ServiceRequest
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public Area? Area { get; set; }
    public int RequestTypeId { get; set; }
    public RequestType? RequestType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PriorityId { get; set; }
    public Priority? Priority { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public TicketStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public string? AttachmentPath { get; set; }
    public ICollection<RequestComment> Comments { get; set; } = new List<RequestComment>();
    public ICollection<RequestHistory> HistoryEntries { get; set; } = new List<RequestHistory>();
}
