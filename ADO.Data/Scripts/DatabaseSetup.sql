-- Database creation script
CREATE DATABASE OrderManagementDB;
GO

USE OrderManagementDB;
GO

-- Create Product table
CREATE TABLE Product (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Weight DECIMAL(18,2) NOT NULL,
    Height DECIMAL(18,2) NOT NULL,
    Width DECIMAL(18,2) NOT NULL,
    Length DECIMAL(18,2) NOT NULL
);

-- Create Order table
CREATE TABLE [Order] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Status INT NOT NULL, -- 0=NotStarted, 1=Loading, 2=InProgress, 3=Arrived, 4=Unloading, 5=Cancelled, 6=Done
    CreatedDate DATETIME2 NOT NULL,
    UpdatedDate DATETIME2 NOT NULL,
    ProductId INT NOT NULL,
    CONSTRAINT FK_Order_Product FOREIGN KEY (ProductId) REFERENCES Product(Id)
);

-- Create indexes for better performance
CREATE INDEX IX_Order_Status ON [Order](Status);
CREATE INDEX IX_Order_CreatedDate ON [Order](CreatedDate);
CREATE INDEX IX_Order_ProductId ON [Order](ProductId);
GO
-- Stored Procedure: Get Filtered Orders
CREATE PROCEDURE sp_GetFilteredOrders
    @Month INT = NULL,
    @Year INT = NULL,
    @Status INT = NULL,
    @ProductId INT = NULL
AS
BEGIN
    SELECT o.Id, o.Status, o.CreatedDate, o.UpdatedDate, o.ProductId,
           p.Name, p.Description, p.Weight, p.Height, p.Width, p.Length
    FROM [Order] o
    INNER JOIN Product p ON o.ProductId = p.Id
    WHERE (@Month IS NULL OR MONTH(o.CreatedDate) = @Month)
      AND (@Year IS NULL OR YEAR(o.CreatedDate) = @Year)
      AND (@Status IS NULL OR o.Status = @Status)
      AND (@ProductId IS NULL OR o.ProductId = @ProductId)
    ORDER BY o.CreatedDate DESC;
END;
GO

-- Stored Procedure: Bulk Delete Orders
CREATE PROCEDURE sp_BulkDeleteOrders
    @Month INT = NULL,
    @Year INT = NULL,
    @Status INT = NULL,
    @ProductId INT = NULL,
    @DeletedCount INT OUTPUT
AS
BEGIN
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DELETE FROM [Order]
        WHERE (@Month IS NULL OR MONTH(CreatedDate) = @Month)
          AND (@Year IS NULL OR YEAR(CreatedDate) = @Year)
          AND (@Status IS NULL OR Status = @Status)
          AND (@ProductId IS NULL OR ProductId = @ProductId);
        
        SET @DeletedCount = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Sample data insertion
INSERT INTO Product (Name, Description, Weight, Height, Width, Length) VALUES
('Laptop', 'Dell Inspiron 15', 2.5, 1.5, 35.0, 25.0),
('Monitor', '24 inch LED Monitor', 5.2, 40.0, 55.0, 20.0),
('Keyboard', 'Mechanical Gaming Keyboard', 1.2, 3.5, 45.0, 15.0),
('Mouse', 'Wireless Optical Mouse', 0.1, 2.0, 12.0, 6.0);

INSERT INTO [Order] (Status, CreatedDate, UpdatedDate, ProductId) VALUES
(0, '2025-08-01 10:00:00', '2025-08-01 10:00:00', 1),
(1, '2025-08-02 11:30:00', '2025-08-02 12:00:00', 2),
(2, '2025-08-03 14:15:00', '2025-08-03 15:30:00', 1),
(6, '2025-07-25 09:00:00', '2025-07-25 17:00:00', 3),
(5, '2025-07-30 16:45:00', '2025-07-30 16:45:00', 4);
