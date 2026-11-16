-- =========================================================================================
-- ĐỀ TÀI: ỨNG DỤNG ĐẶT VÉ XE TRỰC TUYẾN (BusBooking) - MÔN NT106.R12 (NHÓM 07 - UIT)
-- FILE: BusBookingDb_Schema.sql (MS SQL Server DDL Script & Seed Data)
-- Ghi chú: Schema khớp 100% với BusBookingDbContext (Entity Framework Core)
-- =========================================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BusBookingDb')
BEGIN
    CREATE DATABASE BusBookingDb;
END
GO

USE BusBookingDb;
GO

-- 1. Users
IF OBJECT_ID('SupportMessages', 'U') IS NOT NULL DROP TABLE SupportMessages;
IF OBJECT_ID('SupportTickets', 'U') IS NOT NULL DROP TABLE SupportTickets;
IF OBJECT_ID('Notifications', 'U') IS NOT NULL DROP TABLE Notifications;
IF OBJECT_ID('Tickets', 'U') IS NOT NULL DROP TABLE Tickets;
IF OBJECT_ID('TripSeats', 'U') IS NOT NULL DROP TABLE TripSeats;
IF OBJECT_ID('Trips', 'U') IS NOT NULL DROP TABLE Trips;
IF OBJECT_ID('MaintenanceSchedules', 'U') IS NOT NULL DROP TABLE MaintenanceSchedules;
IF OBJECT_ID('Vehicles', 'U') IS NOT NULL DROP TABLE Vehicles;
IF OBJECT_ID('VehicleTypes', 'U') IS NOT NULL DROP TABLE VehicleTypes;
IF OBJECT_ID('RoutePoints', 'U') IS NOT NULL DROP TABLE RoutePoints;
IF OBJECT_ID('Routes', 'U') IS NOT NULL DROP TABLE Routes;
IF OBJECT_ID('OtpCodes', 'U') IS NOT NULL DROP TABLE OtpCodes;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
GO

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer',
    Status NVARCHAR(20) NOT NULL DEFAULT N'Hoạt động',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer', 'Admin', 'Staff', 'Support')),
    CONSTRAINT CK_Users_Status CHECK (Status IN (N'Hoạt động', N'Khóa'))
);
CREATE NONCLUSTERED INDEX IX_Users_Role ON Users(Role);

-- 2. OtpCodes
CREATE TABLE OtpCodes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(150) NOT NULL,
    Code VARCHAR(10) NOT NULL,
    Purpose VARCHAR(30) NOT NULL DEFAULT 'Register',
    TempToken VARCHAR(64) NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    IsUsed BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_OtpCodes_Purpose CHECK (Purpose IN ('Register', 'ForgotPassword'))
);
CREATE NONCLUSTERED INDEX IX_OtpCodes_Email_Purpose_Code ON OtpCodes(Email, Purpose, Code);
CREATE NONCLUSTERED INDEX IX_OtpCodes_TempToken ON OtpCodes(TempToken);

-- 3. Routes
CREATE TABLE Routes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RouteCode VARCHAR(20) NOT NULL UNIQUE,
    Origin NVARCHAR(100) NOT NULL,
    Destination NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(8,2) NULL,
    DurationMinutes INT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Hoạt động',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Routes_Origin_Destination UNIQUE (Origin, Destination),
    CONSTRAINT CK_Routes_Status CHECK (Status IN (N'Hoạt động', N'Ngừng hoạt động'))
);

-- 4. RoutePoints
CREATE TABLE RoutePoints (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RouteId INT NOT NULL,
    Type VARCHAR(20) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Address NVARCHAR(250) NOT NULL,
    OrderIndex INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_RoutePoints_Routes FOREIGN KEY (RouteId) REFERENCES Routes(Id) ON DELETE CASCADE,
    CONSTRAINT CK_RoutePoints_Type CHECK (Type IN ('Pickup', 'Dropoff'))
);
CREATE NONCLUSTERED INDEX IX_RoutePoints_RouteId_Type ON RoutePoints(RouteId, Type);

-- 5. VehicleTypes
CREATE TABLE VehicleTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    SeatCount INT NOT NULL,
    Description NVARCHAR(255) NULL
);

-- 6. Vehicles
CREATE TABLE Vehicles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LicensePlate VARCHAR(20) NOT NULL UNIQUE,
    VehicleTypeId INT NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Hoạt động',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Vehicles_VehicleTypes FOREIGN KEY (VehicleTypeId) REFERENCES VehicleTypes(Id),
    CONSTRAINT CK_Vehicles_Status CHECK (Status IN (N'Hoạt động', N'Bảo trì', N'Ngừng hoạt động'))
);

-- 7. MaintenanceSchedules
CREATE TABLE MaintenanceSchedules (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Note NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_MaintenanceSchedules_Vehicles FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_MaintenanceSchedules_VehicleId ON MaintenanceSchedules(VehicleId);

-- 8. Trips
CREATE TABLE Trips (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TripCode VARCHAR(20) NOT NULL UNIQUE,
    RouteId INT NOT NULL,
    VehicleId INT NOT NULL,
    DriverId INT NOT NULL,
    AssistantId INT NULL,
    DepartureTime DATETIME2 NOT NULL,
    EstimatedArrival DATETIME2 NOT NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    Surcharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Sắp khởi hành',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Trips_Routes FOREIGN KEY (RouteId) REFERENCES Routes(Id),
    CONSTRAINT FK_Trips_Vehicles FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id),
    CONSTRAINT FK_Trips_Driver FOREIGN KEY (DriverId) REFERENCES Users(Id),
    CONSTRAINT FK_Trips_Assistant FOREIGN KEY (AssistantId) REFERENCES Users(Id),
    CONSTRAINT CK_Trips_Status CHECK (Status IN (N'Sắp khởi hành', N'Đang chạy', N'Hoàn thành', N'Đã hủy'))
);
CREATE NONCLUSTERED INDEX IX_Trips_RouteId_DepartureTime ON Trips(RouteId, DepartureTime);

-- 9. TripSeats
CREATE TABLE TripSeats (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TripId INT NOT NULL,
    SeatCode VARCHAR(10) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Trống',
    LockedUntil DATETIME2 NULL,
    RowVersion ROWVERSION NULL,
    CONSTRAINT FK_TripSeats_Trips FOREIGN KEY (TripId) REFERENCES Trips(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TripSeats_TripId_SeatCode UNIQUE (TripId, SeatCode),
    CONSTRAINT CK_TripSeats_Status CHECK (Status IN (N'Trống', N'Đã đặt', N'Chờ xác nhận'))
);
CREATE NONCLUSTERED INDEX IX_TripSeats_TripId_Status ON TripSeats(TripId, Status);

-- 10. Tickets
CREATE TABLE Tickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TicketCode VARCHAR(30) NOT NULL UNIQUE,
    TripId INT NOT NULL,
    SeatId INT NOT NULL,
    CustomerId INT NULL,
    PassengerName NVARCHAR(150) NOT NULL,
    PassengerPhone VARCHAR(20) NOT NULL,
    PassengerEmail NVARCHAR(150) NULL,
    PickupPointId INT NOT NULL,
    DropoffPointId INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Channel NVARCHAR(20) NOT NULL DEFAULT 'Online',
    Status NVARCHAR(30) NOT NULL DEFAULT N'Chờ thanh toán',
    PaymentMethod NVARCHAR(50) NULL,
    PaymentTransId BIGINT NULL,
    PaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Tickets_Trips FOREIGN KEY (TripId) REFERENCES Trips(Id),
    CONSTRAINT FK_Tickets_TripSeats FOREIGN KEY (SeatId) REFERENCES TripSeats(Id),
    CONSTRAINT FK_Tickets_Users FOREIGN KEY (CustomerId) REFERENCES Users(Id),
    CONSTRAINT FK_Tickets_PickupPoint FOREIGN KEY (PickupPointId) REFERENCES RoutePoints(Id),
    CONSTRAINT FK_Tickets_DropoffPoint FOREIGN KEY (DropoffPointId) REFERENCES RoutePoints(Id),
    CONSTRAINT CK_Tickets_Channel CHECK (Channel IN ('Online', N'Trực tiếp')),
    CONSTRAINT CK_Tickets_Status CHECK (Status IN (N'Chờ thanh toán', N'Đã thanh toán', N'Đã xác nhận', N'Đã hủy'))
);
CREATE NONCLUSTERED INDEX IX_Tickets_CustomerId ON Tickets(CustomerId);
CREATE NONCLUSTERED INDEX IX_Tickets_TripId ON Tickets(TripId);
CREATE NONCLUSTERED INDEX IX_Tickets_Status ON Tickets(Status);

-- 11. Notifications
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);

-- 12. SupportTickets
CREATE TABLE SupportTickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý',
    HandlerId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_SupportTickets_Customer FOREIGN KEY (CustomerId) REFERENCES Users(Id),
    CONSTRAINT FK_SupportTickets_Handler FOREIGN KEY (HandlerId) REFERENCES Users(Id),
    CONSTRAINT CK_SupportTickets_Status CHECK (Status IN (N'Chờ xử lý', N'Đang xử lý', N'Chờ phản hồi người dùng', N'Đã xử lý'))
);
CREATE NONCLUSTERED INDEX IX_SupportTickets_CustomerId ON SupportTickets(CustomerId);
CREATE NONCLUSTERED INDEX IX_SupportTickets_Status ON SupportTickets(Status);

-- 13. SupportMessages
CREATE TABLE SupportMessages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SupportTicketId INT NOT NULL,
    SenderId INT NOT NULL,
    SenderRole NVARCHAR(30) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    SentAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_SupportMessages_SupportTickets FOREIGN KEY (SupportTicketId) REFERENCES SupportTickets(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SupportMessages_Users FOREIGN KEY (SenderId) REFERENCES Users(Id)
);
CREATE NONCLUSTERED INDEX IX_SupportMessages_SupportTicketId_SentAt ON SupportMessages(SupportTicketId, SentAt);
GO
