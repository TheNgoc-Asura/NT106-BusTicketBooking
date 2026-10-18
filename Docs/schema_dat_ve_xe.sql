

DROP DATABASE IF EXISTS he_thong_dat_ve_xe;
CREATE DATABASE he_thong_dat_ve_xe CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE he_thong_dat_ve_xe;

-- Tắt kiểm tra khóa ngoại tạm thời để tránh lỗi phụ thuộc thứ tự tạo bảng
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS THONG_BAO;
DROP TABLE IF EXISTS YEU_CAU_HO_TRO;
DROP TABLE IF EXISTS VE;
DROP TABLE IF EXISTS CHUYEN_XE;
DROP TABLE IF EXISTS TUYEN_XE;
DROP TABLE IF EXISTS SO_DO_GHE;
DROP TABLE IF EXISTS XE;
DROP TABLE IF EXISTS TAI_XE;
DROP TABLE IF EXISTS NGUOI_DUNG;

SET FOREIGN_KEY_CHECKS = 1;

-- ---------------------------------------------------
-- 1. Bảng NGUOI_DUNG (Khách hàng, Nhân viên, Quản trị)
-- ---------------------------------------------------
CREATE TABLE NGUOI_DUNG (
    id_nguoi_dung INT AUTO_INCREMENT PRIMARY KEY,
    ho_ten VARCHAR(100) NOT NULL,
    so_dien_thoai VARCHAR(15) NOT NULL UNIQUE,
    email VARCHAR(100) UNIQUE,
    mat_khau_hash VARCHAR(255) NOT NULL,
    vai_tro ENUM('KHACH_HANG', 'NHAN_VIEN_BAN_VE', 'ADMIN') NOT NULL DEFAULT 'KHACH_HANG',
    trang_thai ENUM('HOAT_DONG', 'BI_KHOA') DEFAULT 'HOAT_DONG',
    ngay_tao DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 2. Bảng TAI_XE (Tài xế & Phụ xe phụ trách hành trình)
-- ---------------------------------------------------
CREATE TABLE TAI_XE (
    id_tai_xe INT AUTO_INCREMENT PRIMARY KEY,
    ho_ten VARCHAR(100) NOT NULL,
    so_dien_thoai VARCHAR(15) NOT NULL UNIQUE,
    so_gplx VARCHAR(20) UNIQUE,
    loai_nhan_su ENUM('TAI_XE', 'PHU_XE') DEFAULT 'TAI_XE',
    trang_thai ENUM('SAN_SANG', 'DANG_CHAY', 'NGHI_PHEP') DEFAULT 'SAN_SANG'
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 3. Bảng XE (Thông tin xe khách & tình trạng bảo trì)
-- ---------------------------------------------------
CREATE TABLE XE (
    id_xe INT AUTO_INCREMENT PRIMARY KEY,
    bien_so_xe VARCHAR(20) NOT NULL UNIQUE,
    loai_xe ENUM('GHE_NGOI', 'GIUONG_NAM', 'LIMOUSINE') NOT NULL,
    tong_so_ghe INT NOT NULL,
    tinh_trang_bao_tri ENUM('TOT', 'DANG_BAO_TRI', 'HONG_HOC') DEFAULT 'TOT'
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 4. Bảng SO_DO_GHE (Cấu hình vị trí ghế vật lý trên xe)
-- ---------------------------------------------------
CREATE TABLE SO_DO_GHE (
    id_ghe INT AUTO_INCREMENT PRIMARY KEY,
    id_xe INT NOT NULL,
    ma_ghe VARCHAR(10) NOT NULL,
    tang TINYINT DEFAULT 1,
    trang_thai_hoat_dong BOOLEAN DEFAULT TRUE,
    CONSTRAINT fk_sodoghe_xe FOREIGN KEY (id_xe) REFERENCES XE(id_xe) ON DELETE CASCADE,
    CONSTRAINT uq_xe_maghe UNIQUE (id_xe, ma_ghe)
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 5. Bảng TUYEN_XE (Tuyến đường vận hành)
-- ---------------------------------------------------
CREATE TABLE TUYEN_XE (
    id_tuyen_xe INT AUTO_INCREMENT PRIMARY KEY,
    diem_di VARCHAR(100) NOT NULL,
    diem_den VARCHAR(100) NOT NULL,
    khoang_cach_km DECIMAL(6, 2) NOT NULL,
    thoi_gian_du_kien TIME NOT NULL
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 6. Bảng CHUYEN_XE (Lịch khởi hành cụ thể theo chuyến)
-- ---------------------------------------------------
CREATE TABLE CHUYEN_XE (
    id_chuyen_xe INT AUTO_INCREMENT PRIMARY KEY,
    id_tuyen_xe INT NOT NULL,
    id_xe INT NOT NULL,
    id_tai_xe INT NOT NULL,
    id_phu_xe INT NULL,
    thoi_gian_khoi_hanh DATETIME NOT NULL,
    gia_ve_co_ban DECIMAL(12, 2) NOT NULL,
    chi_phi_phat_sinh DECIMAL(12, 2) DEFAULT 0.00,
    trang_thai_chuyen ENUM('CHUA_KHOI_HANH', 'DANG_CHAY', 'HOAN_THANH', 'DA_HUY') DEFAULT 'CHUA_KHOI_HANH',
    CONSTRAINT fk_chuyenxe_tuyen FOREIGN KEY (id_tuyen_xe) REFERENCES TUYEN_XE(id_tuyen_xe),
    CONSTRAINT fk_chuyenxe_xe FOREIGN KEY (id_xe) REFERENCES XE(id_xe),
    CONSTRAINT fk_chuyenxe_taixe FOREIGN KEY (id_tai_xe) REFERENCES TAI_XE(id_tai_xe),
    CONSTRAINT fk_chuyenxe_phuxe FOREIGN KEY (id_phu_xe) REFERENCES TAI_XE(id_tai_xe)
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 7. Bảng VE (Thông tin vé xe, đặt chỗ & thanh toán)
-- ---------------------------------------------------
CREATE TABLE VE (
    id_ve INT AUTO_INCREMENT PRIMARY KEY,
    ma_ve VARCHAR(20) NOT NULL UNIQUE,
    id_chuyen_xe INT NOT NULL,
    id_nguoi_dung INT NULL,
    id_ghe INT NOT NULL,
    ten_hanh_khach VARCHAR(100) NOT NULL,
    sdt_hanh_khach VARCHAR(15) NOT NULL,
    diem_don_trung_chuyen VARCHAR(255) NULL,
    tong_tien DECIMAL(12, 2) NOT NULL,
    phuong_thuc_thanh_toan ENUM('TIEN_MAT', 'VI_DIEN_TU', 'THE_NGAN_HANG') NOT NULL,
    trang_thai_ve ENUM('GIU_CHO', 'DA_THANH_TOAN', 'DA_LEN_XE', 'DA_HUY', 'DA_DOI') NOT NULL,
    thoi_gian_het_han_giu_cho DATETIME NULL,
    ngay_dat DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_ve_chuyenxe FOREIGN KEY (id_chuyen_xe) REFERENCES CHUYEN_XE(id_chuyen_xe),
    CONSTRAINT fk_ve_nguoidung FOREIGN KEY (id_nguoi_dung) REFERENCES NGUOI_DUNG(id_nguoi_dung),
    CONSTRAINT fk_ve_sodoghe FOREIGN KEY (id_ghe) REFERENCES SO_DO_GHE(id_ghe)
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 8. Bảng YEU_CAU_HO_TRO (Khiếu nại / Helpdesk Ticket)
-- ---------------------------------------------------
CREATE TABLE YEU_CAU_HO_TRO (
    id_ticket INT AUTO_INCREMENT PRIMARY KEY,
    id_nguoi_dung INT NOT NULL,
    id_nhan_vien INT NULL,
    tieu_de VARCHAR(200) NOT NULL,
    noi_dung_van_de TEXT NOT NULL,
    trang_thai_xuly ENUM('CHO_XU_LY', 'DANG_XU_LY', 'DA_GIAI_QUYET', 'DA_DONG') DEFAULT 'CHO_XU_LY',
    ngay_tao DATETIME DEFAULT CURRENT_TIMESTAMP,
    ngay_cap_nhat DATETIME ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_ticket_khachhang FOREIGN KEY (id_nguoi_dung) REFERENCES NGUOI_DUNG(id_nguoi_dung),
    CONSTRAINT fk_ticket_nhanvien FOREIGN KEY (id_nhan_vien) REFERENCES NGUOI_DUNG(id_nguoi_dung)
) ENGINE=InnoDB;

-- ---------------------------------------------------
-- 9. Bảng THONG_BAO (Thông báo đẩy / Push notification)
-- ---------------------------------------------------
CREATE TABLE THONG_BAO (
    id_thong_bao INT AUTO_INCREMENT PRIMARY KEY,
    id_nguoi_dung INT NOT NULL,
    tieu_de VARCHAR(150) NOT NULL,
    noi_dung TEXT NOT NULL,
    loai_thong_bao ENUM('XAC_NHAN_VE', 'NHAC_GIO_CHAY', 'HUY_CHUYEN', 'KHUYEN_MAI') NOT NULL,
    da_doc BOOLEAN DEFAULT FALSE,
    thoi_gian DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_thongbao_nguoidung FOREIGN KEY (id_nguoi_dung) REFERENCES NGUOI_DUNG(id_nguoi_dung)
) ENGINE=InnoDB;

-- Tạo index hỗ trợ tối ưu truy vấn tra cứu chuyến xe và trạng thái vé
CREATE INDEX idx_chuyenxe_tuyen_gio ON CHUYEN_XE(id_tuyen_xe, thoi_gian_khoi_hanh);
CREATE INDEX idx_ve_chuyen_trangthai ON VE(id_chuyen_xe, trang_thai_ve);