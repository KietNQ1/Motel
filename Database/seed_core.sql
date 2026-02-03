/* =========================================================
   seed_core.sql (for MotelDb)
   - Data First: seed only (NO schema changes)
   - Creates core accounts:
     1) admin@motel.local   / 123456
     2) landlord1@motel.local / 123456  + Landlords profile
   ========================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE MotelDb;
GO

BEGIN TRAN;

------------------------------------------------------------
-- 1) Seed Users (AspNetUsers)
------------------------------------------------------------

DECLARE @AdminEmail NVARCHAR(256) = N'admin@motel.local';
DECLARE @LandlordEmail NVARCHAR(256) = N'landlord1@motel.local';

DECLARE @DefaultPassword NVARCHAR(100) = N'123456';

-- Simple DEV hash (SHA2_256) -> hex string
DECLARE @PasswordHash NVARCHAR(64) =
    CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', @DefaultPassword), 2);

-- 1.1 Insert Admin user
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Email = @AdminEmail)
BEGIN
    INSERT INTO dbo.AspNetUsers (Email, PasswordHash, FullName, Phone)
    VALUES (@AdminEmail, @PasswordHash, N'System Admin', N'0900000000');
END

-- 1.2 Insert Landlord user
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Email = @LandlordEmail)
BEGIN
    INSERT INTO dbo.AspNetUsers (Email, PasswordHash, FullName, Phone)
    VALUES (@LandlordEmail, @PasswordHash, N'Landlord 1', N'0911111111');
END

------------------------------------------------------------
-- 2) Seed Landlord profile (Landlords)
------------------------------------------------------------

DECLARE @LandlordUserId INT =
(
    SELECT TOP (1) Id
    FROM dbo.AspNetUsers
    WHERE Email = @LandlordEmail
);

-- A user can have at most 1 active landlord profile (filtered unique index)
IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Landlords
    WHERE UserId = @LandlordUserId AND IsDeleted = 0
)
BEGIN
    INSERT INTO dbo.Landlords (UserId, DisplayName, Address, IsDeleted)
    VALUES (@LandlordUserId, N'Chủ trọ A', N'TP. Hồ Chí Minh', 0);
END

COMMIT TRAN;

-- Output for quick check
SELECT Id, Email, FullName, Phone, CreatedAt
FROM dbo.AspNetUsers
WHERE Email IN (N'admin@motel.local', N'landlord1@motel.local');

SELECT LandlordId, UserId, DisplayName, Address, IsDeleted, CreatedAt
FROM dbo.Landlords
WHERE UserId = (SELECT Id FROM dbo.AspNetUsers WHERE Email = N'landlord1@motel.local');
