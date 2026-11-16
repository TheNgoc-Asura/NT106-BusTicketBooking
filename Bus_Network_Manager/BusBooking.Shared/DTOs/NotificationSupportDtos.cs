namespace BusBooking.Shared.DTOs;

// ---------------- NOTIFICATIONS ----------------
public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MarkNotificationReadResponseData
{
    public int Id { get; set; }
    public bool IsRead { get; set; }
}

// ---------------- SUPPORT CHAT ----------------
public class SupportMessageDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int SenderId { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class CreateSupportMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class SendSupportMessageResponseData
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class SupportTicketSummaryDto
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? HandlerName { get; set; }
    public DateTime LastMessageAt { get; set; }
}

public class SupportStatisticsDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int WaitingCustomer { get; set; }
    public int Resolved { get; set; }
}

public class AdminSupportTicketListResponseData
{
    public SupportStatisticsDto Statistics { get; set; } = new();
    public Models.PagedResult<SupportTicketSummaryDto> Tickets { get; set; } = new();
}

public class UpdateSupportTicketStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateSupportTicketStatusResponseData
{
    public int TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int HandlerId { get; set; }
}
