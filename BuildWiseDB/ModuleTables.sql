-- ============================================
-- BuildWise Module Tables
-- Run this script on BuildWiseDB
-- ============================================

-- Module 1: Budget Items
CREATE TABLE BudgetItems (
    BudgetId INT IDENTITY(1,1) PRIMARY KEY,
    Category NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Module 1: Expense Items
CREATE TABLE ExpenseItems (
    ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
    Category NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ExpenseDate DATE NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Module 2: Transaction Log (auto-generated ledger)
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

-- Module 4&5: Construction Phases
CREATE TABLE ConstructionPhases (
    PhaseId INT IDENTITY(1,1) PRIMARY KEY,
    PhaseName NVARCHAR(100) NOT NULL,
    Weight DECIMAL(5,2) NOT NULL DEFAULT 0,
    SortOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Module 4&5: Phase Tasks
CREATE TABLE PhaseTasks (
    TaskId INT IDENTITY(1,1) PRIMARY KEY,
    PhaseId INT NOT NULL,
    TaskName NVARCHAR(200) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    StartDate DATE NULL,
    EndDate DATE NULL,
    Weight DECIMAL(5,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_PhaseTasks_Phase FOREIGN KEY (PhaseId)
        REFERENCES ConstructionPhases(PhaseId) ON DELETE CASCADE
);

-- Default construction phases
INSERT INTO ConstructionPhases (PhaseName, Weight, SortOrder) VALUES
('Foundation', 30, 1),
('Structure', 25, 2),
('Plumbing', 10, 3),
('Electrical', 10, 4),
('Finishing', 15, 5),
('Paint', 10, 6);
