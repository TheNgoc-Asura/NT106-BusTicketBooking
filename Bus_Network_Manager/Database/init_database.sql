-- =========================================================================================
-- BÁO CÁO ĐỒ ÁN MÔN HỌC: LẬP TRÌNH MẠNG CĂN BẢN (NT106.R12) - UIT
-- ĐỀ TÀI: HỆ THỐNG ĐẶT VÉ XE KHÁCH TRỰC TUYẾN (CLIENT - SERVER)
-- NHÓM 07
-- FILE: init_database.sql (MS SQL Server DDL Script & Seed Data)
-- =========================================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BusTicketDB')
BEGIN
    CREATE DATABASE BusTicketDB;
END
GO

USE BusTicketDB;
GO

-- 1. BẢNG NGƯỜI DÙNG (Users)
IF OBJECT_ID('NguoiDung', 'U') IS NOT NULL DROP TABLE NguoiDung;
CREATE TABLE NguoiDung (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer', -- Customer, Admin, Staff, Support
    Status NVARCHAR(20) NOT NULL DEFAULT N'Hoạt động', -- Hoạt động, Khóa
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_NguoiDung_Role CHECK (Role IN ('Customer', 'Admin', 'Staff', 'Support')),
    CONSTRAINT CK_NguoiDung_Status CHECK (Status IN (N'Hoạt động', N'Khóa'))
);
CREATE NONCLUSTERED INDEX IX_NguoiDung_Email ON NguoiDung(Email);
CREATE NONCLUSTERED INDEX IX_NguoiDung_Role ON NguoiDung(Role);

-- 2. BẢNG XÁC THỰC EMAIL (EmailVerifications)
IF OBJECT_ID('XacThucEmail', 'U') IS NOT NULL DROP TABLE XacThucEmail;
CREATE TABLE XacThucEmail (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(150) NOT NULL,
    Code VARCHAR(10) NOT NULL,
    TempToken VARCHAR(64) NULL,
    Type VARCHAR(30) NOT NULL DEFAULT 'Register', -- Register, ForgotPassword
    ExpiresAt DATETIME2 NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
CREATE NONCLUSTERED INDEX IX_XacThucEmail_Email_Code ON XacThucEmail(Email, Code);
CREATE NONCLUSTERED INDEX IX_XacThucEmail_TempToken ON XacThucEmail(TempToken);

-- 3. BẢNG TUYẾN XE (Routes)
IF OBJECT_ID('TuyenXe', 'U') IS NOT NULL DROP TABLE TuyenXe;
CREATE TABLE TuyenXe (
    RouteId INT IDENTITY(1,1) PRIMARY KEY,
    RouteCode VARCHAR(20) NOT NULL UNIQUE,
    Origin NVARCHAR(100) NOT NULL,
    Destination NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(8,2) NULL,
    DurationMinutes INT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Hoạt động', -- Hoạt động, Ngừng hoạt động
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_TuyenXe_Status CHECK (Status IN (N'Hoạt động', N'Ngừng hoạt động'))
);
CREATE NONCLUSTERED INDEX IX_TuyenXe_Origin_Destination ON TuyenXe(Origin, Destination);

-- 4. BẢNG ĐIỂM ĐÓN TRẢ (RouteStopPoints)
IF OBJECT_ID('DiemDonTra', 'U') IS NOT NULL DROP TABLE DiemDonTra;
CREATE TABLE DiemDonTra (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RouteId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Address NVARCHAR(250) NOT NULL,
    IsPickup BIT NOT NULL DEFAULT 1, -- 1: Đón, 0: Trả
    OrderIndex INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_DiemDonTra_TuyenXe FOREIGN KEY (RouteId) REFERENCES TuyenXe(RouteId) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_DiemDonTra_RouteId ON DiemDonTra(RouteId);

-- 5. BẢNG LOẠI XE (VehicleTypes)
IF OBJECT_ID('LoaiXe', 'U') IS NOT NULL DROP TABLE LoaiXe;
CREATE TABLE LoaiXe (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    SeatCount INT NOT NULL,
    Description NVARCHAR(255) NULL
);

-- 6. BẢNG XE (Vehicles)
IF OBJECT_ID('Xe', 'U') IS NOT NULL DROP TABLE Xe;
CREATE TABLE Xe (
    VehicleId INT IDENTITY(1,1) PRIMARY KEY,
    LicensePlate VARCHAR(20) NOT NULL UNIQUE,
    VehicleTypeId INT NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Hoạt động', -- Hoạt động, Bảo trì, Ngừng hoạt động
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Xe_LoaiXe FOREIGN KEY (VehicleTypeId) REFERENCES LoaiXe(Id),
    CONSTRAINT CK_Xe_Status CHECK (Status IN (N'Hoạt động', N'Bảo trì', N'Ngừng hoạt động'))
);

-- 7. BẢNG BẢO TRÌ XE (VehicleMaintenances)
IF OBJECT_ID('BaoTriXe', 'U') IS NOT NULL DROP TABLE BaoTriXe;
CREATE TABLE BaoTriXe (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Note NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_BaoTriXe_Xe FOREIGN KEY (VehicleId) REFERENCES Xe(VehicleId) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_BaoTriXe_VehicleId ON BaoTriXe(VehicleId);

-- 8. BẢNG CHUYẾN XE (Trips)
IF OBJECT_ID('ChuyenXe', 'U') IS NOT NULL DROP TABLE ChuyenXe;
CREATE TABLE ChuyenXe (
    TripId INT IDENTITY(1,1) PRIMARY KEY,
    TripCode VARCHAR(20) NOT NULL UNIQUE,
    RouteId INT NOT NULL,
    VehicleId INT NOT NULL,
    DriverId INT NOT NULL,
    AssistantId INT NULL,
    DepartureTime DATETIME2 NOT NULL,
    EstimatedArrival DATETIME2 NOT NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    Surcharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Sắp khởi hành', -- Sắp khởi hành, Đang chạy, Hoàn thành, Đã hủy
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ChuyenXe_TuyenXe FOREIGN KEY (RouteId) REFERENCES TuyenXe(RouteId),
    CONSTRAINT FK_ChuyenXe_Xe FOREIGN KEY (VehicleId) REFERENCES Xe(VehicleId),
    CONSTRAINT FK_ChuyenXe_Driver FOREIGN KEY (DriverId) REFERENCES NguoiDung(Id),
    CONSTRAINT FK_ChuyenXe_Assistant FOREIGN KEY (AssistantId) REFERENCES NguoiDung(Id),
    CONSTRAINT CK_ChuyenXe_Status CHECK (Status IN (N'Sắp khởi hành', N'Đang chạy', N'Hoàn thành', N'Đã hủy'))
);
CREATE NONCLUSTERED INDEX IX_ChuyenXe_RouteId_Departure ON ChuyenXe(RouteId, DepartureTime);

-- 9. BẢNG GHẾ CỦA CHUYẾN (Seats)
IF OBJECT_ID('Ghe', 'U') IS NOT NULL DROP TABLE Ghe;
CREATE TABLE Ghe (
    SeatId INT IDENTITY(1,1) PRIMARY KEY,
    TripId INT NOT NULL,
    SeatCode VARCHAR(10) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Trống', -- Trống, Chờ xác nhận, Đã đặt
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    LockedUntil DATETIME2 NULL, -- Hỗ trợ lock giữ ghế trong 10-15 phút khi đang thanh toán
    CONSTRAINT FK_Ghe_ChuyenXe FOREIGN KEY (TripId) REFERENCES ChuyenXe(TripId) ON DELETE CASCADE,
    CONSTRAINT UQ_Ghe_Trip_SeatCode UNIQUE (TripId, SeatCode),
    CONSTRAINT CK_Ghe_Status CHECK (Status IN (N'Trống', N'Chờ xác nhận', N'Đã đặt'))
);
CREATE NONCLUSTERED INDEX IX_Ghe_TripId_Status ON Ghe(TripId, Status);

-- 10. BẢNG VÉ XE (Tickets)
IF OBJECT_ID('VeXe', 'U') IS NOT NULL DROP TABLE VeXe;
CREATE TABLE VeXe (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    TicketCode VARCHAR(30) NOT NULL UNIQUE,
    TripId INT NOT NULL,
    SeatId INT NOT NULL,
    CustomerId INT NULL, -- NULL nếu mua trực tiếp tại quầy không dùng tài khoản
    PassengerName NVARCHAR(150) NOT NULL,
    PassengerPhone VARCHAR(20) NOT NULL,
    PassengerEmail NVARCHAR(150) NULL,
    PickupPointId INT NOT NULL,
    DropoffPointId INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Channel NVARCHAR(20) NOT NULL DEFAULT 'Online', -- Online, Trực tiếp
    Status NVARCHAR(30) NOT NULL DEFAULT N'Chờ thanh toán', -- Chờ thanh toán, Đã thanh toán, Đã xác nhận, Đã hủy
    BookedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    PaidAt DATETIME2 NULL,
    ConfirmedAt DATETIME2 NULL,
    CancelledAt DATETIME2 NULL,
    CONSTRAINT FK_VeXe_ChuyenXe FOREIGN KEY (TripId) REFERENCES ChuyenXe(TripId),
    CONSTRAINT FK_VeXe_Ghe FOREIGN KEY (SeatId) REFERENCES Ghe(SeatId),
    CONSTRAINT FK_VeXe_NguoiDung FOREIGN KEY (CustomerId) REFERENCES NguoiDung(Id),
    CONSTRAINT FK_VeXe_Pickup FOREIGN KEY (PickupPointId) REFERENCES DiemDonTra(Id),
    CONSTRAINT FK_VeXe_Dropoff FOREIGN KEY (DropoffPointId) REFERENCES DiemDonTra(Id),
    CONSTRAINT CK_VeXe_Channel CHECK (Channel IN ('Online', N'Trực tiếp')),
    CONSTRAINT CK_VeXe_Status CHECK (Status IN (N'Chờ thanh toán', N'Đã thanh toán', N'Đã xác nhận', N'Đã hủy'))
);
CREATE NONCLUSTERED INDEX IX_VeXe_CustomerId ON VeXe(CustomerId);
CREATE NONCLUSTERED INDEX IX_VeXe_TripId ON VeXe(TripId);
CREATE NONCLUSTERED INDEX IX_VeXe_Status ON VeXe(Status);

-- 11. BẢNG THANH TOÁN (Payments)
IF OBJECT_ID('ThanhToan', 'U') IS NOT NULL DROP TABLE ThanhToan;
CREATE TABLE ThanhToan (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TicketId INT NOT NULL,
    Provider NVARCHAR(50) NOT NULL DEFAULT 'MoMo',
    OrderId VARCHAR(50) NOT NULL UNIQUE,
    RequestId VARCHAR(50) NOT NULL,
    TransId BIGINT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PayType NVARCHAR(30) NULL DEFAULT 'qr',
    ResultCode INT NOT NULL DEFAULT 0,
    Message NVARCHAR(255) NULL,
    ResponseTime BIGINT NULL,
    Signature VARCHAR(256) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ThanhToan_VeXe FOREIGN KEY (TicketId) REFERENCES VeXe(TicketId) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_ThanhToan_OrderId ON ThanhToan(OrderId);
CREATE NONCLUSTERED INDEX IX_ThanhToan_TicketId ON ThanhToan(TicketId);

-- 12. BẢNG THÔNG BÁO (Notifications)
IF OBJECT_ID('ThongBao', 'U') IS NOT NULL DROP TABLE ThongBao;
CREATE TABLE ThongBao (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Type NVARCHAR(50) NOT NULL DEFAULT 'PaymentConfirmed',
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ThongBao_NguoiDung FOREIGN KEY (UserId) REFERENCES NguoiDung(Id) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_ThongBao_UserId_IsRead ON ThongBao(UserId, IsRead);

-- 13. BẢNG TICKET HỖ TRỢ (SupportTickets)
IF OBJECT_ID('TicketHoTro', 'U') IS NOT NULL DROP TABLE TicketHoTro;
CREATE TABLE TicketHoTro (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý', -- Chờ xử lý, Đang xử lý, Chờ phản hồi người dùng, Đã xử lý
    HandlerId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TicketHoTro_Customer FOREIGN KEY (CustomerId) REFERENCES NguoiDung(Id),
    CONSTRAINT FK_TicketHoTro_Handler FOREIGN KEY (HandlerId) REFERENCES NguoiDung(Id),
    CONSTRAINT CK_TicketHoTro_Status CHECK (Status IN (N'Chờ xử lý', N'Đang xử lý', N'Chờ phản hồi người dùng', N'Đã xử lý'))
);
CREATE NONCLUSTERED INDEX IX_TicketHoTro_CustomerId ON TicketHoTro(CustomerId);
CREATE NONCLUSTERED INDEX IX_TicketHoTro_Status ON TicketHoTro(Status);

-- 14. BẢNG TIN NHẮN HỖ TRỢ (SupportMessages)
IF OBJECT_ID('TinNhanHoTro', 'U') IS NOT NULL DROP TABLE TinNhanHoTro;
CREATE TABLE TinNhanHoTro (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TicketId INT NOT NULL,
    SenderId INT NOT NULL,
    SenderRole NVARCHAR(30) NOT NULL DEFAULT 'Customer', -- Customer, Support, Admin
    Content NVARCHAR(MAX) NOT NULL,
    SentAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TinNhanHoTro_Ticket FOREIGN KEY (TicketId) REFERENCES TicketHoTro(TicketId) ON DELETE CASCADE,
    CONSTRAINT FK_TinNhanHoTro_Sender FOREIGN KEY (SenderId) REFERENCES NguoiDung(Id)
);
CREATE NONCLUSTERED INDEX IX_TinNhanHoTro_TicketId_SentAt ON TinNhanHoTro(TicketId, SentAt);

-- =========================================================================================
-- DỮ LIỆU MẪU (SEED DATA)
-- Mật khẩu mặc định của tất cả tài khoản mẫu là: Matkhau@123 (mã băm SHA256)
-- Hash SHA256 của "Matkhau@123": a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3
-- =========================================================================================

-- Người dùng mẫu
INSERT INTO NguoiDung (FullName, Email, PasswordHash, Phone, Role, Status) VALUES
(N'Quản Trị Viên Hệ Thống', 'admin@nhaxe.vn', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '0901000001', 'Admin', N'Hoạt động'),
(N'Trần Văn B (Nhân Viên Điều Hành)', 'tranvanb@nhaxe.vn', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '0901000002', 'Staff', N'Hoạt động'),
(N'Lê Văn C (Tài Xế)', 'levanc@nhaxe.vn', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '0901000003', 'Staff', N'Hoạt động'),
(N'Nguyễn Văn Tài (Phụ Xe)', 'nguyenvantai@nhaxe.vn', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '0901000004', 'Staff', N'Hoạt động'),
(N'Phạm Thị Hỗ Trợ (CSKH)', 'support@nhaxe.vn', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '0901000005', 'Support', N'Hoạt động'),
(N'Nguyễn Văn A (Khách Hàng)', 'nguyenvana@gmail.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '0901234567', 'Customer', N'Hoạt động'),
(N'Vũ Hoàng Phúc (Khách Hàng)', 'phucvu@gmail.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '0987654321', 'Customer', N'Hoạt động');

-- Loại xe
INSERT INTO LoaiXe (Name, SeatCount, Description) VALUES
(N'Giường nằm 40 chỗ', 40, N'Xe giường nằm 2 tầng cao cấp trang bị điều hòa, wifi, cổng sạc'),
(N'Limousine 9 chỗ', 9, N'Xe Limousine VIP ghế massage, màn hình giải trí riêng'),
(N'Ghế ngồi 29 chỗ', 29, N'Xe ghế ngồi đời mới tiện nghi cho các tuyến ngắn');

-- Xe
INSERT INTO Xe (LicensePlate, VehicleTypeId, Status) VALUES
('51B-123.45', 1, N'Hoạt động'),
('51B-678.90', 2, N'Hoạt động'),
('51B-456.78', 3, N'Hoạt động'),
('51B-999.99', 1, N'Bảo trì');

-- Lịch bảo trì mẫu
INSERT INTO BaoTriXe (VehicleId, StartDate, EndDate, Note) VALUES
(4, '2026-11-10', '2026-11-18', N'Bảo dưỡng định kỳ phanh và lốp xe');

-- Tuyến xe
INSERT INTO TuyenXe (RouteCode, Origin, Destination, DistanceKm, DurationMinutes, Status) VALUES
('T001', N'TP.HCM', N'Đà Lạt', 308.0, 450, N'Hoạt động'),
('T002', N'TP.HCM', N'Vũng Tàu', 105.0, 150, N'Hoạt động'),
('T003', N'TP.HCM', N'Nha Trang', 435.0, 540, N'Hoạt động');

-- Điểm đón trả
INSERT INTO DiemDonTra (RouteId, Name, Address, IsPickup, OrderIndex) VALUES
(1, N'Bến xe Miền Đông Mới', N'501 Hoàng Hữu Nam, TP. Thủ Đức, TP.HCM', 1, 1),
(1, N'Văn phòng Hàng Xanh', N'486 Điện Biên Phủ, Bình Thạnh, TP.HCM', 1, 2),
(1, N'Bến xe Liên tỉnh Đà Lạt', N'01 Tô Hiến Thành, Phường 3, Đà Lạt', 0, 1),
(1, N'Ngã 3 Đa Phú', N'Đường Ankroet, Phường 7, Đà Lạt', 0, 2),
(2, N'Bến xe Miền Tây', N'395 Kinh Dương Vương, Bình Tân, TP.HCM', 1, 1),
(2, N'Bến xe Vũng Tàu', N'Nam Kỳ Khởi Nghĩa, Phường Thắng Tam, Vũng Tàu', 0, 1);

-- Chuyến xe mẫu
INSERT INTO ChuyenXe (TripCode, RouteId, VehicleId, DriverId, AssistantId, DepartureTime, EstimatedArrival, Price, Surcharge, Status) VALUES
('CH0101', 1, 1, 3, 4, DATEADD(DAY, 1, CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' 08:00:00' AS DATETIME2)), DATEADD(DAY, 1, CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' 15:30:00' AS DATETIME2)), 280000, 0, N'Sắp khởi hành'),
('CH0102', 1, 1, 3, 4, DATEADD(DAY, 1, CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' 20:00:00' AS DATETIME2)), DATEADD(DAY, 2, CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' 03:30:00' AS DATETIME2)), 300000, 0, N'Sắp khởi hành'),
('CH0201', 2, 2, 3, NULL, DATEADD(DAY, 1, CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' 09:00:00' AS DATETIME2)), DATEADD(DAY, 1, CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' 11:30:00' AS DATETIME2)), 160000, 0, N'Sắp khởi hành');

-- Sinh sơ đồ ghế mẫu cho chuyến 1 (CH0101 - 40 ghế)
DECLARE @TripId INT = 1;
DECLARE @i INT = 1;
WHILE @i <= 20
BEGIN
    INSERT INTO Ghe (TripId, SeatCode, Status, Price)
    VALUES (@TripId, 'A' + RIGHT('0' + CAST(@i AS VARCHAR(2)), 2), N'Trống', 280000);
    SET @i = @i + 1;
END;
SET @i = 1;
WHILE @i <= 20
BEGIN
    INSERT INTO Ghe (TripId, SeatCode, Status, Price)
    VALUES (@TripId, 'B' + RIGHT('0' + CAST(@i AS VARCHAR(2)), 2), N'Trống', 280000);
    SET @i = @i + 1;
END;

-- Đặt trước ghế A01 và A02 cho demo
UPDATE Ghe SET Status = N'Đã đặt' WHERE TripId = 1 AND SeatCode = 'A01';
UPDATE Ghe SET Status = N'Chờ xác nhận' WHERE TripId = 1 AND SeatCode = 'A02';

-- Vé mẫu
INSERT INTO VeXe (TicketCode, TripId, SeatId, CustomerId, PassengerName, PassengerPhone, PassengerEmail, PickupPointId, DropoffPointId, TotalAmount, Channel, Status, BookedAt, PaidAt)
VALUES ('VE20261109001', 1, 1, 6, N'Nguyễn Văn A', '0901234567', 'nguyenvana@gmail.com', 1, 3, 280000, 'Online', N'Đã thanh toán', GETDATE(), GETDATE());

-- Thanh toán MoMo mẫu
INSERT INTO ThanhToan (TicketId, Provider, OrderId, RequestId, TransId, Amount, PayType, ResultCode, Message, ResponseTime, Signature)
VALUES (1, 'MoMo', 'VE20261109001', 'RQ1762650000001', 4088878653, 280000, 'qr', 0, 'Successful.', 1762650063942, '7b9f4ca728076c32f16041cbc917ebf5');

-- Thông báo mẫu
INSERT INTO ThongBao (UserId, Type, Title, Content, IsRead) VALUES
(6, 'PaymentConfirmed', N'Thanh toán thành công', N'Vé VE20261109001 tuyến TP.HCM - Đà Lạt đã được thanh toán thành công qua MoMo.', 0),
(6, 'TripReminder', N'Nhắc nhở khởi hành', N'Chuyến xe CH0101 của quý khách sẽ khởi hành lúc 08:00 ngày mai tại Bến xe Miền Đông.', 0);

-- Ticket hỗ trợ mẫu
INSERT INTO TicketHoTro (CustomerId, Title, Status, HandlerId) VALUES
(6, N'Hỏi về thời gian đón tại bến xe Miền Đông', N'Đang xử lý', 5);

INSERT INTO TinNhanHoTro (TicketId, SenderId, SenderRole, Content, SentAt) VALUES
(1, 6, 'Customer', N'Chào nhà xe, mình muốn hỏi xe có đón khách đúng 8h tại bến không ạ?', DATEADD(MINUTE, -20, GETDATE())),
(1, 5, 'Support', N'Dạ chào quý khách, xe xuất bến lúc 8h00, quý khách vui lòng có mặt trước 15-30 phút để hoàn tất thủ tục lên xe nhé.', DATEADD(MINUTE, -15, GETDATE()));

GO
PRINT N'Khởi tạo Cơ sở dữ liệu BusTicketDB và dữ liệu mẫu thành công!';
