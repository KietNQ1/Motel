/* =========================================================
   MotelDb - Update: Meter Readings Auto-Save & Transaction History View
   Ngày tạo: 2026-03-09
   Mục đích:
     1. Stored Procedure sp_SaveMeterReading
        → Gọi khi tạo Invoice để lưu chỉ số điện/nước vào MeterReadings
        → Nếu đã có bản ghi thì UPDATE, chưa có thì INSERT (UPSERT)
     2. View vw_TransactionHistory
        → Lịch sử giao dịch: tên người thuê, phòng, số tiền, tháng, trạng thái
   ========================================================= */

USE MotelDb;
GO

-- ============================================================
-- 1) Stored Procedure: sp_SaveMeterReading
--    Dùng MERGE (UPSERT) để insert hoặc update chỉ số điện/nước
--    Parameters:
--      @RoomId           INT
--      @PeriodMonth      INT     -- YYYYMM
--      @ElectricOld      INT
--      @ElectricNew      INT
--      @WaterOld         INT
--      @WaterNew         INT
--      @RecordedByUserId INT     -- UserId của người ghi (landlord)
-- ============================================================
IF OBJECT_ID(N'dbo.sp_SaveMeterReading', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SaveMeterReading;
GO

CREATE PROCEDURE dbo.sp_SaveMeterReading
    @RoomId           INT,
    @PeriodMonth      INT,
    @ElectricOld      INT,
    @ElectricNew      INT,
    @WaterOld         INT,
    @WaterNew         INT,
    @RecordedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate
    IF @ElectricNew < @ElectricOld
        THROW 50001, N'Chỉ số điện mới không thể nhỏ hơn chỉ số cũ.', 1;

    IF @WaterNew < @WaterOld
        THROW 50002, N'Chỉ số nước mới không thể nhỏ hơn chỉ số cũ.', 1;

    IF @PeriodMonth NOT BETWEEN 190001 AND 999912
        THROW 50003, N'PeriodMonth không hợp lệ (định dạng YYYYMM).', 1;

    -- MERGE: nếu đã có bản ghi thì UPDATE, chưa có thì INSERT
    MERGE dbo.MeterReadings AS target
    USING (
        SELECT 
            @RoomId           AS RoomId,
            @PeriodMonth      AS PeriodMonth,
            @ElectricOld      AS ElectricOld,
            @ElectricNew      AS ElectricNew,
            @WaterOld         AS WaterOld,
            @WaterNew         AS WaterNew,
            @RecordedByUserId AS RecordedByUserId
    ) AS source
    ON  target.RoomId      = source.RoomId
    AND target.PeriodMonth = source.PeriodMonth

    WHEN MATCHED THEN
        UPDATE SET
            ElectricOld      = source.ElectricOld,
            ElectricNew      = source.ElectricNew,
            WaterOld         = source.WaterOld,
            WaterNew         = source.WaterNew,
            RecordedByUserId = source.RecordedByUserId,
            RecordedAt       = SYSDATETIME()

    WHEN NOT MATCHED THEN
        INSERT (RoomId, PeriodMonth, ElectricOld, ElectricNew, WaterOld, WaterNew, RecordedByUserId, RecordedAt)
        VALUES (source.RoomId, source.PeriodMonth, source.ElectricOld, source.ElectricNew, 
                source.WaterOld, source.WaterNew, source.RecordedByUserId, SYSDATETIME());
END;
GO

-- ============================================================
-- 2) View: vw_TransactionHistory
--    Lịch sử giao dịch cho landlord:
--    Tên người thuê, Tên phòng, Nhà trọ, Tháng, Tổng tiền, Trạng thái hóa đơn
--    (dựa trên bảng Invoices + InvoiceLines + Contracts + Tenants + Rooms + Properties)
-- ============================================================
IF OBJECT_ID(N'dbo.vw_TransactionHistory', N'V') IS NOT NULL
    DROP VIEW dbo.vw_TransactionHistory;
GO

CREATE VIEW dbo.vw_TransactionHistory
AS
SELECT
    -- Invoice info
    i.InvoiceId,
    i.PeriodMonth,
    CAST(i.PeriodMonth / 100 AS VARCHAR(4)) + N'/' 
        + RIGHT('0' + CAST(i.PeriodMonth % 100 AS VARCHAR(2)), 2)   AS PeriodLabel,  -- VD: "2026/03"
    i.TotalAmount,
    i.Status                                                         AS InvoiceStatus,
    i.DueDate,
    i.CreatedAt                                                      AS InvoiceCreatedAt,

    -- Room & Property
    r.RoomId,
    r.RoomName,
    r.RentPrice,
    p.PropertyId,
    p.Name                                                           AS PropertyName,

    -- Tenant
    t.TenantId,
    t.FullName                                                       AS TenantName,
    t.Phone                                                          AS TenantPhone,
    t.Email                                                          AS TenantEmail,

    -- Contract
    c.ContractId,
    c.StartDate                                                      AS ContractStart,
    c.EndDate                                                        AS ContractEnd,

    -- Landlord (để filter theo landlord)
    l.LandlordId,
    l.DisplayName                                                    AS LandlordName,

    -- Payment status (nếu đã có payment)
    pay.PaidAmount,
    pay.PaidAt

FROM dbo.Invoices           i
INNER JOIN dbo.Contracts    c  ON  c.ContractId = i.ContractId
INNER JOIN dbo.Tenants      t  ON  t.TenantId   = c.TenantId
INNER JOIN dbo.Rooms        r  ON  r.RoomId     = i.RoomId
INNER JOIN dbo.Properties   p  ON  p.PropertyId = r.PropertyId
INNER JOIN dbo.Landlords    l  ON  l.LandlordId = p.LandlordId
LEFT JOIN (
    -- Lấy payment đã thành công gần nhất cho invoice
    SELECT
        py.InvoiceId,
        SUM(py.Amount)  AS PaidAmount,
        MAX(py.PaidAt)  AS PaidAt
    FROM dbo.Payments py
    WHERE py.Status = 'success'
    GROUP BY py.InvoiceId
)                           pay ON pay.InvoiceId = i.InvoiceId

WHERE i.Status != 'cancelled'   -- bỏ hóa đơn đã hủy
  AND c.IsDeleted = 0
  AND r.IsDeleted = 0
  AND p.IsDeleted = 0;
GO

-- ============================================================
-- 3) Kiểm tra xem bảng Payments có cột Status, InvoiceId, Amount, PaidAt chưa
--    (Dựa trên model Payment.cs trong dự án)
--    Nếu chưa có cột PaidAt thì sẽ cần ALTER TABLE
-- ============================================================

-- Thêm cột PaidAt vào bảng Payments nếu chưa có
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.Payments') 
      AND name = N'PaidAt'
)
BEGIN
    ALTER TABLE dbo.Payments
    ADD PaidAt DATETIME2 NULL;
    PRINT N'Đã thêm cột PaidAt vào bảng Payments';
END
ELSE
BEGIN
    PRINT N'Cột PaidAt đã tồn tại trong bảng Payments';
END
GO

-- ============================================================
-- 4) KIỂM TRA KẾT QUẢ
--    Chạy các lệnh dưới để xác nhận đã tạo thành công
-- ============================================================

-- Kiểm tra SP đã tồn tại
SELECT name, type_desc 
FROM sys.objects 
WHERE name IN ('sp_SaveMeterReading')
  AND type = 'P';

-- Kiểm tra View đã tồn tại
SELECT name, type_desc 
FROM sys.objects 
WHERE name = 'vw_TransactionHistory'
  AND type = 'V';

PRINT N'';
PRINT N'✅ Migration hoàn thành!';
PRINT N'  - Stored Procedure: sp_SaveMeterReading';
PRINT N'  - View: vw_TransactionHistory';
PRINT N'';
PRINT N'📌 Hướng dẫn sử dụng sp_SaveMeterReading:';
PRINT N'   EXEC sp_SaveMeterReading';
PRINT N'       @RoomId = 1,';
PRINT N'       @PeriodMonth = 202603,';
PRINT N'       @ElectricOld = 120,';
PRINT N'       @ElectricNew = 150,';
PRINT N'       @WaterOld = 80,';
PRINT N'       @WaterNew = 95,';
PRINT N'       @RecordedByUserId = 1;';
PRINT N'';
PRINT N'📌 Hướng dẫn xem lịch sử giao dịch:';
PRINT N'   SELECT * FROM vw_TransactionHistory WHERE LandlordId = 1 ORDER BY InvoiceCreatedAt DESC;';
GO
