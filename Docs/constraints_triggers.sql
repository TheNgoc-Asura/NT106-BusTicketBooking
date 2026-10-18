USE he_thong_dat_ve_xe;

-- ---------------------------------------------------
-- PHẦN 1: BỔ SUNG CÁC RÀNG BUỘC KIỂM TRA (CHECK CONSTRAINTS)
-- ---------------------------------------------------

-- Định dạng số điện thoại (10 số, bắt đầu bằng 0) và định dạng email
ALTER TABLE NGUOI_DUNG
ADD CONSTRAINT chk_nguoidung_sdt CHECK (so_dien_thoai REGEXP '^0[0-9]{9}$'),
ADD CONSTRAINT chk_nguoidung_email CHECK (email IS NULL OR email REGEXP '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$');

-- SĐT tài xế
ALTER TABLE TAI_XE
ADD CONSTRAINT chk_taixe_sdt CHECK (so_dien_thoai REGEXP '^0[0-9]{9}$');

-- Xe: số ghế phải > 0
ALTER TABLE XE
ADD CONSTRAINT chk_xe_tong_so_ghe CHECK (tong_so_ghe > 0);

-- Sơ đồ ghế: tầng 1 hoặc 2
ALTER TABLE SO_DO_GHE
ADD CONSTRAINT chk_sodoghe_tang CHECK (tang IN (1, 2));

-- Tuyến xe: khoảng cách > 0
ALTER TABLE TUYEN_XE
ADD CONSTRAINT chk_tuyenxe_khoangcach CHECK (khoang_cach_km > 0);

-- Chuyến xe: giá vé & chi phí >= 0; tài xế chính và phụ xe không được trùng nhau
ALTER TABLE CHUYEN_XE
ADD CONSTRAINT chk_chuyenxe_giave CHECK (gia_ve_co_ban >= 0),
ADD CONSTRAINT chk_chuyenxe_chiphi CHECK (chi_phi_phat_sinh >= 0),
ADD CONSTRAINT chk_chuyenxe_taixe_phuxe CHECK (id_phu_xe IS NULL OR id_tai_xe <> id_phu_xe);

-- Vé: tổng tiền >= 0 và SĐT người đi hợp lệ
ALTER TABLE VE
ADD CONSTRAINT chk_ve_tongtien CHECK (tong_tien >= 0),
ADD CONSTRAINT chk_ve_sdt CHECK (sdt_hanh_khach REGEXP '^0[0-9]{9}$');


-- ---------------------------------------------------
-- PHẦN 2: CÁC TRIGGER NGHIỆP VỤ BẮT BUỘC
-- ---------------------------------------------------

DELIMITER $$

-- Trigger 1: Ngăn chặn đặt trùng ghế trên cùng một chuyến xe
-- Một ghế chỉ được đặt nếu chưa có vé đang giữ chỗ hoặc đã thanh toán
DROP TRIGGER IF EXISTS trg_check_trung_ghe_truoc_khi_dat$$
CREATE TRIGGER trg_check_trung_ghe_truoc_khi_dat
BEFORE INSERT ON VE
FOR EACH ROW
BEGIN
    DECLARE v_count INT;
    
    SELECT COUNT(*) INTO v_count
    FROM VE
    WHERE id_chuyen_xe = NEW.id_chuyen_xe
      AND id_ghe = NEW.id_ghe
      AND trang_thai_ve IN ('GIU_CHO', 'DA_THANH_TOAN', 'DA_LEN_XE')
      AND (thoi_gian_het_han_giu_cho IS NULL OR thoi_gian_het_han_giu_cho > NOW());

    IF v_count > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Lỗi: Vị trí ghế này đã có người giữ chỗ hoặc thanh toán trên chuyến xe này!';
    END IF;
END$$

-- Trigger 2: Đảm bảo ghế được đặt phải thuộc đúng chiếc xe vận hành chuyến đó
DROP TRIGGER IF EXISTS trg_check_ghe_thuoc_chuyen_xe$$
CREATE TRIGGER trg_check_ghe_thuoc_chuyen_xe
BEFORE INSERT ON VE
FOR EACH ROW
BEGIN
    DECLARE v_xe_cua_chuyen INT;
    DECLARE v_xe_cua_ghe INT;

    SELECT id_xe INTO v_xe_cua_chuyen
    FROM CHUYEN_XE
    WHERE id_chuyen_xe = NEW.id_chuyen_xe;

    SELECT id_xe INTO v_xe_cua_ghe
    FROM SO_DO_GHE
    WHERE id_ghe = NEW.id_ghe;

    IF v_xe_cua_chuyen <> v_xe_cua_ghe THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Lỗi: Ghế được chọn không thuộc sơ đồ của chiếc xe chạy chuyến này!';
    END IF;
END$$

-- Trigger 3: Không cho phép xếp xe đang bảo trì/hỏng hóc vào chuyến mới
DROP TRIGGER IF EXISTS trg_check_xe_bao_tri_khi_tao_chuyen$$
CREATE TRIGGER trg_check_xe_bao_tri_khi_tao_chuyen
BEFORE INSERT ON CHUYEN_XE
FOR EACH ROW
BEGIN
    DECLARE v_tinh_trang VARCHAR(20);

    SELECT tinh_trang_bao_tri INTO v_tinh_trang
    FROM XE
    WHERE id_xe = NEW.id_xe;

    IF v_tinh_trang IN ('DANG_BAO_TRI', 'HONG_HOC') THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Lỗi: Xe đang bảo trì hoặc hỏng hóc, không thể điều phối xếp chuyến!';
    END IF;
END$$

DELIMITER ;