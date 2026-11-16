namespace BusBooking.Shared.DTOs;

// ---------------- TRIPS & SEATS ----------------
public class TripSearchItemDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime EstimatedArrival { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}

public class TripSeatsResponseData
{
    public int TripId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public List<TripSeatDto> Seats { get; set; } = new();
    public List<RoutePointDto> PickupPoints { get; set; } = new();
    public List<RoutePointDto> DropoffPoints { get; set; } = new();
}

public class TripSeatDto
{
    public int SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class RoutePointDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

// ---------------- BOOKINGS & MOMO PAYMENT ----------------
public class CreateBookingRequest
{
    public int TripId { get; set; }
    public int SeatId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string? PassengerEmail { get; set; }
    public int PickupPointId { get; set; }
    public int DropoffPointId { get; set; }
}

public class BookingResponseData
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public PaymentInfoDto Payment { get; set; } = new();
}

public class PaymentInfoDto
{
    public string Provider { get; set; } = "MoMo";
    public string OrderId { get; set; } = string.Empty;
    public string PayUrl { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
}

public class MomoIpnRequest
{
    public string PartnerCode { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string OrderInfo { get; set; } = string.Empty;
    public string OrderType { get; set; } = "momo_wallet";
    public long TransId { get; set; }
    public int ResultCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PayType { get; set; } = "qr";
    public long ResponseTime { get; set; }
    public string ExtraData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class PaymentStatusResponseData
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

public class PaymentConfirmedEventDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// ---------------- CUSTOMER TICKETS ----------------
public class MyTicketDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TicketDetailDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TicketTripSummaryDto Trip { get; set; } = new();
    public string SeatCode { get; set; } = string.Empty;
    public PassengerInfoDto Passenger { get; set; } = new();
    public string PickupPoint { get; set; } = string.Empty;
    public string DropoffPoint { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

public class TicketTripSummaryDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
}

public class PassengerInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
