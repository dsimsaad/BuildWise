USE BuildWiseDB;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'UserID' AND Object_ID = Object_ID('Properties'))
BEGIN
    ALTER TABLE Properties ADD UserID int NOT NULL DEFAULT 1;
    ALTER TABLE Properties ADD CONSTRAINT FK_Properties_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'City' AND Object_ID = Object_ID('Users'))
BEGIN
    ALTER TABLE Users ADD City nvarchar(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Profession' AND Object_ID = Object_ID('Users'))
BEGIN
    ALTER TABLE Users ADD Profession nvarchar(100) NULL;
END
GO

IF OBJECT_ID('TransactionLogs', 'U') IS NULL
BEGIN
    CREATE TABLE TransactionLogs (
        TransactionId INT IDENTITY(1,1) PRIMARY KEY,
        ProjectId INT NULL,
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        TransactionType NVARCHAR(50) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        BudgetEffect DECIMAL(5,2) NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'ProjectId' AND Object_ID = Object_ID('TransactionLogs'))
BEGIN
    ALTER TABLE TransactionLogs ADD ProjectId INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'UserID' AND Object_ID = Object_ID('Projects'))
BEGIN
    ALTER TABLE Projects ADD UserID int NOT NULL DEFAULT 1;
    ALTER TABLE Projects ADD CONSTRAINT FK_Projects_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'UserID' AND Object_ID = Object_ID('Workers'))
BEGIN
    ALTER TABLE Workers ADD UserID int NULL;
END
GO

EXEC('UPDATE Workers SET UserID = 1 WHERE UserID IS NULL');
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE Name = 'FK_Workers_Users')
BEGIN
    ALTER TABLE Workers ADD CONSTRAINT FK_Workers_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'UserID' AND Object_ID = Object_ID('Contractors'))
BEGIN
    ALTER TABLE Contractors ADD UserID int NULL;
END
GO

EXEC('UPDATE Contractors SET UserID = 1 WHERE UserID IS NULL');
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE Name = 'FK_Contractors_Users')
BEGIN
    ALTER TABLE Contractors ADD CONSTRAINT FK_Contractors_Users FOREIGN KEY (UserID) REFERENCES Users(UserID);
END
GO
