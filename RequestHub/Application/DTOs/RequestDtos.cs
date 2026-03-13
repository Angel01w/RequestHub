using Microsoft.AspNetCore.Http;
using RequestHub.Domain.Enums;

namespace RequestHub.Application.DTOs;

public record CreateServiceRequestDto(
    int AreaId,
    int RequestTypeId,
    string Subject,
    string Description,
    int PriorityId
);

public record UpdateServiceRequestDto(
    int AreaId,
    int RequestTypeId,
    int PriorityId,
    int StatusId,
    string Subject,
    string Description,
    string? RejectionReason,
    bool RemoveAttachment,
    IFormFile? Attachment
);

public record ChangeStatusDto(
    TicketStatus Status,
    string? RejectionReason
);

public record AssignRequestDto(
    int? UserId
);

public record AddCommentDto(
    string Text
);

public record ServiceRequestFilterDto(
    TicketStatus? Status,
    int? AreaId,
    int? PriorityId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Search
);