# BusBooking - Ứng dụng Đặt vé xe trực tuyến

Hệ thống đặt vé xe khách trực tuyến gồm:
- `BusBooking.Server`: ASP.NET Core Web API (.NET 8), Entity Framework Core, MS SQL Server, JWT & SignalR.
- `BusBooking.Client`: Giao diện desktop C# Windows Forms (.NET 8).
- `BusBooking.Shared`: Thư viện DTOs, Models và Enums dùng chung.

---

## 1. Yêu cầu cài đặt trên máy

1. **.NET 8 SDK** (tải tại trang chủ Microsoft .NET).
2. **MS SQL Server**:
   - Mặc định dùng **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`, thường có sẵn khi cài Visual Studio hoặc cài riêng gói `SqlLocalDB.msi`).
   - Nếu máy bạn dùng SQL Server bản khác (`SQLEXPRESS`, `MSSQLSERVER`...), xem mục **Cấu hình SQL Server** bên dưới.

---

## 2. Cách chạy nhanh

1. Giải nén thư mục dự án (nếu tải dạng file nén `.zip`/`.rar`).
2. Nhấn đúp chuột vào file **`run.bat`** ở thư mục gốc.
3. Hệ thống sẽ tự động:
   - Mở cửa sổ **BusBooking Server** tại `http://localhost:5000` (tự tạo database `BusBookingDb` và dữ liệu mẫu ở lần chạy đầu tiên).
   - Chờ 4 giây rồi mở giao diện **BusBooking Client**.

> **Lưu ý:** Giữ nguyên cửa sổ dòng lệnh của `BusBooking Server` trong suốt quá trình sử dụng Client. Khi đóng ứng dụng, đóng cả Client và cửa sổ Server.

---

## 3. Cấu hình SQL Server (nếu không dùng LocalDB)

Mở file `BusBooking.Server/appsettings.json` và sửa giá trị `DefaultConnection` cho khớp với SQL Server trên máy bạn:

- **Dùng SQL Server Express (`SQLEXPRESS`)**:
  ```json
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=BusBookingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
  ```
- **Dùng SQL Server với tài khoản `sa`**:
  ```json
  "DefaultConnection": "Server=localhost;Database=BusBookingDb;User Id=sa;Password=MatKhauCuaBan;MultipleActiveResultSets=true;TrustServerCertificate=True;"
  ```

---

## 4. Tài khoản mẫu để trải nghiệm

Ở màn hình đăng nhập có sẵn **4 nút điền nhanh tài khoản mẫu** (mật khẩu chung là `Matkhau@123`):

| Vai trò | Email | Chức năng chính |
|---|---|---|
| **Khách hàng** | `nguyenvana@gmail.com` | Tìm chuyến, chọn ghế, thanh toán QR, xem vé, chat hỗ trợ |
| **Admin** | `admin@nhaxe.vn` | Quản trị toàn hệ thống (Dashboard, chuyến, tuyến, xe, vé, phân quyền) |
| **Staff** | `staff@nhaxe.vn` | Điều hành chuyến xe, đặt/xác nhận vé tại quầy, tra cứu khách hàng |
| **Support** | `support@nhaxe.vn` | Tiếp nhận yêu cầu & chat hỗ trợ khách hàng realtime |

> **Mẹo test đặt vé:** Dữ liệu mẫu tạo sẵn 4 chuyến xe khởi hành vào **ngày mai và ngày kia** trên các tuyến `TP.HCM → Đà Lạt`, `TP.HCM → Vũng Tàu`, `Đà Lạt → TP.HCM`. Khi thanh toán trên môi trường local, bấm nút **"Giả lập MoMo thanh toán thành công (Local Demo)"** trên màn hình QR để hoàn tất vé.

---

## 5. Xử lý sự cố thường gặp

- **Lỗi không kết nối được Server**: Kiểm tra lại cửa sổ `BusBooking Server` đã mở và hiện `Now listening on: http://localhost:5000` chưa.
- **Lỗi kết nối SQL Server**: Kiểm tra dịch vụ SQL Server / LocalDB đã chạy chưa (với LocalDB có thể chạy `sqllocaldb start MSSQLLocalDB` trong Terminal) và kiểm tra lại `DefaultConnection` trong `BusBooking.Server/appsettings.json`.
- **Lỗi cổng `5000` đang được sử dụng**: Đóng các cửa sổ Server cũ đang chạy ngầm trước khi bấm `run.bat`.
