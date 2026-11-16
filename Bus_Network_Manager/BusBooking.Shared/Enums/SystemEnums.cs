namespace BusBooking.Shared.Enums;

/// <summary>
/// Vai trò người dùng trong hệ thống.
/// </summary>
public static class UserRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Support = "Support";
}

/// <summary>
/// Trạng thái tài khoản người dùng.
/// </summary>
public static class AccountStatuses
{
    public const string Active = "Hoạt động";
    public const string Locked = "Khóa";
}

/// <summary>
/// Mục đích mã OTP xác thực email.
/// </summary>
public static class OtpPurposes
{
    public const string Register = "Register";
    public const string ForgotPassword = "ForgotPassword";
}

/// <summary>
/// Trạng thái tuyến xe.
/// </summary>
public static class RouteStatuses
{
    public const string Active = "Hoạt động";
    public const string Inactive = "Ngừng hoạt động";
}

/// <summary>
/// Loại điểm dừng trên tuyến (điểm đón hoặc điểm trả).
/// </summary>
public static class RoutePointTypes
{
    public const string Pickup = "Pickup";
    public const string Dropoff = "Dropoff";
}

/// <summary>
/// Trạng thái phương tiện (xe).
/// </summary>
public static class VehicleStatuses
{
    public const string Active = "Hoạt động";
    public const string Maintenance = "Bảo trì";
    public const string Inactive = "Ngừng hoạt động";
}

/// <summary>
/// Trạng thái chuyến xe.
/// </summary>
public static class TripStatuses
{
    public const string Upcoming = "Sắp khởi hành";
    public const string Running = "Đang chạy";
    public const string Completed = "Hoàn thành";
    public const string Cancelled = "Đã hủy";
}

/// <summary>
/// Trạng thái ghế trên chuyến xe.
/// </summary>
public static class SeatStatuses
{
    public const string Available = "Trống";
    public const string Booked = "Đã đặt";
    public const string Pending = "Chờ xác nhận";
}

/// <summary>
/// Kênh bán vé.
/// </summary>
public static class BookingChannels
{
    public const string Online = "Online";
    public const string Direct = "Trực tiếp";
}

/// <summary>
/// Trạng thái vé xe.
/// </summary>
public static class TicketStatuses
{
    public const string PendingPayment = "Chờ thanh toán";
    public const string Paid = "Đã thanh toán";
    public const string Confirmed = "Đã xác nhận";
    public const string Cancelled = "Đã hủy";
}

/// <summary>
/// Phương thức thanh toán.
/// </summary>
public static class PaymentMethods
{
    public const string MoMo = "MoMo";
    public const string Cash = "Tiền mặt";
}

/// <summary>
/// Trạng thái ticket hỗ trợ khách hàng.
/// </summary>
public static class SupportTicketStatuses
{
    public const string Pending = "Chờ xử lý";
    public const string Processing = "Đang xử lý";
    public const string WaitingCustomer = "Chờ phản hồi người dùng";
    public const string Resolved = "Đã xử lý";
}

/// <summary>
/// Loại thông báo gửi tới người dùng.
/// </summary>
public static class NotificationTypes
{
    public const string PaymentConfirmed = "PaymentConfirmed";
    public const string BookingCreated = "BookingCreated";
    public const string TripReminder = "TripReminder";
    public const string TripUpdated = "TripUpdated";
    public const string TripCancelled = "TripCancelled";
}
