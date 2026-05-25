IF DB_ID('BuildWiseDB') IS NULL
BEGIN
    CREATE DATABASE BuildWiseDB;
END
GO

USE BuildWiseDB;
GO

IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
    THROW 50001, 'Users table is missing. Run BuildWiseDB/BuildWiseDB.sql first, then run migrate.sql.', 1;
END
GO

IF OBJECT_ID('Projects', 'U') IS NULL
BEGIN
    THROW 50002, 'Projects table is missing. Run BuildWiseDB/BuildWiseDB.sql first, then run migrate.sql.', 1;
END
GO

IF OBJECT_ID('Phases', 'U') IS NULL
BEGIN
    THROW 50003, 'Phases table is missing. Run BuildWiseDB/BuildWiseDB.sql first, then run migrate.sql.', 1;
END
GO

-- User profile fields
IF COL_LENGTH('Users', 'City') IS NULL
BEGIN
    ALTER TABLE Users ADD City nvarchar(100) NULL;
END
GO

IF COL_LENGTH('Users', 'Profession') IS NULL
BEGIN
    ALTER TABLE Users ADD Profession nvarchar(100) NULL;
END
GO

-- Project ownership
IF COL_LENGTH('Projects', 'UserID') IS NULL
BEGIN
    ALTER TABLE Projects ADD UserID int NULL;
END
GO

IF COL_LENGTH('Projects', 'UserID') IS NOT NULL
BEGIN
    EXEC('UPDATE Projects SET UserID = (SELECT TOP 1 UserID FROM Users ORDER BY UserID) WHERE UserID IS NULL');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Projects_Users')
BEGIN
    ALTER TABLE Projects ADD CONSTRAINT FK_Projects_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO

-- Property module lookup tables
IF OBJECT_ID('AreaUnit', 'U') IS NULL
BEGIN
    CREATE TABLE AreaUnit (
        UnitID tinyint NOT NULL PRIMARY KEY,
        UnitName varchar(20) NOT NULL UNIQUE
    );
END
GO

INSERT INTO AreaUnit (UnitID, UnitName)
SELECT v.UnitID, v.UnitName
FROM (VALUES (1, 'Marla'), (2, 'Kanal'), (3, 'Square Feet'), (4, 'Square Meters')) v(UnitID, UnitName)
WHERE NOT EXISTS (SELECT 1 FROM AreaUnit a WHERE a.UnitName = v.UnitName)
  AND NOT EXISTS (SELECT 1 FROM AreaUnit a WHERE a.UnitID = v.UnitID);
GO

IF OBJECT_ID('PropertyType', 'U') IS NULL
BEGIN
    CREATE TABLE PropertyType (
        TypeID tinyint NOT NULL PRIMARY KEY,
        TypeName varchar(30) NOT NULL UNIQUE
    );
END
GO

INSERT INTO PropertyType (TypeID, TypeName)
SELECT v.TypeID, v.TypeName
FROM (VALUES (1, 'Plot'), (2, 'House'), (3, 'Apartment'), (4, 'Commercial')) v(TypeID, TypeName)
WHERE NOT EXISTS (SELECT 1 FROM PropertyType t WHERE t.TypeName = v.TypeName)
  AND NOT EXISTS (SELECT 1 FROM PropertyType t WHERE t.TypeID = v.TypeID);
GO

IF OBJECT_ID('PropertyStatus', 'U') IS NULL
BEGIN
    CREATE TABLE PropertyStatus (
        StatusID tinyint NOT NULL PRIMARY KEY,
        StatusName varchar(30) NOT NULL UNIQUE
    );
END
GO

INSERT INTO PropertyStatus (StatusID, StatusName)
SELECT v.StatusID, v.StatusName
FROM (VALUES (1, 'Planned'), (2, 'Under Construction'), (3, 'Completed'), (4, 'On Hold')) v(StatusID, StatusName)
WHERE NOT EXISTS (SELECT 1 FROM PropertyStatus s WHERE s.StatusName = v.StatusName)
  AND NOT EXISTS (SELECT 1 FROM PropertyStatus s WHERE s.StatusID = v.StatusID);
GO

-- Property module table
IF OBJECT_ID('Properties', 'U') IS NULL
BEGIN
    CREATE TABLE Properties (
        PropertyID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID int NOT NULL,
        ProjectID int NULL,
        PropertyName nvarchar(150) NOT NULL,
        TypeID tinyint NOT NULL,
        StatusID tinyint NOT NULL DEFAULT 1,
        Location nvarchar(255) NOT NULL,
        City nvarchar(100) NULL,
        AreaSize decimal(12,2) NOT NULL,
        AreaUnitID tinyint NOT NULL,
        Notes nvarchar(500) NULL,
        CreatedAt datetime NOT NULL DEFAULT GETDATE(),
        UpdatedAt datetime NOT NULL DEFAULT GETDATE()
    );
END
GO

IF COL_LENGTH('Properties', 'UserID') IS NULL
BEGIN
    ALTER TABLE Properties ADD UserID int NULL;
END
GO

IF COL_LENGTH('Properties', 'City') IS NULL
BEGIN
    ALTER TABLE Properties ADD City nvarchar(100) NULL;
END
GO

IF COL_LENGTH('Properties', 'ProjectID') IS NULL
BEGIN
    ALTER TABLE Properties ADD ProjectID int NULL;
END
GO

IF COL_LENGTH('Properties', 'UserID') IS NOT NULL
BEGIN
    EXEC('UPDATE Properties SET UserID = (SELECT TOP 1 UserID FROM Users ORDER BY UserID) WHERE UserID IS NULL');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Properties_Users')
BEGIN
    ALTER TABLE Properties ADD CONSTRAINT FK_Properties_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Properties_Projects_ProjectID')
BEGIN
    ALTER TABLE Properties ADD CONSTRAINT FK_Properties_Projects_ProjectID FOREIGN KEY (ProjectID) REFERENCES Projects(ProjectID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Properties_ProjectID' AND object_id = OBJECT_ID('Properties'))
BEGIN
    CREATE INDEX IX_Properties_ProjectID ON Properties(ProjectID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Properties_PropertyType')
BEGIN
    ALTER TABLE Properties ADD CONSTRAINT FK_Properties_PropertyType FOREIGN KEY (TypeID) REFERENCES PropertyType(TypeID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Properties_PropertyStatus')
BEGIN
    ALTER TABLE Properties ADD CONSTRAINT FK_Properties_PropertyStatus FOREIGN KEY (StatusID) REFERENCES PropertyStatus(StatusID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Properties_AreaUnit')
BEGIN
    ALTER TABLE Properties ADD CONSTRAINT FK_Properties_AreaUnit FOREIGN KEY (AreaUnitID) REFERENCES AreaUnit(UnitID);
END
GO

-- Make sure every existing user has a main project.
-- New users already get this from AccountController, but old users need it too.
INSERT INTO Properties (UserID, PropertyName, TypeID, StatusID, Location, City, AreaSize, AreaUnitID, Notes, CreatedAt, UpdatedAt)
SELECT
    u.UserID,
    'Default Property',
    (SELECT TOP 1 TypeID FROM PropertyType ORDER BY TypeID),
    (SELECT TOP 1 StatusID FROM PropertyStatus ORDER BY StatusID),
    'Not specified',
    NULL,
    0,
    (SELECT TOP 1 UnitID FROM AreaUnit ORDER BY UnitID),
    'Auto-created for the main project.',
    GETDATE(),
    GETDATE()
FROM Users u
WHERE NOT EXISTS (SELECT 1 FROM Properties p WHERE p.UserID = u.UserID);
GO

INSERT INTO Projects (PropertyID, UserID, ProjectName, Description, StartDate, ExpectedEndDate, ActualEndDate, TotalBudget, IsCompleted, CreatedAt, UpdatedAt)
SELECT
    p.PropertyID,
    u.UserID,
    'main',
    'Default project created by BuildWise.',
    CONVERT(date, GETDATE()),
    NULL,
    NULL,
    0,
    0,
    GETDATE(),
    GETDATE()
FROM Users u
CROSS APPLY (
    SELECT TOP 1 PropertyID
    FROM Properties p
    WHERE p.UserID = u.UserID
    ORDER BY p.PropertyID
) p
WHERE NOT EXISTS (
    SELECT 1
    FROM Projects pr
    WHERE pr.UserID = u.UserID
      AND pr.ProjectName = 'main'
);
GO

UPDATE p
SET ProjectID = pr.ProjectID,
    UpdatedAt = GETDATE()
FROM Properties p
CROSS APPLY (
    SELECT TOP 1 ProjectID
    FROM Projects pr
    WHERE pr.PropertyID = p.PropertyID
    ORDER BY pr.ProjectID
) pr
WHERE p.ProjectID IS NULL;
GO

-- Material module lookup and main tables
IF OBJECT_ID('MaterialUnit', 'U') IS NULL
BEGIN
    CREATE TABLE MaterialUnit (
        UnitID tinyint NOT NULL PRIMARY KEY,
        UnitName varchar(30) NOT NULL UNIQUE
    );
END
GO

INSERT INTO MaterialUnit (UnitID, UnitName)
SELECT v.UnitID, v.UnitName
FROM (VALUES
    (1, 'Bag'),
    (2, 'Piece'),
    (3, 'Ton'),
    (4, 'Cubic Feet'),
    (5, 'Liter'),
    (6, 'Bundle'),
    (7, 'Feet'),
    (8, 'Square Feet')
) v(UnitID, UnitName)
WHERE NOT EXISTS (SELECT 1 FROM MaterialUnit u WHERE u.UnitName = v.UnitName)
  AND NOT EXISTS (SELECT 1 FROM MaterialUnit u WHERE u.UnitID = v.UnitID);
GO

IF OBJECT_ID('Suppliers', 'U') IS NULL
BEGIN
    CREATE TABLE Suppliers (
        SupplierID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierName nvarchar(150) NOT NULL,
        Phone varchar(20) NULL,
        Email varchar(150) NULL,
        Address nvarchar(300) NULL,
        IsActive bit NOT NULL DEFAULT 1
    );
END
GO

IF OBJECT_ID('Materials', 'U') IS NULL
BEGIN
    CREATE TABLE Materials (
        MaterialID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MaterialName nvarchar(100) NOT NULL UNIQUE,
        DefaultUnitID tinyint NOT NULL,
        Description nvarchar(300) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        UserID int NULL
    );
END
GO

IF COL_LENGTH('Materials', 'UserID') IS NULL
BEGIN
    ALTER TABLE Materials ADD UserID int NULL;
END
GO

IF COL_LENGTH('Materials', 'IsActive') IS NULL
BEGIN
    ALTER TABLE Materials ADD IsActive bit NOT NULL DEFAULT 1;
END
GO

IF COL_LENGTH('Materials', 'UserID') IS NOT NULL
BEGIN
    EXEC('UPDATE Materials SET UserID = (SELECT TOP 1 UserID FROM Users ORDER BY UserID) WHERE UserID IS NULL');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Materials_Users')
BEGIN
    ALTER TABLE Materials ADD CONSTRAINT FK_Materials_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Materials_MaterialUnit')
BEGIN
    ALTER TABLE Materials ADD CONSTRAINT FK_Materials_MaterialUnit FOREIGN KEY (DefaultUnitID) REFERENCES MaterialUnit(UnitID);
END
GO

INSERT INTO Materials (MaterialName, DefaultUnitID, Description, IsActive, UserID)
SELECT v.MaterialName, u.UnitID, NULL, 1, 1
FROM (VALUES
    ('Cement', 'Bag'),
    ('Sand', 'Cubic Feet'),
    ('Gravel', 'Cubic Feet'),
    ('Bricks', 'Piece'),
    ('Steel Rods', 'Ton'),
    ('Wood Planks', 'Feet'),
    ('Paint', 'Liter'),
    ('Tiles', 'Square Feet'),
    ('PVC Pipes', 'Feet'),
    ('Electrical Wire', 'Feet')
) v(MaterialName, UnitName)
JOIN MaterialUnit u ON u.UnitName = v.UnitName
WHERE EXISTS (SELECT 1 FROM Users WHERE UserID = 1)
  AND NOT EXISTS (SELECT 1 FROM Materials m WHERE m.MaterialName = v.MaterialName);
GO

IF OBJECT_ID('MaterialPurchases', 'U') IS NULL
BEGIN
    CREATE TABLE MaterialPurchases (
        PurchaseID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProjectID int NOT NULL,
        MaterialID int NOT NULL,
        SupplierID int NULL,
        Quantity decimal(10,3) NOT NULL,
        UnitID tinyint NOT NULL,
        UnitPrice decimal(12,2) NOT NULL,
        TotalCost AS (Quantity * UnitPrice) PERSISTED,
        PurchaseDate date NOT NULL DEFAULT CONVERT(date, GETDATE()),
        InvoiceNumber varchar(50) NULL,
        Notes nvarchar(300) NULL,
        CreatedAt datetime NOT NULL DEFAULT GETDATE()
    );
END
GO

IF COL_LENGTH('MaterialPurchases', 'TotalCost') IS NULL
BEGIN
    ALTER TABLE MaterialPurchases ADD TotalCost AS (Quantity * UnitPrice) PERSISTED;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaterialPurchases_Projects')
BEGIN
    ALTER TABLE MaterialPurchases ADD CONSTRAINT FK_MaterialPurchases_Projects FOREIGN KEY (ProjectID) REFERENCES Projects(ProjectID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaterialPurchases_Materials')
BEGIN
    ALTER TABLE MaterialPurchases ADD CONSTRAINT FK_MaterialPurchases_Materials FOREIGN KEY (MaterialID) REFERENCES Materials(MaterialID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaterialPurchases_Suppliers')
BEGIN
    ALTER TABLE MaterialPurchases ADD CONSTRAINT FK_MaterialPurchases_Suppliers FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaterialPurchases_MaterialUnit')
BEGIN
    ALTER TABLE MaterialPurchases ADD CONSTRAINT FK_MaterialPurchases_MaterialUnit FOREIGN KEY (UnitID) REFERENCES MaterialUnit(UnitID);
END
GO

IF OBJECT_ID('MaterialUsages', 'U') IS NULL
BEGIN
    CREATE TABLE MaterialUsages (
        UsageID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseID int NOT NULL,
        PhaseID int NOT NULL,
        QuantityUsed decimal(10,3) NOT NULL,
        UsageDate date NOT NULL DEFAULT CONVERT(date, GETDATE()),
        Notes nvarchar(300) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaterialUsages_MaterialPurchases')
BEGIN
    ALTER TABLE MaterialUsages ADD CONSTRAINT FK_MaterialUsages_MaterialPurchases FOREIGN KEY (PurchaseID) REFERENCES MaterialPurchases(PurchaseID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaterialUsages_Phases')
BEGIN
    ALTER TABLE MaterialUsages ADD CONSTRAINT FK_MaterialUsages_Phases FOREIGN KEY (PhaseID) REFERENCES Phases(PhaseID);
END
GO

-- Older module tables used by some pages
IF OBJECT_ID('TransactionLogs', 'U') IS NULL
BEGIN
    CREATE TABLE TransactionLogs (
        TransactionId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProjectId int NULL,
        TransactionDate datetime NOT NULL DEFAULT GETDATE(),
        TransactionType nvarchar(50) NOT NULL,
        Category nvarchar(100) NOT NULL,
        Description nvarchar(500) NULL,
        Amount decimal(18,2) NOT NULL,
        BudgetEffect decimal(5,2) NULL
    );
END
GO

IF OBJECT_ID('Workers', 'U') IS NOT NULL AND COL_LENGTH('Workers', 'UserID') IS NULL
BEGIN
    ALTER TABLE Workers ADD UserID int NULL;
END
GO

IF OBJECT_ID('Workers', 'U') IS NOT NULL AND COL_LENGTH('Workers', 'UserID') IS NOT NULL
BEGIN
    EXEC('UPDATE Workers SET UserID = (SELECT TOP 1 UserID FROM Users ORDER BY UserID) WHERE UserID IS NULL');
END
GO

IF OBJECT_ID('Workers', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Workers_Users')
BEGIN
    ALTER TABLE Workers ADD CONSTRAINT FK_Workers_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO

IF OBJECT_ID('Contractors', 'U') IS NOT NULL AND COL_LENGTH('Contractors', 'UserID') IS NULL
BEGIN
    ALTER TABLE Contractors ADD UserID int NULL;
END
GO

IF OBJECT_ID('Contractors', 'U') IS NOT NULL AND COL_LENGTH('Contractors', 'UserID') IS NOT NULL
BEGIN
    EXEC('UPDATE Contractors SET UserID = (SELECT TOP 1 UserID FROM Users ORDER BY UserID) WHERE UserID IS NULL');
END
GO

IF OBJECT_ID('Contractors', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contractors_Users')
BEGIN
    ALTER TABLE Contractors ADD CONSTRAINT FK_Contractors_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO
