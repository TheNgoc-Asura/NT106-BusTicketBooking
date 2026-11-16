using System.ComponentModel.DataAnnotations;
using BusBooking.Shared.Enums;

namespace BusBooking.Server.Entities;

public class Route
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string RouteCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Destination { get; set; } = string.Empty;

    public decimal? DistanceKm { get; set; }
    public int? DurationMinutes { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = RouteStatuses.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<RoutePoint> RoutePoints { get; set; } = new List<RoutePoint>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public class RoutePoint
{
    public int Id { get; set; }
    public int RouteId { get; set; }

    [Required, MaxLength(20)]
    public string Type { get; set; } = RoutePointTypes.Pickup; // Pickup | Dropoff

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    public int OrderIndex { get; set; } = 1;

    // Navigation properties
    public Route Route { get; set; } = null!;
    public ICollection<Ticket> PickupTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> DropoffTickets { get; set; } = new List<Ticket>();
}
