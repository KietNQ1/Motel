/* seed_demo_enhanced.sql - Minimal demo data */
USE MotelDb;
GO

DECLARE @LandlordId INT = (SELECT LandlordId FROM dbo.Landlords WHERE UserId = (SELECT Id FROM dbo.AspNetUsers WHERE Email = N'landlord1@motel.local') AND IsDeleted = 0);
DECLARE @Today DATE = GETDATE();

-- 2 Properties
INSERT INTO dbo.Properties (LandlordId, Name, Address, IsDeleted)
SELECT @LandlordId, v.Name, v.Address, 0 FROM (VALUES (N'Nhà trọ A', N'123 Đường A'), (N'Nhà trọ B', N'456 Đường B')) v(Name, Address)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Properties WHERE LandlordId = @LandlordId AND Name = v.Name);

-- 6 Rooms (3 occupied, 2 available, 1 maintenance)
INSERT INTO dbo.Rooms (PropertyId, RoomName, RentPrice, Status, MaxOccupants, IsDeleted)
SELECT p.PropertyId, v.RoomName, 2500000, v.Status, 2, 0
FROM (VALUES (N'Nhà trọ A', N'101', N'occupied'), (N'Nhà trọ A', N'102', N'occupied'), (N'Nhà trọ A', N'103', N'available'),
             (N'Nhà trọ B', N'201', N'occupied'), (N'Nhà trọ B', N'202', N'available'), (N'Nhà trọ B', N'203', N'maintenance')) v(PropName, RoomName, Status)
INNER JOIN dbo.Properties p ON p.Name = v.PropName AND p.LandlordId = @LandlordId
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE PropertyId = p.PropertyId AND RoomName = v.RoomName);

-- 3 Tenants
INSERT INTO dbo.Tenants (LandlordId, FullName, Phone, IdentityNo, IsDeleted)
SELECT @LandlordId, v.Name, v.Phone, v.IdNo, 0 FROM (VALUES (N'Nguyễn Văn A', N'0901111111', N'A1'), (N'Trần Thị B', N'0902222222', N'B2'), (N'Lê Văn C', N'0903333333', N'C3')) v(Name, Phone, IdNo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE LandlordId = @LandlordId AND IdentityNo = v.IdNo);

-- 3 Contracts (expiry: 10 days, 20 days, 30 days)
;WITH NumberedRooms AS (
    SELECT r.RoomId, ROW_NUMBER() OVER (ORDER BY r.RoomId) AS RoomNum
    FROM dbo.Rooms r INNER JOIN dbo.Properties p ON r.PropertyId = p.PropertyId
    WHERE p.LandlordId = @LandlordId AND r.Status = 'occupied' AND NOT EXISTS (SELECT 1 FROM dbo.Contracts WHERE RoomId = r.RoomId AND Status = 'active')
),
NumberedTenants AS (
    SELECT TenantId, ROW_NUMBER() OVER (ORDER BY TenantId) AS TenantNum
    FROM dbo.Tenants WHERE LandlordId = @LandlordId AND IsDeleted = 0
)
INSERT INTO dbo.Contracts (RoomId, TenantId, DepositAmount, StartDate, EndDate, Status, IsDeleted)
SELECT nr.RoomId, nt.TenantId, 2500000, DATEADD(MONTH, -6, @Today), DATEADD(DAY, nr.RoomNum * 10, @Today), 'active', 0
FROM NumberedRooms nr
INNER JOIN NumberedTenants nt ON nt.TenantNum = ((nr.RoomNum - 1) % 3) + 1;

-- Invoices (last 12 months)
DECLARE @M INT = -11;
WHILE @M <= 0
BEGIN
    INSERT INTO dbo.Invoices (ContractId, RoomId, PeriodMonth, TotalAmount, Status, DueDate, CreatedAt)
    SELECT c.ContractId, c.RoomId, YEAR(DATEADD(MONTH, @M, @Today)) * 100 + MONTH(DATEADD(MONTH, @M, @Today)), 2800000, 
           CASE WHEN @M = 0 AND c.ContractId % 2 = 0 THEN 'unpaid' ELSE 'paid' END, DATEADD(DAY, 7, DATEADD(MONTH, @M, @Today)), DATEADD(MONTH, @M, @Today)
    FROM dbo.Contracts c INNER JOIN dbo.Rooms r ON c.RoomId = r.RoomId INNER JOIN dbo.Properties p ON r.PropertyId = p.PropertyId
    WHERE p.LandlordId = @LandlordId AND c.Status = 'active' AND NOT EXISTS (SELECT 1 FROM dbo.Invoices WHERE RoomId = c.RoomId AND PeriodMonth = YEAR(DATEADD(MONTH, @M, @Today)) * 100 + MONTH(DATEADD(MONTH, @M, @Today)));
    SET @M = @M + 1;
END

PRINT 'Done!';
GO
