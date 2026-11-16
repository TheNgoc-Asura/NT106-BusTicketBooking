using System.ComponentModel.DataAnnotations;
using BusBooking.Shared.Enums;

namespace BusBooking.Server.Entities;

public class VehicleType
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SeatCount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

public class Vehicle
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = VehicleStatuses.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public VehicleType VehicleType { get; set; } = null!;
    public ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public class MaintenanceSchedule
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
}
