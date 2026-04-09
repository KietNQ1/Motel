/* =========================================================
   MotelDb - Custom SQL Objects
   - Stored Procedures
   - Views
   - Triggers

   Run this script AFTER the database schema is created via Entity Framework (Code First initialization / Update-Database).
   ========================================================= */

USE MotelDb;
GO

SET NOCOUNT ON;

------------------------------------------------------------
-- 1) SP: sp_SoftDeleteProperty
------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_SoftDeleteProperty', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SoftDeleteProperty;
GO

CREATE PROCEDURE dbo.sp_SoftDeleteProperty
    @PropertyId INT,
    @DeletedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Properties WHERE PropertyId = @PropertyId AND IsDeleted = 0)
    BEGIN
        RAISERROR(N'Property not found or already deleted.', 16, 1);
        RETURN;
    END

    -- Soft delete rooms first
    UPDATE dbo.Rooms
    SET IsDeleted = 1
    WHERE PropertyId = @PropertyId AND IsDeleted = 0;

    -- Soft delete property
    UPDATE dbo.Properties
    SET IsDeleted = 1
    WHERE PropertyId = @PropertyId AND IsDeleted = 0;
END
GO

------------------------------------------------------------
-- 2) TRIGGER: trg_Rooms_BlockSoftDeleteWhenActiveContract
------------------------------------------------------------
IF OBJECT_ID(N'dbo.trg_Rooms_BlockSoftDeleteWhenActiveContract', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Rooms_BlockSoftDeleteWhenActiveContract;
GO

CREATE TRIGGER dbo.trg_Rooms_BlockSoftDeleteWhenActiveContract
ON dbo.Rooms
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Detect attempts to soft delete: IsDeleted changes 0 -> 1
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.RoomId = i.RoomId
        WHERE d.IsDeleted = 0 AND i.IsDeleted = 1
    )
    BEGIN
        -- If any of those rooms has an active (non-deleted) contract, block
        IF EXISTS
        (
            SELECT 1
            FROM inserted i
            JOIN deleted d ON d.RoomId = i.RoomId
            JOIN dbo.Contracts c ON c.RoomId = i.RoomId
            WHERE d.IsDeleted = 0 AND i.IsDeleted = 1
              AND c.IsDeleted = 0
              AND c.Status = 'active'
        )
        BEGIN
            RAISERROR(N'Cannot soft delete room because an active contract exists.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
END
GO

------------------------------------------------------------
-- 3) SP: sp_SaveMeterReading
------------------------------------------------------------
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
END
GO

------------------------------------------------------------
-- 4) VIEW: vw_TransactionHistory
------------------------------------------------------------
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
        + RIGHT('0' + CAST(i.PeriodMonth % 100 AS VARCHAR(2)), 2)   AS PeriodLabel,
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

    -- Landlord
    l.LandlordId,
    l.DisplayName                                                    AS LandlordName,

    -- Payment status
    pay.PaidAmount,
    pay.PaidAt

FROM dbo.Invoices           i
INNER JOIN dbo.Contracts    c  ON  c.ContractId = i.ContractId
INNER JOIN dbo.Tenants      t  ON  t.TenantId   = c.TenantId
INNER JOIN dbo.Rooms        r  ON  r.RoomId     = i.RoomId
INNER JOIN dbo.Properties   p  ON  p.PropertyId = r.PropertyId
INNER JOIN dbo.Landlords    l  ON  l.LandlordId = p.LandlordId
LEFT JOIN (
    SELECT
        py.InvoiceId,
        SUM(py.Amount)  AS PaidAmount,
        MAX(py.PaidAt)  AS PaidAt
    FROM dbo.Payments py
    WHERE py.Status = 'success'
    GROUP BY py.InvoiceId
)                           pay ON pay.InvoiceId = i.InvoiceId

WHERE i.Status != 'cancelled'
  AND c.IsDeleted = 0
  AND r.IsDeleted = 0
  AND p.IsDeleted = 0;
GO

------------------------------------------------------------
-- 5) CONSTRAINTS & INDEXES: FeeSettings
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FeeSettings_PropertyOrRoom')
BEGIN
    ALTER TABLE dbo.FeeSettings
    ADD CONSTRAINT CK_FeeSettings_PropertyOrRoom CHECK (
        (PropertyId IS NOT NULL AND RoomId IS NULL)
        OR
        (PropertyId IS NULL AND RoomId IS NOT NULL)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FeeSettings_Property' AND object_id = OBJECT_ID('dbo.FeeSettings'))
BEGIN
    CREATE INDEX IX_FeeSettings_Property ON dbo.FeeSettings(PropertyId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FeeSettings_Room' AND object_id = OBJECT_ID('dbo.FeeSettings'))
BEGIN
    CREATE INDEX IX_FeeSettings_Room ON dbo.FeeSettings(RoomId);
END
GO
