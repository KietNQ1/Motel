USE MotelDb;
GO
USE MotelDb;
GO
SELECT TOP(1)
FROM Landlords
WHERE UserId = @currentUserId AND IsDeleted = 0



ALTER TABLE dbo.StoredFileReference
DROP CONSTRAINT CK_StoredFileReference_RefType;
GO

ALTER TABLE dbo.StoredFileReference
ADD CONSTRAINT CK_StoredFileReference_RefType
CHECK (
    [RefType] = 'tenant'
    OR [RefType] = 'room'
    OR [RefType] = 'property'
    OR [RefType] = 'ticket'
    OR [RefType] = 'meter'
    OR [RefType] = 'invoice'
    OR [RefType] = 'contract'
    OR [RefType] = 'roomfurniture'
);
GO
DROP TABLE dbo.RoomFurnitures;

-- Tạo bảng RoomFurnitures
CREATE TABLE [dbo].[RoomFurnitures] (
    [FurnitureId] INT IDENTITY(1,1) PRIMARY KEY,
    [RoomId] INT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Quantity] INT NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY ([RoomId]) REFERENCES [dbo].[Rooms]([RoomId]) ON DELETE CASCADE
);
GO 