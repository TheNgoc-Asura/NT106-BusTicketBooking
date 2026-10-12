CREATE DATABASE QuanLyXe_DoAn CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE QuanLyXe_DoAn;


CREATE TABLE TUYEN_XE (
    ID_TuyenXe INT PRIMARY KEY,
    DiemDi NVARCHAR(100),
    DiemDen NVARCHAR(100),
    KhoangCach_km DECIMAL(6,2),
    ThoiGianDuKien NVARCHAR(50)
);

CREATE TABLE XE (
    ID_Xe INT PRIMARY KEY,
    BienSoXe VARCHAR(20),
    LoaiXe NVARCHAR(50),
    TongSoGhe INT,
    TinhTrangBaoTri NVARCHAR(50)
);

CREATE TABLE TAI_XE (
    ID_TaiXe INT PRIMARY KEY,
    HoTen NVARCHAR(100),
    SoDienThoai VARCHAR(15)
);

CREATE TABLE NGUOI_DUNG (
    ID_NguoiDung INT PRIMARY KEY,
    HoTen NVARCHAR(100),
    SoDienThoai VARCHAR(15),
    MatKhau VARCHAR(255),
    VaiTro NVARCHAR(50)
);

CREATE TABLE SO_DO_GHE (
    ID_Ghe INT PRIMARY KEY,
    ID_Xe INT,
    MaGhe VARCHAR(10),
    TrangThaiGhe NVARCHAR(50),
    FOREIGN KEY (ID_Xe) REFERENCES XE(ID_Xe)
);

CREATE TABLE CHUYEN_XE (
    ID_ChuyenXe INT PRIMARY KEY,
    ID_TuyenXe INT,
    ID_Xe INT,
    ID_TaiXe INT,
    ThoiGianKhoiHanh DATETIME,
    GiaVeCoBan DECIMAL(12,2),
    ChiPhiPhatSinh DECIMAL(12,2),
    FOREIGN KEY (ID_TuyenXe) REFERENCES TUYEN_XE(ID_TuyenXe),
    FOREIGN KEY (ID_Xe) REFERENCES XE(ID_Xe),
    FOREIGN KEY (ID_TaiXe) REFERENCES TAI_XE(ID_TaiXe)
);

CREATE TABLE THONG_BAO (
    ID_ThongBao INT PRIMARY KEY,
    ID_NguoiDung INT,
    NoiDung NVARCHAR(255),
    ThoiGian DATETIME,
    FOREIGN KEY (ID_NguoiDung) REFERENCES NGUOI_DUNG(ID_NguoiDung)
);

CREATE TABLE YEU_CAU_HO_TRO (
    ID_Ticket INT PRIMARY KEY,
    ID_NguoiDung INT,
    NoiDung_VanDe NVARCHAR(255),
    TrangThai_XuLy NVARCHAR(50),
    FOREIGN KEY (ID_NguoiDung) REFERENCES NGUOI_DUNG(ID_NguoiDung)
);

CREATE TABLE VE (
    ID_Ve INT PRIMARY KEY,
    ID_ChuyenXe INT,
    ID_NguoiDung INT,
    ID_Ghe INT,
    DiemDon_TrungChuyen NVARCHAR(255),
    PhuongThucThanhToan NVARCHAR(50),
    TongTien DECIMAL(12,2),
    TrangThaiVe NVARCHAR(50),
    FOREIGN KEY (ID_ChuyenXe) REFERENCES CHUYEN_XE(ID_ChuyenXe),
    FOREIGN KEY (ID_NguoiDung) REFERENCES NGUOI_DUNG(ID_NguoiDung),
    FOREIGN KEY (ID_Ghe) REFERENCES SO_DO_GHE(ID_Ghe)
);

-- =========================================================
-- BƯỚC 3: THÊM DỮ LIỆU MẪU (SẮM ĐỒ ĐẠC)
-- =========================================================
INSERT INTO TUYEN_XE (ID_TuyenXe, DiemDi, DiemDen, KhoangCach_km, ThoiGianDuKien)
VALUES (1, N'TP. Hồ Chí Minh', N'Đà Lạt', 300.50, N'8 tiếng');

INSERT INTO XE (ID_Xe, BienSoXe, LoaiXe, TongSoGhe, TinhTrangBaoTri)
VALUES (1, '51B-123.45', N'Xe giường nằm cao cấp', 40, N'Hoạt động tốt');

INSERT INTO TAI_XE (ID_TaiXe, HoTen, SoDienThoai)
VALUES (1, N'Nguyễn Văn Tài', '0901234567');

INSERT INTO NGUOI_DUNG (ID_NguoiDung, HoTen, SoDienThoai, MatKhau, VaiTro)
VALUES (1, N'Lê Anh Quân', '0987654321', 'hashed_pass_123', N'Khách hàng');

INSERT INTO SO_DO_GHE (ID_Ghe, ID_Xe, MaGhe, TrangThaiGhe)
VALUES (1, 1, 'A01', N'Trống');

INSERT INTO CHUYEN_XE (ID_ChuyenXe, ID_TuyenXe, ID_Xe, ID_TaiXe, ThoiGianKhoiHanh, GiaVeCoBan, ChiPhiPhatSinh)
VALUES (1, 1, 1, 1, '2026-10-15 22:00:00', 350000.00, 0.00);

INSERT INTO VE (ID_Ve, ID_ChuyenXe, ID_NguoiDung, ID_Ghe, DiemDon_TrungChuyen, PhuongThucThanhToan, TongTien, TrangThaiVe)
VALUES (1, 1, 1, 1, N'Bến xe Miền Đông', N'Chuyển khoản VNPay', 350000.00, N'Đã thanh toán');