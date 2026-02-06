USE MotelDb;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

------------------------------------------------------------
-- 1) EXTEND AspNetUsers (Identity columns)
------------------------------------------------------------
ALTER TABLE dbo.AspNetUsers ADD
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0),
    SecurityStamp NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(256) NULL,
    PhoneNumber NVARCHAR(30) NULL,
    PhoneNumberConfirmed BIT NOT NULL CONSTRAINT DF_AspNetUsers_PhoneConfirmed DEFAULT (0),
    TwoFactorEnabled BIT NOT NULL CONSTRAINT DF_AspNetUsers_2FA DEFAULT (0),
    LockoutEnd DATETIMEOFFSET NULL,
    LockoutEnabled BIT NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (1),
    AccessFailedCount INT NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailed DEFAULT (0);
GO

-- Init normalized fields for existing users
UPDATE dbo.AspNetUsers
SET
    UserName = Email,
    NormalizedUserName = UPPER(Email),
    NormalizedEmail = UPPER(Email)
WHERE UserName IS NULL;
GO

CREATE UNIQUE INDEX UX_AspNetUsers_NormalizedUserName
ON dbo.AspNetUsers (NormalizedUserName)
WHERE NormalizedUserName IS NOT NULL;
GO

CREATE INDEX IX_AspNetUsers_NormalizedEmail
ON dbo.AspNetUsers (NormalizedEmail);
GO

------------------------------------------------------------
-- 2) AspNetRoles
------------------------------------------------------------
CREATE TABLE dbo.AspNetRoles
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(256) NULL
);
GO

CREATE UNIQUE INDEX UX_AspNetRoles_NormalizedName
ON dbo.AspNetRoles (NormalizedName)
WHERE NormalizedName IS NOT NULL;
GO

------------------------------------------------------------
-- 3) AspNetUserRoles
------------------------------------------------------------
CREATE TABLE dbo.AspNetUserRoles
(
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
);
GO

------------------------------------------------------------
-- 4) AspNetUserClaims
------------------------------------------------------------
CREATE TABLE dbo.AspNetUserClaims
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
    UserId INT NOT NULL,
    ClaimType NVARCHAR(256) NULL,
    ClaimValue NVARCHAR(1024) NULL,
    CONSTRAINT FK_UserClaims_Users FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
GO

------------------------------------------------------------
-- 5) AspNetRoleClaims
------------------------------------------------------------
CREATE TABLE dbo.AspNetRoleClaims
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
    RoleId INT NOT NULL,
    ClaimType NVARCHAR(256) NULL,
    ClaimValue NVARCHAR(1024) NULL,
    CONSTRAINT FK_RoleClaims_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
);
GO

------------------------------------------------------------
-- 6) AspNetUserLogins
------------------------------------------------------------
CREATE TABLE dbo.AspNetUserLogins
(
    LoginProvider NVARCHAR(128) NOT NULL,
    ProviderKey NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(256) NULL,
    UserId INT NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_UserLogins_Users FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
GO

------------------------------------------------------------
-- 7) AspNetUserTokens
------------------------------------------------------------
CREATE TABLE dbo.AspNetUserTokens
(
    UserId INT NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Value NVARCHAR(2048) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_UserTokens_Users FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
GO

------------------------------------------------------------
-- 8) SEED ROLES
------------------------------------------------------------
INSERT INTO dbo.AspNetRoles (Name, NormalizedName)
VALUES
    (N'Admin',    N'ADMIN'),
    (N'Landlord', N'LANDLORD'),
    (N'Staff',    N'STAFF');
GO
