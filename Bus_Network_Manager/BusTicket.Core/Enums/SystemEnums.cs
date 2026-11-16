namespace BusTicket.Core.Enums;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Support = "Support";
}

public static class TicketStatuses
{
    public const string PendingPayment = "Chờ thanh toán";
    public const string Paid = "Đã thanh toán";
    public const string Confirmed = "Đã xác nhận";
    public const string Cancelled = "Đã hủy";
}

public static class SeatStatuses
{
    public const string Available = "Trống";
    public const string Pending = "Chờ xác nhận";
    public const string Booked = "Đã đặt";
}

public static class TripStatuses
{
    public const string Upcoming = "Sắp khởi hành";
    public const string Running = "Đang chạy";
    public const string Completed = "Hoàn thành";
    public const string Cancelled = "Đã hủy";
}

public static class RouteStatuses
{
    public const string Active = "Hoạt động";
    public const string Inactive = "Ngừng hoạt động";
}

public static class VehicleStatuses
{
    public const string Active = "Hoạt động";
    public const string Maintenance = "Bảo trì";
    public const string Inactive = "Ngừng hoạt động";
}

public static class AccountStatuses
{
    public const string Active = "Hoạt động";
    public const string Locked = "Khóa";
}

public static class SupportTicketStatuses
{
    public const string Pending = "Chờ xử lý";
    public const string Processing = "Đang xử lý";
    public const string WaitingCustomer = "Chờ phản hồi người dùng";
    public const string Resolved = "Đã xử lý";
}

public static class BookingChannels
{
    public const string Online = "Online";
    public const string Direct = "Trực tiếp";
}
