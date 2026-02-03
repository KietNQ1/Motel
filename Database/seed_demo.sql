/* =========================================================
   seed_demo.sql (for MotelDb)
   - Data First: seed only (NO schema changes)
   - Demo data:
     Property -> Rooms -> Tenant -> Contract(active) -> Occupancy(primary)
     UtilitySetting -> MeterReading(YYYYMM) -> Invoice + Lines
   ========================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE MotelDb;
GO

BEGIN TRAN;

------------------------------------------------------------
-- 0) Resolve LandlordId (created by seed_core.sql)
------------------------------------------------------------
DECLARE @LandlordEmail NVARCHAR(256) = N'landlord1@motel.local';

DECLARE @LandlordUserId INT =
(
    SELECT TOP (1) Id
    FROM dbo.AspNetUsers
    WHERE Email = @LandlordEmail
);

IF @LandlordUserId IS NULL
BEGIN
    RAISERROR(N'Landlord user not found. Please run seed_core.sql first.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

DECLARE @LandlordId INT =
(
    SELECT TOP (1) LandlordId
    FROM dbo.Landlords
    WHERE UserId = @LandlordUserId AND IsDeleted = 0
);

IF @LandlordId IS NULL
BEGIN
    RAISERROR(N'Landlord profile not found. Please run seed_core.sql first.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

------------------------------------------------------------
-- 1) Property
------------------------------------------------------------
DECLARE @PropertyName NVARCHAR(200) = N'Nhà trọ A';
DECLARE @PropertyAddress NVARCHAR(300) = N'123 Nguyễn Văn A, Quận 1, TP.HCM';

DECLARE @PropertyId INT =
(
    SELECT TOP (1) PropertyId
    FROM dbo.Properties
    WHERE LandlordId = @LandlordId
      AND Name = @PropertyName
      AND IsDeleted = 0
);

IF @PropertyId IS NULL
BEGIN
    INSERT INTO dbo.Properties (LandlordId, Name, Address, Description, IsDeleted)
    VALUES (@LandlordId, @PropertyName, @PropertyAddress, N'Demo property (seed_demo.sql)', 0);

    SET @PropertyId = SCOPE_IDENTITY();
END

------------------------------------------------------------
-- 2) Rooms (unique: PropertyId + RoomName where IsDeleted=0)
------------------------------------------------------------
DECLARE @RoomA101Id INT =
(
    SELECT TOP (1) RoomId
    FROM dbo.Rooms
    WHERE PropertyId = @PropertyId AND RoomName = N'A101' AND IsDeleted = 0
);

IF @RoomA101Id IS NULL
BEGIN
    INSERT INTO dbo.Rooms (PropertyId, RoomName, RentPrice, Status, MaxOccupants, IsDeleted)
    VALUES (@PropertyId, N'A101', 2500000, N'occupied', 2, 0);

    SET @RoomA101Id = SCOPE_IDENTITY();
END
ELSE
BEGIN
    -- Ensure status matches demo expectation
    UPDATE dbo.Rooms
    SET Status = N'occupied'
    WHERE RoomId = @RoomA101Id;
END

DECLARE @RoomA102Id INT =
(
    SELECT TOP (1) RoomId
    FROM dbo.Rooms
    WHERE PropertyId = @PropertyId AND RoomName = N'A102' AND IsDeleted = 0
);

IF @RoomA102Id IS NULL
BEGIN
    INSERT INTO dbo.Rooms (PropertyId, RoomName, RentPrice, Status, MaxOccupants, IsDeleted)
    VALUES (@PropertyId, N'A102', 2800000, N'available', 3, 0);

    SET @RoomA102Id = SCOPE_IDENTITY();
END

------------------------------------------------------------
-- 3) Tenant (unique filtered: LandlordId + IdentityNo when IdentityNo IS NOT NULL)
------------------------------------------------------------
DECLARE @TenantIdentity NVARCHAR(50) = N'0123456789';

DECLARE @TenantId INT =
(
    SELECT TOP (1) TenantId
    FROM dbo.Tenants
    WHERE LandlordId = @LandlordId
      AND IdentityNo = @TenantIdentity
      AND IsDeleted = 0
);

IF @TenantId IS NULL
BEGIN
    INSERT INTO dbo.Tenants (LandlordId, FullName, Phone, Email, IdentityNo, IsDeleted)
    VALUES (@LandlordId, N'Nguyễn Văn Thuê', N'0922222222', N'tenant1@motel.local', @TenantIdentity, 0);

    SET @TenantId = SCOPE_IDENTITY();
END

------------------------------------------------------------
-- 4) Contract (unique filtered: RoomId where Status='active' AND IsDeleted=0)
------------------------------------------------------------
-- Use Date type consistent with your schema (db.sql uses DATE / not datetime for Start/End)
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @StartDate DATE = @Today;
DECLARE @EndDate DATE = DATEADD(MONTH, 6, @StartDate);

DECLARE @ContractId INT =
(
    SELECT TOP (1) ContractId
    FROM dbo.Contracts
    WHERE RoomId = @RoomA101Id
      AND Status = N'active'
      AND IsDeleted = 0
);

IF @ContractId IS NULL
BEGIN
    INSERT INTO dbo.Contracts
    (
        RoomId, TenantId, DepositAmount, StartDate, EndDate, Status, IsDeleted
    )
    VALUES
    (
        @RoomA101Id, @TenantId, 1000000, @StartDate, @EndDate, N'active', 0
    );

    SET @ContractId = SCOPE_IDENTITY();
END

------------------------------------------------------------
-- 5) RoomOccupancy (unique filtered: RoomId where IsPrimary=1 AND Status='active')
------------------------------------------------------------
DECLARE @OccId INT =
(
    SELECT TOP (1) RoomOccupancyId
    FROM dbo.RoomOccupancies
    WHERE RoomId = @RoomA101Id
      AND IsPrimary = 1
      AND Status = N'active'
);

IF @OccId IS NULL
BEGIN
    INSERT INTO dbo.RoomOccupancies
    (
        RoomId, TenantId, MoveInDate, MoveOutDate, IsPrimary, Status
    )
    VALUES
    (
        @RoomA101Id, @TenantId, @StartDate, NULL, 1, N'active'
    );
END

------------------------------------------------------------
-- 6) RoomUtilitySettings (EffectiveFrom/EffectiveTo)
------------------------------------------------------------
-- Keep one active setting (EffectiveTo IS NULL) per room for demo
IF NOT EXISTS
(
    SELECT 1
    FROM dbo.RoomUtilitySettings
    WHERE RoomId = @RoomA101Id
      AND EffectiveTo IS NULL
)
BEGIN
    INSERT INTO dbo.RoomUtilitySettings
    (
        RoomId, ElectricUnitPrice, WaterUnitPrice, InternetFee, TrashFee, EffectiveFrom, EffectiveTo
    )
    VALUES
    (
        @RoomA101Id, 3500, 15000, 100000, 30000, @Today, NULL
    );
END

------------------------------------------------------------
-- 7) MeterReadings (unique: RoomId + PeriodMonth)
------------------------------------------------------------
DECLARE @PeriodMonth INT = (YEAR(@Today) * 100) + MONTH(@Today);

-- Pull current utility prices for computation later
DECLARE @ElectricUnit DECIMAL(18,2) =
(
    SELECT TOP (1) ElectricUnitPrice
    FROM dbo.RoomUtilitySettings
    WHERE RoomId = @RoomA101Id AND EffectiveTo IS NULL
    ORDER BY EffectiveFrom DESC
);

DECLARE @WaterUnit DECIMAL(18,2) =
(
    SELECT TOP (1) WaterUnitPrice
    FROM dbo.RoomUtilitySettings
    WHERE RoomId = @RoomA101Id AND EffectiveTo IS NULL
    ORDER BY EffectiveFrom DESC
);

DECLARE @InternetFee DECIMAL(18,2) =
(
    SELECT TOP (1) InternetFee
    FROM dbo.RoomUtilitySettings
    WHERE RoomId = @RoomA101Id AND EffectiveTo IS NULL
    ORDER BY EffectiveFrom DESC
);

DECLARE @TrashFee DECIMAL(18,2) =
(
    SELECT TOP (1) TrashFee
    FROM dbo.RoomUtilitySettings
    WHERE RoomId = @RoomA101Id AND EffectiveTo IS NULL
    ORDER BY EffectiveFrom DESC
);

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.MeterReadings
    WHERE RoomId = @RoomA101Id AND PeriodMonth = @PeriodMonth
)
BEGIN
    INSERT INTO dbo.MeterReadings
    (
        RoomId, PeriodMonth,
        ElectricOld, ElectricNew,
        WaterOld, WaterNew,
        RecordedByUserId
    )
    VALUES
    (
        @RoomA101Id, @PeriodMonth,
        1000, 1050,
        200, 205,
        @LandlordUserId
    );
END

-- Get meter values (either newly inserted or existing)
DECLARE @ElectricOld INT, @ElectricNew INT, @WaterOld INT, @WaterNew INT;

SELECT TOP (1)
    @ElectricOld = ElectricOld,
    @ElectricNew = ElectricNew,
    @WaterOld = WaterOld,
    @WaterNew = WaterNew
FROM dbo.MeterReadings
WHERE RoomId = @RoomA101Id AND PeriodMonth = @PeriodMonth;

------------------------------------------------------------
-- 8) Invoice + InvoiceLines (unique: RoomId + PeriodMonth)
------------------------------------------------------------
DECLARE @InvoiceId INT =
(
    SELECT TOP (1) InvoiceId
    FROM dbo.Invoices
    WHERE RoomId = @RoomA101Id AND PeriodMonth = @PeriodMonth
);

-- Rent price
DECLARE @RentPrice DECIMAL(18,2) =
(
    SELECT TOP (1) RentPrice
    FROM dbo.Rooms
    WHERE RoomId = @RoomA101Id
);

-- Costs
DECLARE @ElectricQty INT = (@ElectricNew - @ElectricOld);
DECLARE @WaterQty INT = (@WaterNew - @WaterOld);

DECLARE @ElectricCost DECIMAL(18,2) = CAST(@ElectricQty AS DECIMAL(18,2)) * @ElectricUnit;
DECLARE @WaterCost DECIMAL(18,2)   = CAST(@WaterQty AS DECIMAL(18,2)) * @WaterUnit;

DECLARE @Total DECIMAL(18,2) = @RentPrice + @ElectricCost + @WaterCost + @InternetFee + @TrashFee;

IF @InvoiceId IS NULL
BEGIN
    INSERT INTO dbo.Invoices
    (
        ContractId, RoomId, PeriodMonth, TotalAmount, Status, DueDate
    )
    VALUES
    (
        @ContractId, @RoomA101Id, @PeriodMonth, @Total, N'unpaid', DATEADD(DAY, 7, @Today)
    );

    SET @InvoiceId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    -- keep total synced (in case meter/price changed)
    UPDATE dbo.Invoices
    SET TotalAmount = @Total
    WHERE InvoiceId = @InvoiceId;
END

-- Insert InvoiceLines if none exist for this invoice
IF NOT EXISTS (SELECT 1 FROM dbo.InvoiceLines WHERE InvoiceId = @InvoiceId)
BEGIN
    INSERT INTO dbo.InvoiceLines (InvoiceId, ItemType, Description, Quantity, UnitPrice)
    VALUES
    (@InvoiceId, N'rent',     N'Tiền phòng', 1, @RentPrice),
    (@InvoiceId, N'electric', N'Tiền điện',  @ElectricQty, @ElectricUnit),
    (@InvoiceId, N'water',    N'Tiền nước',  @WaterQty,   @WaterUnit),
    (@InvoiceId, N'internet', N'Internet',   1, @InternetFee),
    (@InvoiceId, N'trash',    N'Rác',        1, @TrashFee);
END

COMMIT TRAN;

------------------------------------------------------------
-- Output quick demo summary
------------------------------------------------------------
SELECT p.PropertyId, p.Name AS PropertyName, p.Address
FROM dbo.Properties p
WHERE p.PropertyId = @PropertyId;

SELECT r.RoomId, r.RoomName, r.Status, r.RentPrice
FROM dbo.Rooms r
WHERE r.PropertyId = @PropertyId AND r.IsDeleted = 0
ORDER BY r.RoomName;

SELECT t.TenantId, t.FullName, t.Phone, t.IdentityNo
FROM dbo.Tenants t
WHERE t.TenantId = @TenantId;

SELECT c.ContractId, c.RoomId, c.TenantId, c.Status, c.StartDate, c.EndDate
FROM dbo.Contracts c
WHERE c.ContractId = @ContractId;

SELECT i.InvoiceId, i.RoomId, i.PeriodMonth, i.TotalAmount, i.Status, i.DueDate
FROM dbo.Invoices i
WHERE i.InvoiceId = @InvoiceId;

SELECT il.InvoiceLineId, il.ItemType, il.Description, il.Quantity, il.UnitPrice, il.LineTotal
FROM dbo.InvoiceLines il
WHERE il.InvoiceId = @InvoiceId
ORDER BY il.InvoiceLineId;
