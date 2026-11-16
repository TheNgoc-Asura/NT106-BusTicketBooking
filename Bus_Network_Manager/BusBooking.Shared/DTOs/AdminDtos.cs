namespace BusBooking.Shared.DTOs;

// ---------------- 1. DASHBOARD ----------------
public class DashboardStatsDto
{
    public int TicketsToday { get; set; }
    public int TripsToday { get; set; }
    public decimal RevenueToday { get; set; }
    public List<UpcomingTripDto> UpcomingTrips { get; set; } = new();
}

public class UpcomingTripDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int BookedSeats { get; set; }
    public int TotalSeats { get; set; }
}

// ---------------- 2. TRIPS ----------------
public class AdminTripDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? AssistantName { get; set; }
    public decimal Price { get; set; }
    public int BookedSeats { get; set; }
    public int TotalSeats { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AdminTripDetailDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public int RouteId { get; set; }
    public DateTime DepartureTime { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public int? AssistantId { get; set; }
    public decimal Price { get; set; }
    public decimal Surcharge { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TripFormOptionsDto
{
    public List<OptionItemDto> Routes { get; set; } = new();
    public List<VehicleOptionDto> Vehicles { get; set; } = new();
    public List<StaffOptionDto> Drivers { get; set; } = new();
    public List<StaffOptionDto> Assistants { get; set; } = new();
}

public class OptionItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VehicleOptionDto
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
}

public class StaffOptionDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class CreateTripRequest
{
    public int RouteId { get; set; }
    public DateTime DepartureTime { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public int? AssistantId { get; set; }
    public decimal Price { get; set; }
    public decimal Surcharge { get; set; }
}

public class UpdateTripRequest : CreateTripRequest
{
    public string Status { get; set; } = string.Empty;
}

public class CreateTripResponseData
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
}

// ---------------- 3. ROUTES ----------------
public class AdminRouteDto
{
    public int RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UpcomingTrips { get; set; }
}

public class RouteDetailDto
{
    public int RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public int? DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<RoutePointDto> PickupPoints { get; set; } = new();
    public List<RoutePointDto> DropoffPoints { get; set; } = new();
}

public class RoutePointInputDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class CreateRouteRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public int? DurationMinutes { get; set; }
    public List<RoutePointInputDto> PickupPoints { get; set; } = new();
    public List<RoutePointInputDto> DropoffPoints { get; set; } = new();
}

public class UpdateRouteRequest : CreateRouteRequest
{
    public string Status { get; set; } = string.Empty;
}

public class CreateRouteResponseData
{
    public int RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
}

// ---------------- 4. VEHICLES & MAINTENANCE ----------------
public class AdminVehicleDto
{
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class VehicleTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeatCount { get; set; }
}

public class CreateVehicleRequest
{
    public string LicensePlate { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
}

public class UpdateVehicleRequest
{
    public string LicensePlate { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public string? Status { get; set; }
}

public class MaintenanceScheduleDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class MaintenanceOverviewDto
{
    public List<AdminVehicleDto> Vehicles { get; set; } = new();
    public List<MaintenanceScheduleDto> Schedules { get; set; } = new();
}

public class CreateMaintenanceRequest
{
    public int VehicleId { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string? Note { get; set; }
}

// ---------------- 5. ADMIN SEATS ----------------
public class AdminTripHeaderDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
}

public class AdminTripSeatsResponseData
{
    public AdminTripHeaderDto Trip { get; set; } = new();
    public List<TripSeatDto> Seats { get; set; } = new();
    public List<RoutePointDto> PickupPoints { get; set; } = new();
    public List<RoutePointDto> DropoffPoints { get; set; } = new();
}

// ---------------- 6. ADMIN TICKETS ----------------
public class AdminTicketDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string TripCode { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;
    public string SeatCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class AdminTicketDetailDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public TicketTripSummaryDto Trip { get; set; } = new();
    public string SeatCode { get; set; } = string.Empty;
    public PassengerInfoDto Passenger { get; set; } = new();
    public string PickupPoint { get; set; } = string.Empty;
    public string DropoffPoint { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class CounterBookingRequest
{
    public int TripId { get; set; }
    public int SeatId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string? PassengerEmail { get; set; }
    public int PickupPointId { get; set; }
    public int DropoffPointId { get; set; }
}

public class CreateTicketResponseData
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
}

public class ConfirmTicketResponseData
{
    public int TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
}

// ---------------- 7. STAFF & PERMISSIONS ----------------
public class StaffAccountDto
{
    public int AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RolePermissionDto
{
    public string Role { get; set; } = string.Empty;
    public List<string> Functions { get; set; } = new();
}

public class StaffAccountsResponseData
{
    public List<StaffAccountDto> Accounts { get; set; } = new();
    public List<RolePermissionDto> Permissions { get; set; } = new();
}

public class LockStaffResponseData
{
    public int AccountId { get; set; }
    public string Status { get; set; } = string.Empty;
}

// ---------------- 8. CUSTOMERS ----------------
public class AdminCustomerDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TicketCount { get; set; }
}

public class AdminCustomerDetailDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<CustomerTicketSummaryDto> Tickets { get; set; } = new();
}

public class CustomerTicketSummaryDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

// ---------------- 10. REVENUE ----------------
public class RevenueReportDto
{
    public string FromDate { get; set; } = string.Empty;
    public string ToDate { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal OnlineRevenue { get; set; }
    public decimal OfflineRevenue { get; set; }
    public List<DailyRevenueDto> Details { get; set; } = new();
}

public class DailyRevenueDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Online { get; set; }
    public decimal Offline { get; set; }
    public decimal Total { get; set; }
}
