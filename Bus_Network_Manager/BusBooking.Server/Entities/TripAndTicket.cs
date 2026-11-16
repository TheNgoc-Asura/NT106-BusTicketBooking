using System.ComponentModel.DataAnnotations;
using BusBooking.Shared.Enums;

namespace BusBooking.Server.Entities;

public class Trip
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string TripCode { get; set; } = string.Empty;

    public int RouteId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public int? AssistantId { get; set; }

    public DateTime DepartureTime { get; set; }
    public DateTime EstimatedArrival { get; set; }
    public decimal Price { get; set; }
    public decimal Surcharge { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = TripStatuses.Upcoming;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Route Route { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public User Driver { get; set; } = null!;
    public User? Assistant { get; set; }
    public ICollection<TripSeat> TripSeats { get; set; } = new List<TripSeat>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class TripSeat
{
    public int Id { get; set; }
    public int TripId { get; set; }

    [Required, MaxLength(10)]
    public string SeatCode { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Status { get; set; } = SeatStatuses.Available;

    public DateTime? LockedUntil { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Ticket
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string TicketCode { get; set; } = string.Empty;

    public int TripId { get; set; }
    public int SeatId { get; set; }
    public int? CustomerId { get; set; }

    [Required, MaxLength(150)]
    public string PassengerName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PassengerPhone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? PassengerEmail { get; set; }

    public int PickupPointId { get; set; }
    public int DropoffPointId { get; set; }

    public decimal TotalAmount { get; set; }

    [Required, MaxLength(20)]
    public string Channel { get; set; } = BookingChannels.Online;

    [Required, MaxLength(30)]
    public string Status { get; set; } = TicketStatuses.PendingPayment;

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public long? PaymentTransId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public TripSeat Seat { get; set; } = null!;
    public User? Customer { get; set; }
    public RoutePoint PickupPoint { get; set; } = null!;
    public RoutePoint DropoffPoint { get; set; } = null!;
}
