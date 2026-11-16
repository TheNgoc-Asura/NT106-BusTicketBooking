using System.ComponentModel.DataAnnotations;
using BusBooking.Shared.Enums;

namespace BusBooking.Server.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = UserRoles.Customer;

    [Required, MaxLength(20)]
    public string Status { get; set; } = AccountStatuses.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Trip> DrivenTrips { get; set; } = new List<Trip>();
    public ICollection<Trip> AssistedTrips { get; set; } = new List<Trip>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<SupportTicket> CustomerSupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<SupportTicket> HandledSupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<SupportMessage> SupportMessages { get; set; } = new List<SupportMessage>();
}
