using System.ComponentModel.DataAnnotations;
using BusBooking.Shared.Enums;

namespace BusBooking.Server.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required, MaxLength(50)]
    public string Type { get; set; } = NotificationTypes.PaymentConfirmed;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}

public class SupportTicket
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Status { get; set; } = SupportTicketStatuses.Pending;

    public int? HandlerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Customer { get; set; } = null!;
    public User? Handler { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}

public class SupportMessage
{
    public int Id { get; set; }
    public int SupportTicketId { get; set; }
    public int SenderId { get; set; }

    [Required, MaxLength(30)]
    public string SenderRole { get; set; } = UserRoles.Customer;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public SupportTicket SupportTicket { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
