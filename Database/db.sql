/* =========================================================
   MotelDb - Full Schema (SSMS 2022)
   Naming: PascalCase table names (only first letters capitalized)
   Includes:
     A) Stored procedure: sp_SoftDeleteProperty (soft delete Property + Rooms)
     B) Trigger (Option 1 - safe): block Room soft delete if active Contract exists
   Update:
     - Files            -> StoredFile
     - FileReferences   -> StoredFileReference
   ========================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;

------------------------------------------------------------
-- 0) Create DB
------------------------------------------------------------
IF DB_ID(N'MotelDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE MotelDb;');
END
GO

USE MotelDb;
GO

------------------------------------------------------------
-- 1) Tables
------------------------------------------------------------

/* NOTE:
   This schema follows your ERD (v3):
   - AspNetUsers
   - Landlords, Properties, Rooms, Tenants
   - Contracts, RoomOccupancies
   - RoomUtilitySettings, MeterReadings
   - Invoices, InvoiceLines
   - PaymentIntents, Payments
   - Transactions
   - StoredFile, StoredFileReference
   - Notifications
   - Subscriptions
*/

-- 1.1 AspNetUsers (simplified user profile table per ERD)
IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL DROP TABLE dbo.AspNetUsers;
GO
CREATE TABLE dbo.AspNetUsers
(
    Id              INT             IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
    Email           NVARCHAR(256)   NOT NULL,
    PasswordHash    NVARCHAR(500)   NULL,
    FullName        NVARCHAR(150)   NOT NULL,
    Phone           NVARCHAR(30)    NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_AspNetUsers_CreatedAt DEFAULT (SYSDATETIME())
);
GO
CREATE UNIQUE INDEX UX_AspNetUsers_Email ON dbo.AspNetUsers(Email);
GO

-- 1.2 Landlords
IF OBJECT_ID(N'dbo.Landlords', N'U') IS NOT NULL DROP TABLE dbo.Landlords;
GO
CREATE TABLE dbo.Landlords
(
    LandlordId  INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Landlords PRIMARY KEY,
    UserId      INT           NOT NULL,
    DisplayName NVARCHAR(150) NOT NULL,
    Address     NVARCHAR(255) NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_Landlords_IsDeleted DEFAULT (0),
    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Landlords_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Landlords_AspNetUsers
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id)
);
GO
-- A user can have at most 1 landlord profile
CREATE UNIQUE INDEX UX_Landlords_UserId_Active
ON dbo.Landlords(UserId)
WHERE IsDeleted = 0;
GO

-- 1.3 Properties
IF OBJECT_ID(N'dbo.Properties', N'U') IS NOT NULL DROP TABLE dbo.Properties;
GO
CREATE TABLE dbo.Properties
(
    PropertyId   INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Properties PRIMARY KEY,
    LandlordId   INT            NOT NULL,
    Name         NVARCHAR(150)  NOT NULL,
    Address      NVARCHAR(255)  NOT NULL,
    Description  NVARCHAR(MAX)  NULL,
    IsDeleted    BIT            NOT NULL CONSTRAINT DF_Properties_IsDeleted DEFAULT (0),
    CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_Properties_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Properties_Landlords
        FOREIGN KEY (LandlordId) REFERENCES dbo.Landlords(LandlordId)
);
GO
-- Unique property name per landlord (only active rows)
CREATE UNIQUE INDEX UX_Properties_LandlordId_Name_Active
ON dbo.Properties(LandlordId, Name)
WHERE IsDeleted = 0;
GO

-- 1.4 Rooms
IF OBJECT_ID(N'dbo.Rooms', N'U') IS NOT NULL DROP TABLE dbo.Rooms;
GO
CREATE TABLE dbo.Rooms
(
    RoomId        INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY,
    PropertyId    INT            NOT NULL,
    RoomName      NVARCHAR(50)   NOT NULL,
    RentPrice     DECIMAL(18,2)  NOT NULL,
    Status        VARCHAR(20)    NOT NULL, -- available|occupied|maintenance
    MaxOccupants  INT            NOT NULL,
    IsDeleted     BIT            NOT NULL CONSTRAINT DF_Rooms_IsDeleted DEFAULT (0),
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_Rooms_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Rooms_Properties
        FOREIGN KEY (PropertyId) REFERENCES dbo.Properties(PropertyId),

    CONSTRAINT CK_Rooms_Status
        CHECK (Status IN ('available','occupied','maintenance')),

    CONSTRAINT CK_Rooms_MaxOccupants
        CHECK (MaxOccupants > 0)
);
GO
-- Unique room name per property (only active rows)
CREATE UNIQUE INDEX UX_Rooms_PropertyId_RoomName_Active
ON dbo.Rooms(PropertyId, RoomName)
WHERE IsDeleted = 0;
GO

-- 1.5 Tenants
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL DROP TABLE dbo.Tenants;
GO
CREATE TABLE dbo.Tenants
(
    TenantId     INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY,
    LandlordId   INT            NOT NULL,
    FullName     NVARCHAR(150)  NOT NULL,
    Phone        NVARCHAR(30)   NULL,
    Email        NVARCHAR(256)  NULL,
    IdentityNo   NVARCHAR(50)   NULL,
    IsDeleted    BIT            NOT NULL CONSTRAINT DF_Tenants_IsDeleted DEFAULT (0),
    CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_Tenants_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Tenants_Landlords
        FOREIGN KEY (LandlordId) REFERENCES dbo.Landlords(LandlordId)
);
GO
-- Prevent duplicate identity number per landlord (only active rows, and only when IdentityNo provided)
CREATE UNIQUE INDEX UX_Tenants_LandlordId_IdentityNo_Active
ON dbo.Tenants(LandlordId, IdentityNo)
WHERE IsDeleted = 0 AND IdentityNo IS NOT NULL;
GO

-- 1.6 Contracts
IF OBJECT_ID(N'dbo.Contracts', N'U') IS NOT NULL DROP TABLE dbo.Contracts;
GO
CREATE TABLE dbo.Contracts
(
    ContractId     INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Contracts PRIMARY KEY,
    RoomId         INT            NOT NULL,
    TenantId       INT            NOT NULL,
    DepositAmount  DECIMAL(18,2)  NOT NULL,
    StartDate      DATE           NOT NULL,
    EndDate        DATE           NOT NULL,
    Status         VARCHAR(20)    NOT NULL, -- active|ended|cancelled
    IsDeleted      BIT            NOT NULL CONSTRAINT DF_Contracts_IsDeleted DEFAULT (0),
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_Contracts_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Contracts_Rooms   FOREIGN KEY (RoomId)   REFERENCES dbo.Rooms(RoomId),
    CONSTRAINT FK_Contracts_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),

    CONSTRAINT CK_Contracts_Status CHECK (Status IN ('active','ended','cancelled')),
    CONSTRAINT CK_Contracts_DateRange CHECK (EndDate >= StartDate)
);
GO
-- Only one active (non-deleted) contract per room at a time
CREATE UNIQUE INDEX UX_Contracts_RoomId_ActiveOnly
ON dbo.Contracts(RoomId)
WHERE IsDeleted = 0 AND Status = 'active';
GO

-- 1.7 RoomOccupancies
IF OBJECT_ID(N'dbo.RoomOccupancies', N'U') IS NOT NULL DROP TABLE dbo.RoomOccupancies;
GO
CREATE TABLE dbo.RoomOccupancies
(
    OccupancyId  INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_RoomOccupancies PRIMARY KEY,
    RoomId       INT           NOT NULL,
    TenantId     INT           NOT NULL,
    MoveInDate   DATE          NOT NULL,
    MoveOutDate  DATE          NULL,
    IsPrimary    BIT           NOT NULL CONSTRAINT DF_RoomOccupancies_IsPrimary DEFAULT (0),
    Status       VARCHAR(20)   NOT NULL, -- active|inactive
    CreatedAt    DATETIME2     NOT NULL CONSTRAINT DF_RoomOccupancies_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_RoomOccupancies_Rooms   FOREIGN KEY (RoomId)   REFERENCES dbo.Rooms(RoomId),
    CONSTRAINT FK_RoomOccupancies_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),

    CONSTRAINT CK_RoomOccupancies_Status CHECK (Status IN ('active','inactive')),
    CONSTRAINT CK_RoomOccupancies_MoveOutAfterIn CHECK (MoveOutDate IS NULL OR MoveOutDate >= MoveInDate)
);
GO
-- At most 1 primary active occupancy per room
CREATE UNIQUE INDEX UX_RoomOccupancies_PrimaryActivePerRoom
ON dbo.RoomOccupancies(RoomId)
WHERE Status = 'active' AND IsPrimary = 1;
GO

-- 1.8 RoomUtilitySettings (pricing history)
IF OBJECT_ID(N'dbo.RoomUtilitySettings', N'U') IS NOT NULL DROP TABLE dbo.RoomUtilitySettings;
GO
CREATE TABLE dbo.RoomUtilitySettings
(
    UtilitySettingId  INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_RoomUtilitySettings PRIMARY KEY,
    RoomId            INT            NOT NULL,
    ElectricUnitPrice DECIMAL(18,2)  NOT NULL,
    WaterUnitPrice    DECIMAL(18,2)  NOT NULL,
    InternetFee       DECIMAL(18,2)  NOT NULL CONSTRAINT DF_RoomUtilitySettings_InternetFee DEFAULT (0),
    TrashFee          DECIMAL(18,2)  NOT NULL CONSTRAINT DF_RoomUtilitySettings_TrashFee DEFAULT (0),
    EffectiveFrom     DATE           NOT NULL,
    EffectiveTo       DATE           NULL,
    CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_RoomUtilitySettings_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_RoomUtilitySettings_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId),
    CONSTRAINT CK_RoomUtilitySettings_DateRange CHECK (EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom)
);
GO
-- Avoid overlapping effective ranges for same room (simple guard: unique start date per room)
CREATE UNIQUE INDEX UX_RoomUtilitySettings_RoomId_EffectiveFrom
ON dbo.RoomUtilitySettings(RoomId, EffectiveFrom);
GO

-- 1.9 MeterReadings
IF OBJECT_ID(N'dbo.MeterReadings', N'U') IS NOT NULL DROP TABLE dbo.MeterReadings;
GO
CREATE TABLE dbo.MeterReadings
(
    MeterReadingId   INT        IDENTITY(1,1) NOT NULL CONSTRAINT PK_MeterReadings PRIMARY KEY,
    RoomId           INT        NOT NULL,
    PeriodMonth      INT        NOT NULL,  -- YYYYMM
    ElectricOld      INT        NOT NULL,
    ElectricNew      INT        NOT NULL,
    WaterOld         INT        NOT NULL,
    WaterNew         INT        NOT NULL,
    RecordedAt       DATETIME2  NOT NULL CONSTRAINT DF_MeterReadings_RecordedAt DEFAULT (SYSDATETIME()),
    RecordedByUserId INT        NOT NULL,

    CONSTRAINT FK_MeterReadings_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId),
    CONSTRAINT FK_MeterReadings_AspNetUsers FOREIGN KEY (RecordedByUserId) REFERENCES dbo.AspNetUsers(Id),

    CONSTRAINT CK_MeterReadings_PeriodMonth CHECK (PeriodMonth BETWEEN 190001 AND 999912),
    CONSTRAINT CK_MeterReadings_ElectricNonDecreasing CHECK (ElectricNew >= ElectricOld),
    CONSTRAINT CK_MeterReadings_WaterNonDecreasing CHECK (WaterNew >= WaterOld)
);
GO
-- One reading per room per month
CREATE UNIQUE INDEX UX_MeterReadings_RoomId_PeriodMonth
ON dbo.MeterReadings(RoomId, PeriodMonth);
GO

-- 1.10 Invoices
IF OBJECT_ID(N'dbo.Invoices', N'U') IS NOT NULL DROP TABLE dbo.Invoices;
GO
CREATE TABLE dbo.Invoices
(
    InvoiceId    INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    ContractId   INT           NOT NULL,
    RoomId       INT           NOT NULL,
    PeriodMonth  INT           NOT NULL, -- YYYYMM
    TotalAmount  DECIMAL(18,2) NOT NULL,
    Status       VARCHAR(20)   NOT NULL, -- draft|unpaid|paid|cancelled
    DueDate      DATE          NOT NULL,
    CreatedAt    DATETIME2     NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Invoices_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(ContractId),
    CONSTRAINT FK_Invoices_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId),

    CONSTRAINT CK_Invoices_Status CHECK (Status IN ('draft','unpaid','paid','cancelled')),
    CONSTRAINT CK_Invoices_PeriodMonth CHECK (PeriodMonth BETWEEN 190001 AND 999912),
    CONSTRAINT CK_Invoices_TotalAmountNonNegative CHECK (TotalAmount >= 0)
);
GO
-- One invoice per room per month (regardless of status) - adjust later if needed
CREATE UNIQUE INDEX UX_Invoices_RoomId_PeriodMonth
ON dbo.Invoices(RoomId, PeriodMonth);
GO

-- 1.11 InvoiceLines
IF OBJECT_ID(N'dbo.InvoiceLines', N'U') IS NOT NULL DROP TABLE dbo.InvoiceLines;
GO
CREATE TABLE dbo.InvoiceLines
(
    InvoiceLineId INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceLines PRIMARY KEY,
    InvoiceId     INT            NOT NULL,
    ItemType      VARCHAR(20)    NOT NULL, -- rent|electric|water|internet|trash|other
    Description   NVARCHAR(255)  NULL,
    Quantity      DECIMAL(18,2)  NOT NULL,
    UnitPrice     DECIMAL(18,2)  NOT NULL,
    LineTotal     AS (ROUND(Quantity * UnitPrice, 2)) PERSISTED,

    CONSTRAINT FK_InvoiceLines_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT CK_InvoiceLines_ItemType CHECK (ItemType IN ('rent','electric','water','internet','trash','other')),
    CONSTRAINT CK_InvoiceLines_QuantityNonNegative CHECK (Quantity >= 0),
    CONSTRAINT CK_InvoiceLines_UnitPriceNonNegative CHECK (UnitPrice >= 0)
);
GO

-- 1.12 PaymentIntents
IF OBJECT_ID(N'dbo.PaymentIntents', N'U') IS NOT NULL DROP TABLE dbo.PaymentIntents;
GO
CREATE TABLE dbo.PaymentIntents
(
    PaymentIntentId   INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentIntents PRIMARY KEY,
    InvoiceId         INT            NOT NULL,
    Provider          VARCHAR(20)    NOT NULL, -- momo|zalopay|vnpay|bank
    ProviderIntentId  NVARCHAR(100)  NULL,
    Amount            DECIMAL(18,2)  NOT NULL,
    Status            VARCHAR(20)    NOT NULL, -- created|pending|succeeded|failed|cancelled
    CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_PaymentIntents_CreatedAt DEFAULT (SYSDATETIME()),
    ExpiredAt         DATETIME2      NULL,

    CONSTRAINT FK_PaymentIntents_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT CK_PaymentIntents_Provider CHECK (Provider IN ('momo','zalopay','vnpay','bank')),
    CONSTRAINT CK_PaymentIntents_Status CHECK (Status IN ('created','pending','succeeded','failed','cancelled')),
    CONSTRAINT CK_PaymentIntents_AmountNonNegative CHECK (Amount >= 0)
);
GO

-- 1.13 Payments
IF OBJECT_ID(N'dbo.Payments', N'U') IS NOT NULL DROP TABLE dbo.Payments;
GO
CREATE TABLE dbo.Payments
(
    PaymentId        INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    InvoiceId        INT            NOT NULL,
    PaymentIntentId  INT            NOT NULL,
    Provider         VARCHAR(20)    NOT NULL,
    ProviderTxnId    NVARCHAR(120)  NOT NULL,
    Amount           DECIMAL(18,2)  NOT NULL,
    PaidAt           DATETIME2      NOT NULL CONSTRAINT DF_Payments_PaidAt DEFAULT (SYSDATETIME()),
    Status           VARCHAR(20)    NOT NULL, -- succeeded|failed
    RawCallbackJson  NVARCHAR(MAX)  NULL,

    CONSTRAINT FK_Payments_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT FK_Payments_PaymentIntents FOREIGN KEY (PaymentIntentId) REFERENCES dbo.PaymentIntents(PaymentIntentId),
    CONSTRAINT CK_Payments_Status CHECK (Status IN ('succeeded','failed')),
    CONSTRAINT CK_Payments_AmountNonNegative CHECK (Amount >= 0)
);
GO
-- ProviderTxnId must be globally unique
CREATE UNIQUE INDEX UX_Payments_ProviderTxnId ON dbo.Payments(ProviderTxnId);
GO

-- 1.14 Transactions (cashbook)
IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL DROP TABLE dbo.Transactions;
GO
CREATE TABLE dbo.Transactions
(
    TransactionId    INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
    LandlordId       INT            NOT NULL,
    RoomId           INT            NULL,
    TenantId         INT            NULL,
    ContractId       INT            NULL,
    InvoiceId        INT            NULL,
    Type             VARCHAR(30)    NOT NULL, -- deposit_in|deposit_refund|manual_charge|manual_refund
    Amount           DECIMAL(18,2)  NOT NULL,
    Direction        VARCHAR(10)    NOT NULL, -- in|out
    Note             NVARCHAR(255)  NULL,
    CreatedAt        DATETIME2      NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT (SYSDATETIME()),
    CreatedByUserId  INT            NOT NULL,

    CONSTRAINT FK_Transactions_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.Landlords(LandlordId),
    CONSTRAINT FK_Transactions_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId),
    CONSTRAINT FK_Transactions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
    CONSTRAINT FK_Transactions_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(ContractId),
    CONSTRAINT FK_Transactions_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT FK_Transactions_AspNetUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AspNetUsers(Id),

    CONSTRAINT CK_Transactions_Type CHECK (Type IN ('deposit_in','deposit_refund','manual_charge','manual_refund')),
    CONSTRAINT CK_Transactions_Direction CHECK (Direction IN ('in','out')),
    CONSTRAINT CK_Transactions_AmountNonNegative CHECK (Amount >= 0)
);
GO

-- 1.15 StoredFile  (was: Files)
IF OBJECT_ID(N'dbo.StoredFile', N'U') IS NOT NULL DROP TABLE dbo.StoredFile;
GO
CREATE TABLE dbo.StoredFile
(
    StoredFileId      INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_StoredFile PRIMARY KEY,
    LandlordId        INT            NOT NULL,
    FileName          NVARCHAR(255)  NOT NULL,
    MimeType          NVARCHAR(100)  NOT NULL,
    StoragePath       NVARCHAR(500)  NOT NULL,
    UploadedAt        DATETIME2      NOT NULL CONSTRAINT DF_StoredFile_UploadedAt DEFAULT (SYSDATETIME()),
    UploadedByUserId  INT            NOT NULL,

    CONSTRAINT FK_StoredFile_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.Landlords(LandlordId),
    CONSTRAINT FK_StoredFile_AspNetUsers FOREIGN KEY (UploadedByUserId) REFERENCES dbo.AspNetUsers(Id)
);
GO
-- Optional: prevent duplicate storage path
CREATE UNIQUE INDEX UX_StoredFile_StoragePath ON dbo.StoredFile(StoragePath);
GO

-- 1.16 StoredFileReference (was: FileReferences)
IF OBJECT_ID(N'dbo.StoredFileReference', N'U') IS NOT NULL DROP TABLE dbo.StoredFileReference;
GO
CREATE TABLE dbo.StoredFileReference
(
    StoredFileRefId  INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_StoredFileReference PRIMARY KEY,
    StoredFileId     INT           NOT NULL,
    RefType          VARCHAR(20)   NOT NULL, -- contract|invoice|meter|ticket|property|room|tenant
    RefId            INT           NOT NULL,
    CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_StoredFileReference_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_StoredFileReference_StoredFile
        FOREIGN KEY (StoredFileId) REFERENCES dbo.StoredFile(StoredFileId),

    CONSTRAINT CK_StoredFileReference_RefType
        CHECK (RefType IN ('contract','invoice','meter','ticket','property','room','tenant'))
);
GO
-- Basic dedupe: same file cannot be linked to same target twice
CREATE UNIQUE INDEX UX_StoredFileReference_Dedupe
ON dbo.StoredFileReference(StoredFileId, RefType, RefId);
GO

-- 1.17 Notifications
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NOT NULL DROP TABLE dbo.Notifications;
GO
CREATE TABLE dbo.Notifications
(
    NotificationId INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
    LandlordId     INT            NOT NULL,
    TenantId       INT            NULL,
    Channel        VARCHAR(10)    NOT NULL, -- app|email|sms
    Type           VARCHAR(30)    NOT NULL, -- invoice_due|contract_expiring|payment_success|custom
    Title          NVARCHAR(150)  NOT NULL,
    Content        NVARCHAR(MAX)  NOT NULL,
    Status         VARCHAR(20)    NOT NULL, -- queued|sent|failed|read
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSDATETIME()),
    SentAt         DATETIME2      NULL,
    ReadAt         DATETIME2      NULL,

    CONSTRAINT FK_Notifications_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.Landlords(LandlordId),
    CONSTRAINT FK_Notifications_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),

    CONSTRAINT CK_Notifications_Channel CHECK (Channel IN ('app','email','sms')),
    CONSTRAINT CK_Notifications_Type CHECK (Type IN ('invoice_due','contract_expiring','payment_success','custom')),
    CONSTRAINT CK_Notifications_Status CHECK (Status IN ('queued','sent','failed','read'))
);
GO

-- 1.18 Subscriptions
IF OBJECT_ID(N'dbo.Subscriptions', N'U') IS NOT NULL DROP TABLE dbo.Subscriptions;
GO
CREATE TABLE dbo.Subscriptions
(
    SubscriptionId INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_Subscriptions PRIMARY KEY,
    LandlordId     INT            NOT NULL,
    PlanName       NVARCHAR(50)   NOT NULL,
    Status         VARCHAR(20)    NOT NULL, -- active|past_due|cancelled|expired
    StartDate      DATE           NOT NULL,
    EndDate        DATE           NOT NULL,
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_Subscriptions_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Subscriptions_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.Landlords(LandlordId),
    CONSTRAINT CK_Subscriptions_Status CHECK (Status IN ('active','past_due','cancelled','expired')),
    CONSTRAINT CK_Subscriptions_DateRange CHECK (EndDate >= StartDate)
);
GO

------------------------------------------------------------
-- 2) Stored Procedure: Soft delete Property + Rooms
------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_SoftDeleteProperty', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SoftDeleteProperty;
GO
CREATE PROCEDURE dbo.sp_SoftDeleteProperty
    @PropertyId INT,
    @DeletedByUserId INT = NULL -- optional audit (not stored in this minimal ERD)
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
-- 3) Trigger: Block Room soft delete if active Contract exists (Option 1)
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
-- 4) Helpful Indexes (performance)
------------------------------------------------------------
-- Common lookups
CREATE INDEX IX_Rooms_PropertyId ON dbo.Rooms(PropertyId) WHERE IsDeleted = 0;
CREATE INDEX IX_Tenants_LandlordId ON dbo.Tenants(LandlordId) WHERE IsDeleted = 0;
CREATE INDEX IX_Contracts_RoomId ON dbo.Contracts(RoomId) WHERE IsDeleted = 0;
CREATE INDEX IX_Invoices_ContractId ON dbo.Invoices(ContractId);
CREATE INDEX IX_Payments_InvoiceId ON dbo.Payments(InvoiceId);
GO
