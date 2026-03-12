-- ========================================
-- Tax Estimation Module Database Schema
-- ========================================
-- Purpose: Store tax rules and tax estimations for rental income
-- Created: 2026-03-09
-- Note: Run this script to add tax estimation functionality

USE [MotelDB]
GO

-- ========================================
-- Table: TaxRules
-- ========================================
-- Description: Stores tax regulations that may change over time
-- Business Logic:
--   - VAT (Value Added Tax) for rental property in Vietnam
--   - PIT (Personal Income Tax) for rental income
--   - Revenue threshold for tax exemption
--   - Effective date to track regulatory changes

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TaxRules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TaxRules] (
        [TaxRuleId] INT IDENTITY(1,1) NOT NULL,
        [RuleName] NVARCHAR(200) NOT NULL,
        [VatRate] DECIMAL(5, 4) NOT NULL,                    -- e.g., 0.0500 = 5%
        [PitRate] DECIMAL(5, 4) NOT NULL,                    -- e.g., 0.0500 = 5%
        [RevenueThreshold] DECIMAL(18, 2) NOT NULL,          -- e.g., 100,000,000 VND
        [EffectiveDate] DATE NOT NULL,                       -- When this rule takes effect
        [EndDate] DATE NULL,                                 -- When this rule expires (NULL = current)
        [IsActive] BIT NOT NULL DEFAULT 1,
        [Notes] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_TaxRules] PRIMARY KEY CLUSTERED ([TaxRuleId] ASC)
    )
END
GO

-- ========================================
-- Table: TaxEstimations
-- ========================================
-- Description: Stores tax estimation results for each landlord per calendar year
-- Business Logic:
--   - One record per landlord per year
--   - Revenue from rent only (excludes utility fees collected on behalf)
--   - Calculated based on contract amounts, not actual payments
--   - Used for estimation only, not official tax declaration

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TaxEstimations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TaxEstimations] (
        [TaxEstimationId] INT IDENTITY(1,1) NOT NULL,
        [LandlordId] INT NOT NULL,
        [Year] INT NOT NULL,                                 -- Calendar year (e.g., 2026)
        [TotalRevenue] DECIMAL(18, 2) NOT NULL,              -- Total rental revenue for the year
        [TaxableRevenue] DECIMAL(18, 2) NOT NULL,            -- Revenue subject to tax
        [VatAmount] DECIMAL(18, 2) NOT NULL,                 -- VAT = TaxableRevenue * VatRate
        [PitAmount] DECIMAL(18, 2) NOT NULL,                 -- PIT = TaxableRevenue * PitRate
        [TotalTaxAmount] DECIMAL(18, 2) NOT NULL,            -- Total = VAT + PIT
        [TaxRuleId] INT NOT NULL,                            -- Reference to applied rule
        [IsExempt] BIT NOT NULL DEFAULT 0,                   -- TRUE if below threshold
        [Notes] NVARCHAR(MAX) NULL,
        [CalculatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_TaxEstimations] PRIMARY KEY CLUSTERED ([TaxEstimationId] ASC),
        CONSTRAINT [FK_TaxEstimations_Landlords] FOREIGN KEY ([LandlordId])
            REFERENCES [dbo].[Landlords] ([LandlordId]),
        CONSTRAINT [FK_TaxEstimations_TaxRules] FOREIGN KEY ([TaxRuleId])
            REFERENCES [dbo].[TaxRules] ([TaxRuleId]),
        CONSTRAINT [UK_TaxEstimations_LandlordYear] UNIQUE ([LandlordId], [Year])
    )
END
GO

-- ========================================
-- Create Indexes
-- ========================================

-- Index for finding active tax rule by effective date
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TaxRules_EffectiveDate' AND object_id = OBJECT_ID(N'[dbo].[TaxRules]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TaxRules_EffectiveDate] 
    ON [dbo].[TaxRules] ([EffectiveDate] DESC, [IsActive])
END
GO

-- Index for landlord tax estimation lookup
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TaxEstimations_Landlord' AND object_id = OBJECT_ID(N'[dbo].[TaxEstimations]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TaxEstimations_Landlord] 
    ON [dbo].[TaxEstimations] ([LandlordId], [Year] DESC)
END
GO

-- ========================================
-- Seed Initial Tax Rule (Current Vietnam Regulation)
-- ========================================
-- Based on: Circular 40/2021/TT-BTC
-- Effective: 2021-06-07
-- Note: This is a simplified version for educational purposes

IF NOT EXISTS (SELECT * FROM [dbo].[TaxRules] WHERE [RuleName] = N'Thông tư 40/2021/TT-BTC')
BEGIN
    INSERT INTO [dbo].[TaxRules] 
        ([RuleName], [VatRate], [PitRate], [RevenueThreshold], [EffectiveDate], [EndDate], [IsActive], [Notes])
    VALUES 
        (
            N'Thông tư 40/2021/TT-BTC',
            0.0500,                          -- 5% VAT
            0.0500,                          -- 5% PIT
            100000000.00,                    -- 100 million VND threshold
            '2021-06-07',                    -- Effective date
            NULL,                            -- Currently active
            1,                               -- IsActive
            N'Luật thuế hiện hành cho cho thuê tài sản. Doanh thu trên 100 triệu/năm chịu thuế VAT 5% và thuế TNCN 5%.'
        )
END
GO

-- ========================================
-- Verification Query
-- ========================================

PRINT '========================================';
PRINT 'Tax Estimation Tables Created Successfully';
PRINT '========================================';
PRINT '';
PRINT 'Tables Created:';
PRINT '  - TaxRules';
PRINT '  - TaxEstimations';
PRINT '';
PRINT 'Seeded Data:';
SELECT 
    [RuleName],
    [VatRate] * 100 AS [VAT %],
    [PitRate] * 100 AS [PIT %],
    FORMAT([RevenueThreshold], 'N0') AS [Threshold (VND)],
    [EffectiveDate],
    CASE WHEN [IsActive] = 1 THEN N'Đang áp dụng' ELSE N'Không áp dụng' END AS [Status]
FROM [dbo].[TaxRules]
WHERE [IsActive] = 1
ORDER BY [EffectiveDate] DESC;
GO
