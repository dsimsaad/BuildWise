USE [master]
GO
CREATE DATABASE [BuildWiseDB]
GO
ALTER DATABASE [BuildWiseDB] SET COMPATIBILITY_LEVEL = 170
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [BuildWiseDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [BuildWiseDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [BuildWiseDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [BuildWiseDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [BuildWiseDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [BuildWiseDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [BuildWiseDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [BuildWiseDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [BuildWiseDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [BuildWiseDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [BuildWiseDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [BuildWiseDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [BuildWiseDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [BuildWiseDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [BuildWiseDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [BuildWiseDB] SET  ENABLE_BROKER 
GO
ALTER DATABASE [BuildWiseDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [BuildWiseDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [BuildWiseDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [BuildWiseDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [BuildWiseDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [BuildWiseDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [BuildWiseDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [BuildWiseDB] SET RECOVERY FULL 
GO
ALTER DATABASE [BuildWiseDB] SET  MULTI_USER 
GO
ALTER DATABASE [BuildWiseDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [BuildWiseDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [BuildWiseDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [BuildWiseDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [BuildWiseDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [BuildWiseDB] SET OPTIMIZED_LOCKING = OFF 
GO
ALTER DATABASE [BuildWiseDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [BuildWiseDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [BuildWiseDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO

USE [BuildWiseDB]
GO
/****** Object:  Table [dbo].[Phases]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Phases](
	[PhaseID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NOT NULL,
	[PhaseTypeID] [tinyint] NOT NULL,
	[CustomPhaseName] [nvarchar](100) NULL,
	[Sequence] [tinyint] NOT NULL,
	[StartDate] [date] NULL,
	[EndDate] [date] NULL,
	[IsCompleted] [bit] NOT NULL,
	[Notes] [nvarchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[PhaseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tasks]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tasks](
	[TaskID] [int] IDENTITY(1,1) NOT NULL,
	[PhaseID] [int] NOT NULL,
	[ContractorID] [int] NULL,
	[TaskName] [nvarchar](150) NOT NULL,
	[Description] [nvarchar](300) NULL,
	[StatusID] [tinyint] NOT NULL,
	[StartDate] [date] NULL,
	[EndDate] [date] NULL,
	[EstimatedCost] [decimal](12, 2) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TaskID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MaterialPurchases]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MaterialPurchases](
	[PurchaseID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NOT NULL,
	[MaterialID] [int] NOT NULL,
	[SupplierID] [int] NULL,
	[Quantity] [decimal](10, 3) NOT NULL,
	[UnitID] [tinyint] NOT NULL,
	[UnitPrice] [decimal](12, 2) NOT NULL,
	[TotalCost]  AS ([Quantity]*[UnitPrice]) PERSISTED,
	[PurchaseDate] [date] NOT NULL,
	[InvoiceNumber] [varchar](50) NULL,
	[Notes] [nvarchar](300) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PurchaseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Expenses]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Expenses](
	[ExpenseID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NOT NULL,
	[PhaseID] [int] NULL,
	[CategoryID] [tinyint] NOT NULL,
	[Description] [nvarchar](300) NOT NULL,
	[Amount] [decimal](12, 2) NOT NULL,
	[ExpenseDate] [date] NOT NULL,
	[PaymentMethodID] [tinyint] NULL,
	[ReceiptURL] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ExpenseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ClientPayments]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ClientPayments](
	[PaymentID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NOT NULL,
	[Amount] [decimal](12, 2) NOT NULL,
	[PaymentDate] [date] NOT NULL,
	[PaymentMethodID] [tinyint] NULL,
	[Description] [nvarchar](300) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PaymentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WagePayments]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WagePayments](
	[WagePaymentID] [int] IDENTITY(1,1) NOT NULL,
	[WorkerID] [int] NOT NULL,
	[ProjectID] [int] NOT NULL,
	[AmountPaid] [decimal](12, 2) NOT NULL,
	[PaymentDate] [date] NOT NULL,
	[PaymentMethodID] [tinyint] NULL,
	[PeriodFrom] [date] NULL,
	[PeriodTo] [date] NULL,
	[Notes] [nvarchar](200) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WagePaymentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[UserID] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Email] [varchar](150) NOT NULL,
	[PasswordHash] [varchar](256) NOT NULL,
	[PhoneNumber] [varchar](20) NULL,
	[City] [nvarchar](100) NULL,
	[Profession] [nvarchar](100) NULL,
	[ProfileImageURL] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Properties]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Properties](
	[PropertyID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[ProjectID] [int] NULL,
	[PropertyName] [nvarchar](150) NOT NULL,
	[TypeID] [tinyint] NOT NULL,
	[StatusID] [tinyint] NOT NULL,
	[Location] [nvarchar](300) NOT NULL,
	[City] [nvarchar](100) NULL,
	[AreaSize] [decimal](10, 4) NOT NULL,
	[AreaUnitID] [tinyint] NOT NULL,
	[Notes] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Projects]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Projects](
	[ProjectID] [int] IDENTITY(1,1) NOT NULL,
	[PropertyID] [int] NOT NULL,
	[UserID] [int] NOT NULL,
	[ProjectName] [nvarchar](150) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[StartDate] [date] NOT NULL,
	[ExpectedEndDate] [date] NULL,
	[ActualEndDate] [date] NULL,
	[TotalBudget] [decimal](14, 2) NOT NULL,
	[IsCompleted] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_ProjectDashboard]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── VIEW 1: Project Dashboard Overview ───────────────────────
-- Used by: Dashboard Page — main summary card
CREATE   VIEW [dbo].[vw_ProjectDashboard] AS
SELECT
    p.ProjectID,
    p.ProjectName,
    pr.PropertyName,
    pr.Location                                             AS PropertyLocation,
    p.StartDate,
    p.ExpectedEndDate,
    p.TotalBudget,

    -- Total spent (expenses + material purchases + wages paid)
    ISNULL(exp.TotalExpenses,   0)                         AS TotalExpenses,
    ISNULL(mat.TotalMaterials,  0)                         AS TotalMaterials,
    ISNULL(wage.TotalWages,     0)                         AS TotalWagesPaid,

    ISNULL(exp.TotalExpenses,0)
        + ISNULL(mat.TotalMaterials,0)
        + ISNULL(wage.TotalWages,0)                        AS TotalSpent,

    p.TotalBudget
        - (ISNULL(exp.TotalExpenses,0)
           + ISNULL(mat.TotalMaterials,0)
           + ISNULL(wage.TotalWages,0))                    AS RemainingBudget,

    -- Client payments received
    ISNULL(cp.TotalReceived,    0)                         AS TotalClientPayments,

    -- Profit/Loss = Received - Spent
    ISNULL(cp.TotalReceived,0)
        - (ISNULL(exp.TotalExpenses,0)
           + ISNULL(mat.TotalMaterials,0)
           + ISNULL(wage.TotalWages,0))                    AS ProfitLoss,

    -- Phase progress
    ph.TotalPhases,
    ph.CompletedPhases,
    CASE WHEN ph.TotalPhases > 0
         THEN CAST(ph.CompletedPhases AS FLOAT) / ph.TotalPhases * 100
         ELSE 0 END                                        AS PhaseProgress_Pct,

    -- Task progress
    tk.TotalTasks,
    tk.CompletedTasks,
    CASE WHEN tk.TotalTasks > 0
         THEN CAST(tk.CompletedTasks AS FLOAT) / tk.TotalTasks * 100
         ELSE 0 END                                        AS TaskProgress_Pct,

    p.IsCompleted,
    u.FullName                                             AS OwnerName
FROM Projects p
JOIN Properties pr ON p.PropertyID = pr.PropertyID
JOIN Users      u  ON p.UserID     = u.UserID

LEFT JOIN (
    SELECT ProjectID, SUM(Amount) AS TotalExpenses
    FROM Expenses
    GROUP BY ProjectID
) exp  ON exp.ProjectID  = p.ProjectID

LEFT JOIN (
    SELECT ProjectID, SUM(TotalCost) AS TotalMaterials
    FROM MaterialPurchases
    GROUP BY ProjectID
) mat  ON mat.ProjectID  = p.ProjectID

LEFT JOIN (
    SELECT ProjectID, SUM(AmountPaid) AS TotalWages
    FROM WagePayments
    GROUP BY ProjectID
) wage ON wage.ProjectID = p.ProjectID

LEFT JOIN (
    SELECT ProjectID, SUM(Amount) AS TotalReceived
    FROM ClientPayments
    GROUP BY ProjectID
) cp   ON cp.ProjectID   = p.ProjectID

LEFT JOIN (
    SELECT ProjectID,
           COUNT(*) AS TotalPhases,
           SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) AS CompletedPhases
    FROM Phases
    GROUP BY ProjectID
) ph   ON ph.ProjectID   = p.ProjectID

LEFT JOIN (
    SELECT ph2.ProjectID,
           COUNT(*) AS TotalTasks,
           SUM(CASE WHEN t.StatusID = 3 THEN 1 ELSE 0 END) AS CompletedTasks
    FROM Tasks t
    JOIN Phases ph2 ON t.PhaseID = ph2.PhaseID
    GROUP BY ph2.ProjectID
) tk   ON tk.ProjectID   = p.ProjectID;
GO
/****** Object:  Table [dbo].[Attendance]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Attendance](
	[AttendanceID] [int] IDENTITY(1,1) NOT NULL,
	[WorkerID] [int] NOT NULL,
	[ProjectID] [int] NOT NULL,
	[AttendanceDate] [date] NOT NULL,
	[StatusID] [tinyint] NOT NULL,
	[WageForDay] [decimal](10, 2) NOT NULL,
	[Notes] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[AttendanceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Workers]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Workers](
	[WorkerID] [int] IDENTITY(1,1) NOT NULL,
	[ContractorID] [int] NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Phone] [varchar](20) NULL,
	[CNIC] [char](15) NULL,
	[DailyWage] [decimal](10, 2) NOT NULL,
	[SkillType] [nvarchar](100) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_WorkerWageSummary]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── VIEW 2: Worker Attendance & Wages Summary ─────────────────
-- Used by: Reports Page — labor cost per worker
CREATE   VIEW [dbo].[vw_WorkerWageSummary] AS
SELECT
    w.WorkerID,
    w.FullName                          AS WorkerName,
    w.SkillType,
    w.DailyWage,
    att.ProjectID,
    p.ProjectName,
    COUNT(CASE WHEN att.StatusID = 1 THEN 1 END) AS DaysPresent,
    COUNT(CASE WHEN att.StatusID = 2 THEN 1 END) AS DaysAbsent,
    COUNT(CASE WHEN att.StatusID = 3 THEN 1 END) AS HalfDays,
    SUM(att.WageForDay)                 AS TotalWageEarned,
    ISNULL(wp.TotalPaid, 0)             AS TotalWagePaid,
    SUM(att.WageForDay) - ISNULL(wp.TotalPaid, 0) AS WageDue
FROM Workers w
JOIN Attendance att ON w.WorkerID = att.WorkerID
JOIN Projects   p   ON att.ProjectID = p.ProjectID
LEFT JOIN (
    SELECT WorkerID, ProjectID, SUM(AmountPaid) AS TotalPaid
    FROM WagePayments
    GROUP BY WorkerID, ProjectID
) wp ON wp.WorkerID = w.WorkerID AND wp.ProjectID = att.ProjectID
GROUP BY w.WorkerID, w.FullName, w.SkillType, w.DailyWage,
         att.ProjectID, p.ProjectName, wp.TotalPaid;
GO
/****** Object:  Table [dbo].[Materials]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Materials](
	[MaterialID] [int] IDENTITY(1,1) NOT NULL,
	[MaterialName] [nvarchar](100) NOT NULL,
	[DefaultUnitID] [tinyint] NOT NULL,
	[Description] [nvarchar](300) NULL,
	[IsActive] [bit] NOT NULL,
	[UserID] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaterialID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MaterialUnit]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MaterialUnit](
	[UnitID] [tinyint] NOT NULL,
	[UnitName] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UnitID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Suppliers]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Suppliers](
	[SupplierID] [int] IDENTITY(1,1) NOT NULL,
	[SupplierName] [nvarchar](100) NOT NULL,
	[ContactPerson] [nvarchar](100) NULL,
	[Phone] [varchar](20) NULL,
	[Email] [varchar](150) NULL,
	[Address] [nvarchar](300) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SupplierID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_MaterialCostByProject]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── VIEW 3: Material Cost Per Project ────────────────────────
-- Used by: Materials Page, Reports
CREATE   VIEW [dbo].[vw_MaterialCostByProject] AS
SELECT
    mp.ProjectID,
    p.ProjectName,
    m.MaterialName,
    SUM(mp.Quantity)   AS TotalQuantityPurchased,
    mu.UnitName,
    SUM(mp.TotalCost)  AS TotalCost,
    s.SupplierName
FROM MaterialPurchases mp
JOIN Projects  p  ON mp.ProjectID  = p.ProjectID
JOIN Materials m  ON mp.MaterialID = m.MaterialID
JOIN MaterialUnit mu ON mp.UnitID  = mu.UnitID
LEFT JOIN Suppliers s ON mp.SupplierID = s.SupplierID
GROUP BY mp.ProjectID, p.ProjectName, m.MaterialName,
         mu.UnitName, s.SupplierName;
GO
/****** Object:  Table [dbo].[MaterialUsages]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MaterialUsages](
	[UsageID] [int] IDENTITY(1,1) NOT NULL,
	[PurchaseID] [int] NOT NULL,
	[PhaseID] [int] NOT NULL,
	[QuantityUsed] [decimal](10, 3) NOT NULL,
	[UsageDate] [date] NOT NULL,
	[Notes] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[UsageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhaseType]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhaseType](
	[PhaseTypeID] [tinyint] NOT NULL,
	[PhaseName] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PhaseTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_PhaseWiseCost]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── VIEW 4: Phase-Wise Cost Breakdown ────────────────────────
-- Used by: Reports Page — phase-wise cost chart
CREATE   VIEW [dbo].[vw_PhaseWiseCost] AS
SELECT
    ph.PhaseID,
    ph.ProjectID,
    p.ProjectName,
    pt.PhaseName,
    ISNULL(ph.CustomPhaseName, pt.PhaseName) AS DisplayPhaseName,
    ph.Sequence,
    ph.IsCompleted,

    ISNULL(exp.PhaseCost,  0) AS ExpenseCost,
    ISNULL(mat.MatCost,    0) AS MaterialCost,
    ISNULL(exp.PhaseCost,0) + ISNULL(mat.MatCost,0) AS TotalPhaseCost,

    -- Task count
    tk.TotalTasks,
    tk.CompletedTasks,
    tk.PendingTasks

FROM Phases ph
JOIN Projects  p  ON ph.ProjectID  = p.ProjectID
JOIN PhaseType pt ON ph.PhaseTypeID = pt.PhaseTypeID

LEFT JOIN (
    SELECT PhaseID, SUM(Amount) AS PhaseCost
    FROM Expenses
    WHERE PhaseID IS NOT NULL
    GROUP BY PhaseID
) exp ON exp.PhaseID = ph.PhaseID

LEFT JOIN (
    SELECT mu.PhaseID, SUM(mp.TotalCost) AS MatCost
    FROM MaterialUsages mu
    JOIN MaterialPurchases mp ON mu.PurchaseID = mp.PurchaseID
    GROUP BY mu.PhaseID
) mat ON mat.PhaseID = ph.PhaseID

LEFT JOIN (
    SELECT PhaseID,
           COUNT(*) AS TotalTasks,
           SUM(CASE WHEN StatusID = 3 THEN 1 ELSE 0 END) AS CompletedTasks,
           SUM(CASE WHEN StatusID = 1 THEN 1 ELSE 0 END) AS PendingTasks
    FROM Tasks
    GROUP BY PhaseID
) tk ON tk.PhaseID = ph.PhaseID;
GO
/****** Object:  Table [dbo].[Contractors]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Contractors](
	[ContractorID] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Phone] [varchar](20) NOT NULL,
	[Email] [varchar](150) NULL,
	[SpecialityNotes] [nvarchar](300) NULL,
	[ContractCost] [decimal](12, 2) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ContractorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_ContractorSummary]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── VIEW 5: Contractor Task & Cost Summary ────────────────────
-- Used by: Contractors Page
CREATE   VIEW [dbo].[vw_ContractorSummary] AS
SELECT
    c.ContractorID,
    c.FullName   AS ContractorName,
    c.Phone,
    c.ContractCost,
    COUNT(DISTINCT t.TaskID)   AS TotalTasksAssigned,
    SUM(CASE WHEN t.StatusID = 3 THEN 1 ELSE 0 END) AS CompletedTasks,
    SUM(CASE WHEN t.StatusID = 2 THEN 1 ELSE 0 END) AS InProgressTasks,
    SUM(CASE WHEN t.StatusID = 1 THEN 1 ELSE 0 END) AS PendingTasks,
    COUNT(DISTINCT w.WorkerID) AS WorkersUnder
FROM Contractors c
LEFT JOIN Tasks   t ON c.ContractorID = t.ContractorID
LEFT JOIN Workers w ON c.ContractorID = w.ContractorID
GROUP BY c.ContractorID, c.FullName, c.Phone, c.ContractCost;
GO
/****** Object:  Table [dbo].[ExpenseCategory]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExpenseCategory](
	[CategoryID] [tinyint] NOT NULL,
	[CategoryName] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CategoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PaymentMethod]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PaymentMethod](
	[MethodID] [tinyint] NOT NULL,
	[MethodName] [varchar](30) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MethodID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_ExpenseHistory]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── VIEW 6: Expense History (full join for expense page) ──────
CREATE   VIEW [dbo].[vw_ExpenseHistory] AS
SELECT
    e.ExpenseID,
    e.ProjectID,
    p.ProjectName,
    ec.CategoryName,
    ph.PhaseID,
    ISNULL(pt.PhaseName, 'N/A')   AS PhaseName,
    e.Description,
    e.Amount,
    e.ExpenseDate,
    pm.MethodName                 AS PaymentMethod,
    e.ReceiptURL,
    e.CreatedAt
FROM Expenses e
JOIN Projects        p  ON e.ProjectID  = p.ProjectID
JOIN ExpenseCategory ec ON e.CategoryID = ec.CategoryID
LEFT JOIN Phases     ph ON e.PhaseID    = ph.PhaseID
LEFT JOIN PhaseType  pt ON ph.PhaseTypeID = pt.PhaseTypeID
LEFT JOIN PaymentMethod pm ON e.PaymentMethodID = pm.MethodID;
GO
/****** Object:  Table [dbo].[AttendanceStatus]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AttendanceStatus](
	[StatusID] [tinyint] NOT NULL,
	[StatusName] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[StatusID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_DailyAttendance]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ── VIEW 7: Daily Attendance Sheet ───────────────────────────
-- Used by: Attendance / Workers Page — day-by-day sheet
CREATE   VIEW [dbo].[vw_DailyAttendance] AS
SELECT
    a.AttendanceDate,
    a.ProjectID,
    p.ProjectName,
    w.WorkerID,
    w.FullName      AS WorkerName,
    w.SkillType,
    ast.StatusName  AS AttendanceStatus,
    a.WageForDay,
    a.Notes
FROM Attendance a
JOIN Workers          w   ON a.WorkerID = w.WorkerID
JOIN Projects         p   ON a.ProjectID = p.ProjectID
JOIN AttendanceStatus ast ON a.StatusID  = ast.StatusID;
GO
/****** Object:  Table [dbo].[AreaUnit]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AreaUnit](
	[UnitID] [tinyint] NOT NULL,
	[UnitName] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UnitID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BudgetAuditLog]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BudgetAuditLog](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[BudgetID] [int] NOT NULL,
	[ProjectID] [int] NOT NULL,
	[OldBudget] [decimal](14, 2) NULL,
	[NewBudget] [decimal](14, 2) NULL,
	[ChangedAt] [datetime2](7) NOT NULL,
	[ChangedByMsg] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Budgets]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Budgets](
	[BudgetID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NOT NULL,
	[TotalBudget] [decimal](14, 2) NOT NULL,
	[LaborBudget] [decimal](14, 2) NULL,
	[MaterialBudget] [decimal](14, 2) NULL,
	[MiscBudget] [decimal](14, 2) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BudgetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProjectAlerts]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProjectAlerts](
	[AlertID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NOT NULL,
	[AlertType] [varchar](50) NOT NULL,
	[AlertMessage] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsRead] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AlertID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PropertyStatus]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PropertyStatus](
	[StatusID] [tinyint] NOT NULL,
	[StatusName] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[StatusID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PropertyType]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PropertyType](
	[TypeID] [tinyint] NOT NULL,
	[TypeName] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskStatus]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskStatus](
	[StatusID] [tinyint] NOT NULL,
	[StatusName] [varchar](30) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[StatusID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskWorkers]    Script Date: 02/05/2026 12:33:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskWorkers](
	[TaskWorkerID] [int] IDENTITY(1,1) NOT NULL,
	[TaskID] [int] NOT NULL,
	[WorkerID] [int] NOT NULL,
	[AssignedDate] [date] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TaskWorkerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[AreaUnit] ([UnitID], [UnitName]) VALUES (2, N'Kanal')
INSERT [dbo].[AreaUnit] ([UnitID], [UnitName]) VALUES (1, N'Marla')
INSERT [dbo].[AreaUnit] ([UnitID], [UnitName]) VALUES (3, N'Square Feet')
INSERT [dbo].[AreaUnit] ([UnitID], [UnitName]) VALUES (4, N'Square Meters')
GO
INSERT [dbo].[AttendanceStatus] ([StatusID], [StatusName]) VALUES (2, N'Absent')
INSERT [dbo].[AttendanceStatus] ([StatusID], [StatusName]) VALUES (3, N'Half Day')
INSERT [dbo].[AttendanceStatus] ([StatusID], [StatusName]) VALUES (4, N'Leave')
INSERT [dbo].[AttendanceStatus] ([StatusID], [StatusName]) VALUES (1, N'Present')
GO
INSERT [dbo].[ExpenseCategory] ([CategoryID], [CategoryName]) VALUES (3, N'Equipment')
INSERT [dbo].[ExpenseCategory] ([CategoryID], [CategoryName]) VALUES (1, N'Labor')
INSERT [dbo].[ExpenseCategory] ([CategoryID], [CategoryName]) VALUES (2, N'Material')
INSERT [dbo].[ExpenseCategory] ([CategoryID], [CategoryName]) VALUES (5, N'Miscellaneous')
INSERT [dbo].[ExpenseCategory] ([CategoryID], [CategoryName]) VALUES (4, N'Transport')
GO
SET IDENTITY_INSERT [dbo].[Materials] ON 

INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (1, N'Cement', 1, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (2, N'Sand', 4, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (3, N'Gravel', 4, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (4, N'Bricks', 2, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (5, N'Steel Rods', 3, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (6, N'Wood Planks', 7, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (7, N'Paint', 5, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (8, N'Tiles', 8, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (9, N'PVC Pipes', 7, NULL, 1)
INSERT [dbo].[Materials] ([MaterialID], [MaterialName], [DefaultUnitID], [Description], [IsActive]) VALUES (10, N'Electrical Wire', 7, NULL, 1)
SET IDENTITY_INSERT [dbo].[Materials] OFF
GO
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (1, N'Bag')
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (6, N'Bundle')
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (4, N'Cubic Feet')
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (7, N'Feet')
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (5, N'Liter')
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (2, N'Piece')
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (8, N'Square Feet')
INSERT [dbo].[MaterialUnit] ([UnitID], [UnitName]) VALUES (3, N'Ton')
GO
INSERT [dbo].[PaymentMethod] ([MethodID], [MethodName]) VALUES (2, N'Bank Transfer')
INSERT [dbo].[PaymentMethod] ([MethodID], [MethodName]) VALUES (1, N'Cash')
INSERT [dbo].[PaymentMethod] ([MethodID], [MethodName]) VALUES (3, N'Cheque')
INSERT [dbo].[PaymentMethod] ([MethodID], [MethodName]) VALUES (4, N'EasyPaisa')
INSERT [dbo].[PaymentMethod] ([MethodID], [MethodName]) VALUES (5, N'JazzCash')
GO
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (8, N'Custom')
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (4, N'Electrical')
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (3, N'Finishing')
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (1, N'Foundation')
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (2, N'Grey Structure')
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (7, N'Painting')
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (5, N'Plumbing')
INSERT [dbo].[PhaseType] ([PhaseTypeID], [PhaseName]) VALUES (6, N'Tiling')
GO
INSERT [dbo].[PropertyStatus] ([StatusID], [StatusName]) VALUES (2, N'Completed')
INSERT [dbo].[PropertyStatus] ([StatusID], [StatusName]) VALUES (3, N'On Hold')
INSERT [dbo].[PropertyStatus] ([StatusID], [StatusName]) VALUES (4, N'Planned')
INSERT [dbo].[PropertyStatus] ([StatusID], [StatusName]) VALUES (1, N'Under Construction')
GO
INSERT [dbo].[PropertyType] ([TypeID], [TypeName]) VALUES (3, N'Apartment')
INSERT [dbo].[PropertyType] ([TypeID], [TypeName]) VALUES (4, N'Commercial')
INSERT [dbo].[PropertyType] ([TypeID], [TypeName]) VALUES (2, N'House')
INSERT [dbo].[PropertyType] ([TypeID], [TypeName]) VALUES (1, N'Plot')
GO
INSERT [dbo].[TaskStatus] ([StatusID], [StatusName]) VALUES (5, N'Cancelled')
INSERT [dbo].[TaskStatus] ([StatusID], [StatusName]) VALUES (3, N'Completed')
INSERT [dbo].[TaskStatus] ([StatusID], [StatusName]) VALUES (2, N'In Progress')
INSERT [dbo].[TaskStatus] ([StatusID], [StatusName]) VALUES (4, N'On Hold')
INSERT [dbo].[TaskStatus] ([StatusID], [StatusName]) VALUES (1, N'Pending')
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__AreaUnit__B5EE667824F8D709]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[AreaUnit] ADD UNIQUE NONCLUSTERED 
(
	[UnitName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_Attendance]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[Attendance] ADD  CONSTRAINT [UQ_Attendance] UNIQUE NONCLUSTERED 
(
	[WorkerID] ASC,
	[ProjectID] ASC,
	[AttendanceDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Attendance_ProjectDate]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Attendance_ProjectDate] ON [dbo].[Attendance]
(
	[ProjectID] ASC,
	[AttendanceDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Attendance_WorkerDate]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Attendance_WorkerDate] ON [dbo].[Attendance]
(
	[WorkerID] ASC,
	[AttendanceDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Attendan__05E7698A52FCB386]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[AttendanceStatus] ADD UNIQUE NONCLUSTERED 
(
	[StatusName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ__Budgets__761ABED17412DCF4]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[Budgets] ADD UNIQUE NONCLUSTERED 
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__ExpenseC__8517B2E0D1B789A1]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[ExpenseCategory] ADD UNIQUE NONCLUSTERED 
(
	[CategoryName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Expenses_Date]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Expenses_Date] ON [dbo].[Expenses]
(
	[ExpenseDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Expenses_PhaseID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Expenses_PhaseID] ON [dbo].[Expenses]
(
	[PhaseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Expenses_ProjectID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Expenses_ProjectID] ON [dbo].[Expenses]
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MatPurchase_ProjectID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_MatPurchase_ProjectID] ON [dbo].[MaterialPurchases]
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Material__9C87053C39999378]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[Materials] ADD UNIQUE NONCLUSTERED 
(
	[MaterialName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Material__B5EE66782D88D194]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[MaterialUnit] ADD UNIQUE NONCLUSTERED 
(
	[UnitName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__PaymentM__218CFB177207F2C1]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[PaymentMethod] ADD UNIQUE NONCLUSTERED 
(
	[MethodName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_Phase_Project_Seq]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[Phases] ADD  CONSTRAINT [UQ_Phase_Project_Seq] UNIQUE NONCLUSTERED 
(
	[ProjectID] ASC,
	[Sequence] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Phases_ProjectID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Phases_ProjectID] ON [dbo].[Phases]
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__PhaseTyp__DB942EE30EF9AF4D]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[PhaseType] ADD UNIQUE NONCLUSTERED 
(
	[PhaseName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Projects_PropertyID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Projects_PropertyID] ON [dbo].[Projects]
(
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Projects_UserID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Projects_UserID] ON [dbo].[Projects]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Properties_UserID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Properties_UserID] ON [dbo].[Properties]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Properties_ProjectID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Properties_ProjectID] ON [dbo].[Properties]
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Property__05E7698AAF0F992C]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[PropertyStatus] ADD UNIQUE NONCLUSTERED 
(
	[StatusName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Property__D4E7DFA8EE59639D]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[PropertyType] ADD UNIQUE NONCLUSTERED 
(
	[TypeName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Tasks_ContractorID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Tasks_ContractorID] ON [dbo].[Tasks]
(
	[ContractorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Tasks_PhaseID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_Tasks_PhaseID] ON [dbo].[Tasks]
(
	[PhaseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__TaskStat__05E7698AD6EAA427]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[TaskStatus] ADD UNIQUE NONCLUSTERED 
(
	[StatusName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_TaskWorker]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[TaskWorkers] ADD  CONSTRAINT [UQ_TaskWorker] UNIQUE NONCLUSTERED 
(
	[TaskID] ASC,
	[WorkerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Users__A9D10534C0CC46CD]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[Users] ADD UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_WagePay_WorkerID]    Script Date: 02/05/2026 12:33:05 AM ******/
CREATE NONCLUSTERED INDEX [IX_WagePay_WorkerID] ON [dbo].[WagePayments]
(
	[WorkerID] ASC,
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Workers__AA570FD4566FD91A]    Script Date: 02/05/2026 12:33:05 AM ******/
ALTER TABLE [dbo].[Workers] ADD UNIQUE NONCLUSTERED 
(
	[CNIC] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Attendance] ADD  DEFAULT ((1)) FOR [StatusID]
GO
ALTER TABLE [dbo].[Attendance] ADD  DEFAULT ((0)) FOR [WageForDay]
GO
ALTER TABLE [dbo].[BudgetAuditLog] ADD  DEFAULT (getdate()) FOR [ChangedAt]
GO
ALTER TABLE [dbo].[BudgetAuditLog] ADD  DEFAULT ('System') FOR [ChangedByMsg]
GO
ALTER TABLE [dbo].[Budgets] ADD  DEFAULT ((0)) FOR [LaborBudget]
GO
ALTER TABLE [dbo].[Budgets] ADD  DEFAULT ((0)) FOR [MaterialBudget]
GO
ALTER TABLE [dbo].[Budgets] ADD  DEFAULT ((0)) FOR [MiscBudget]
GO
ALTER TABLE [dbo].[Budgets] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Budgets] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[ClientPayments] ADD  DEFAULT (CONVERT([date],getdate())) FOR [PaymentDate]
GO
ALTER TABLE [dbo].[ClientPayments] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Contractors] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Contractors] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Expenses] ADD  DEFAULT (CONVERT([date],getdate())) FOR [ExpenseDate]
GO
ALTER TABLE [dbo].[Expenses] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[MaterialPurchases] ADD  DEFAULT (CONVERT([date],getdate())) FOR [PurchaseDate]
GO
ALTER TABLE [dbo].[MaterialPurchases] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Materials] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[MaterialUsages] ADD  DEFAULT (CONVERT([date],getdate())) FOR [UsageDate]
GO
ALTER TABLE [dbo].[Phases] ADD  DEFAULT ((1)) FOR [Sequence]
GO
ALTER TABLE [dbo].[Phases] ADD  DEFAULT ((0)) FOR [IsCompleted]
GO
ALTER TABLE [dbo].[ProjectAlerts] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ProjectAlerts] ADD  DEFAULT ((0)) FOR [IsRead]
GO
ALTER TABLE [dbo].[Projects] ADD  DEFAULT ((0)) FOR [TotalBudget]
GO
ALTER TABLE [dbo].[Projects] ADD  DEFAULT ((0)) FOR [IsCompleted]
GO
ALTER TABLE [dbo].[Projects] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Projects] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[Properties] ADD  DEFAULT ((1)) FOR [StatusID]
GO
ALTER TABLE [dbo].[Properties] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Properties] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[Suppliers] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Suppliers] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Tasks] ADD  DEFAULT ((1)) FOR [StatusID]
GO
ALTER TABLE [dbo].[Tasks] ADD  DEFAULT ((0)) FOR [EstimatedCost]
GO
ALTER TABLE [dbo].[Tasks] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Tasks] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[TaskWorkers] ADD  DEFAULT (CONVERT([date],getdate())) FOR [AssignedDate]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[WagePayments] ADD  DEFAULT (CONVERT([date],getdate())) FOR [PaymentDate]
GO
ALTER TABLE [dbo].[WagePayments] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Workers] ADD  DEFAULT ((0)) FOR [DailyWage]
GO
ALTER TABLE [dbo].[Workers] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Workers] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Attendance]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[Attendance]  WITH CHECK ADD FOREIGN KEY([StatusID])
REFERENCES [dbo].[AttendanceStatus] ([StatusID])
GO
ALTER TABLE [dbo].[Attendance]  WITH CHECK ADD FOREIGN KEY([WorkerID])
REFERENCES [dbo].[Workers] ([WorkerID])
GO
ALTER TABLE [dbo].[Budgets]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[ClientPayments]  WITH CHECK ADD FOREIGN KEY([PaymentMethodID])
REFERENCES [dbo].[PaymentMethod] ([MethodID])
GO
ALTER TABLE [dbo].[ClientPayments]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD FOREIGN KEY([CategoryID])
REFERENCES [dbo].[ExpenseCategory] ([CategoryID])
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD FOREIGN KEY([PaymentMethodID])
REFERENCES [dbo].[PaymentMethod] ([MethodID])
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD FOREIGN KEY([PhaseID])
REFERENCES [dbo].[Phases] ([PhaseID])
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[MaterialPurchases]  WITH CHECK ADD FOREIGN KEY([MaterialID])
REFERENCES [dbo].[Materials] ([MaterialID])
GO
ALTER TABLE [dbo].[MaterialPurchases]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[MaterialPurchases]  WITH CHECK ADD FOREIGN KEY([SupplierID])
REFERENCES [dbo].[Suppliers] ([SupplierID])
GO
ALTER TABLE [dbo].[MaterialPurchases]  WITH CHECK ADD FOREIGN KEY([UnitID])
REFERENCES [dbo].[MaterialUnit] ([UnitID])
GO
ALTER TABLE [dbo].[Materials]  WITH CHECK ADD FOREIGN KEY([DefaultUnitID])
REFERENCES [dbo].[MaterialUnit] ([UnitID])
GO
ALTER TABLE [dbo].[Materials]  WITH CHECK ADD CONSTRAINT [FK_Materials_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[MaterialUsages]  WITH CHECK ADD FOREIGN KEY([PhaseID])
REFERENCES [dbo].[Phases] ([PhaseID])
GO
ALTER TABLE [dbo].[MaterialUsages]  WITH CHECK ADD FOREIGN KEY([PurchaseID])
REFERENCES [dbo].[MaterialPurchases] ([PurchaseID])
GO
ALTER TABLE [dbo].[Phases]  WITH CHECK ADD FOREIGN KEY([PhaseTypeID])
REFERENCES [dbo].[PhaseType] ([PhaseTypeID])
GO
ALTER TABLE [dbo].[Phases]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ProjectAlerts]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[Projects]  WITH CHECK ADD FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Properties] ([PropertyID])
GO
ALTER TABLE [dbo].[Projects]  WITH CHECK ADD FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[Properties]  WITH CHECK ADD FOREIGN KEY([AreaUnitID])
REFERENCES [dbo].[AreaUnit] ([UnitID])
GO
ALTER TABLE [dbo].[Properties]  WITH CHECK ADD CONSTRAINT [FK_Properties_Projects_ProjectID] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[Properties]  WITH CHECK ADD FOREIGN KEY([StatusID])
REFERENCES [dbo].[PropertyStatus] ([StatusID])
GO
ALTER TABLE [dbo].[Properties]  WITH CHECK ADD FOREIGN KEY([TypeID])
REFERENCES [dbo].[PropertyType] ([TypeID])
GO
ALTER TABLE [dbo].[Properties]  WITH CHECK ADD FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[Tasks]  WITH CHECK ADD FOREIGN KEY([ContractorID])
REFERENCES [dbo].[Contractors] ([ContractorID])
GO
ALTER TABLE [dbo].[Tasks]  WITH CHECK ADD FOREIGN KEY([PhaseID])
REFERENCES [dbo].[Phases] ([PhaseID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Tasks]  WITH CHECK ADD FOREIGN KEY([StatusID])
REFERENCES [dbo].[TaskStatus] ([StatusID])
GO
ALTER TABLE [dbo].[TaskWorkers]  WITH CHECK ADD FOREIGN KEY([TaskID])
REFERENCES [dbo].[Tasks] ([TaskID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TaskWorkers]  WITH CHECK ADD FOREIGN KEY([WorkerID])
REFERENCES [dbo].[Workers] ([WorkerID])
GO
ALTER TABLE [dbo].[WagePayments]  WITH CHECK ADD FOREIGN KEY([PaymentMethodID])
REFERENCES [dbo].[PaymentMethod] ([MethodID])
GO
ALTER TABLE [dbo].[WagePayments]  WITH CHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
GO
ALTER TABLE [dbo].[WagePayments]  WITH CHECK ADD FOREIGN KEY([WorkerID])
REFERENCES [dbo].[Workers] ([WorkerID])
GO
ALTER TABLE [dbo].[Workers]  WITH CHECK ADD FOREIGN KEY([ContractorID])
REFERENCES [dbo].[Contractors] ([ContractorID])
GO
/****** Object:  StoredProcedure [dbo].[AddColDesc]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Helper: adds description to a column
-- Usage: EXEC AddColDesc 'TableName', 'ColumnName', 'Description'

CREATE   PROCEDURE [dbo].[AddColDesc]
    @table NVARCHAR(128),
    @col   NVARCHAR(128),
    @desc  NVARCHAR(1000)
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.extended_properties
        WHERE major_id = OBJECT_ID(@table)
          AND minor_id = (
              SELECT column_id FROM sys.columns
              WHERE object_id = OBJECT_ID(@table) AND name = @col)
          AND name = 'MS_Description')
        EXEC sys.sp_dropextendedproperty
            'MS_Description', 'SCHEMA', 'dbo', 'TABLE', @table, 'COLUMN', @col;

    EXEC sys.sp_addextendedproperty
        'MS_Description', @desc,
        'SCHEMA', 'dbo', 'TABLE', @table, 'COLUMN', @col;
END
GO
/****** Object:  StoredProcedure [dbo].[usp_AddExpense]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 7: usp_AddExpense  (CRUD helper)
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_AddExpense]
    @ProjectID      INT,
    @PhaseID        INT           = NULL,
    @CategoryID     TINYINT,
    @Description    NVARCHAR(300),
    @Amount         DECIMAL(12,2),
    @ExpenseDate    DATE          = NULL,
    @PaymentMethod  TINYINT       = NULL,
    @ReceiptURL     NVARCHAR(500) = NULL,
    @NewExpenseID   INT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ExpenseDate = ISNULL(@ExpenseDate, CAST(GETDATE() AS DATE));

    INSERT INTO Expenses (ProjectID, PhaseID, CategoryID, Description,
                          Amount, ExpenseDate, PaymentMethodID, ReceiptURL)
    VALUES (@ProjectID, @PhaseID, @CategoryID, @Description,
            @Amount, @ExpenseDate, @PaymentMethod, @ReceiptURL);

    SET @NewExpenseID = SCOPE_IDENTITY();
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_AddMaterialPurchase]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 8: usp_AddMaterialPurchase
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_AddMaterialPurchase]
    @ProjectID      INT,
    @MaterialID     INT,
    @SupplierID     INT           = NULL,
    @Quantity       DECIMAL(10,3),
    @UnitID         TINYINT,
    @UnitPrice      DECIMAL(12,2),
    @PurchaseDate   DATE          = NULL,
    @InvoiceNumber  VARCHAR(50)   = NULL,
    @Notes          NVARCHAR(300) = NULL,
    @NewPurchaseID  INT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @PurchaseDate = ISNULL(@PurchaseDate, CAST(GETDATE() AS DATE));

    INSERT INTO MaterialPurchases (ProjectID, MaterialID, SupplierID, Quantity,
                                   UnitID, UnitPrice, PurchaseDate,
                                   InvoiceNumber, Notes)
    VALUES (@ProjectID, @MaterialID, @SupplierID, @Quantity,
            @UnitID, @UnitPrice, @PurchaseDate, @InvoiceNumber, @Notes);

    SET @NewPurchaseID = SCOPE_IDENTITY();
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_CalculateProgress]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 5: usp_CalculateProgress
--  Returns % completion based on tasks
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_CalculateProgress]
    @ProjectID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ph.PhaseID,
        ISNULL(pt.PhaseName, ph.CustomPhaseName) AS PhaseName,
        ph.Sequence,
        ph.IsCompleted,
        COUNT(t.TaskID)                           AS TotalTasks,
        SUM(CASE WHEN t.StatusID = 3 THEN 1 ELSE 0 END) AS CompletedTasks,
        SUM(CASE WHEN t.StatusID = 2 THEN 1 ELSE 0 END) AS InProgressTasks,
        SUM(CASE WHEN t.StatusID = 1 THEN 1 ELSE 0 END) AS PendingTasks,
        CASE WHEN COUNT(t.TaskID) > 0
             THEN CAST(
                SUM(CASE WHEN t.StatusID = 3 THEN 1.0 ELSE 0 END)
                / COUNT(t.TaskID) * 100 AS DECIMAL(5,2))
             ELSE 0
        END AS PhaseCompletion_Pct
    FROM Phases ph
    JOIN PhaseType pt ON ph.PhaseTypeID = pt.PhaseTypeID
    LEFT JOIN Tasks t ON ph.PhaseID = t.PhaseID
    WHERE ph.ProjectID = @ProjectID
    GROUP BY ph.PhaseID, pt.PhaseName, ph.CustomPhaseName,
             ph.Sequence, ph.IsCompleted
    ORDER BY ph.Sequence;
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_CalculateTotalExpenses]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 3: usp_CalculateTotalExpenses
--  Returns all cost components for a project
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_CalculateTotalExpenses]
    @ProjectID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        @ProjectID                                   AS ProjectID,
        ISNULL(e.GeneralExpenses, 0)                 AS GeneralExpenses,
        ISNULL(m.MaterialCost, 0)                    AS MaterialCost,
        ISNULL(w.WagesPaid, 0)                       AS WagesPaid,
        ISNULL(e.GeneralExpenses,0)
            + ISNULL(m.MaterialCost,0)
            + ISNULL(w.WagesPaid,0)                  AS TotalSpent,
        b.TotalBudget,
        b.TotalBudget
            - (ISNULL(e.GeneralExpenses,0)
               + ISNULL(m.MaterialCost,0)
               + ISNULL(w.WagesPaid,0))              AS RemainingBudget,
        CASE
            WHEN b.TotalBudget > 0
            THEN CAST(
                (ISNULL(e.GeneralExpenses,0)
                 + ISNULL(m.MaterialCost,0)
                 + ISNULL(w.WagesPaid,0))
                / b.TotalBudget * 100 AS DECIMAL(5,2))
            ELSE 0
        END                                          AS BudgetUsedPercent

    FROM Budgets b

    LEFT JOIN (
        SELECT ProjectID, SUM(Amount) AS GeneralExpenses
        FROM Expenses WHERE ProjectID = @ProjectID
        GROUP BY ProjectID
    ) e ON e.ProjectID = @ProjectID

    LEFT JOIN (
        SELECT ProjectID, SUM(TotalCost) AS MaterialCost
        FROM MaterialPurchases WHERE ProjectID = @ProjectID
        GROUP BY ProjectID
    ) m ON m.ProjectID = @ProjectID

    LEFT JOIN (
        SELECT ProjectID, SUM(AmountPaid) AS WagesPaid
        FROM WagePayments WHERE ProjectID = @ProjectID
        GROUP BY ProjectID
    ) w ON w.ProjectID = @ProjectID

    WHERE b.ProjectID = @ProjectID;
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_CheckBudgetStatus]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 4: usp_CheckBudgetStatus
--  Returns budget alert level for a project
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_CheckBudgetStatus]
    @ProjectID INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalBudget DECIMAL(14,2);
    DECLARE @TotalSpent  DECIMAL(14,2);
    DECLARE @UsedPct     DECIMAL(5,2);

    SELECT @TotalBudget = TotalBudget FROM Budgets WHERE ProjectID = @ProjectID;

    SELECT @TotalSpent =
        ISNULL((SELECT SUM(Amount)    FROM Expenses          WHERE ProjectID = @ProjectID), 0)
      + ISNULL((SELECT SUM(TotalCost) FROM MaterialPurchases WHERE ProjectID = @ProjectID), 0)
      + ISNULL((SELECT SUM(AmountPaid)FROM WagePayments      WHERE ProjectID = @ProjectID), 0);

    SET @UsedPct = CASE WHEN @TotalBudget > 0
                        THEN @TotalSpent / @TotalBudget * 100
                        ELSE 0 END;

    SELECT
        @ProjectID   AS ProjectID,
        @TotalBudget AS TotalBudget,
        @TotalSpent  AS TotalSpent,
        @TotalBudget - @TotalSpent AS RemainingBudget,
        @UsedPct     AS UsedPercent,
        CASE
            WHEN @UsedPct >= 100 THEN 'OVER_BUDGET'
            WHEN @UsedPct >= 90  THEN 'CRITICAL'
            WHEN @UsedPct >= 75  THEN 'WARNING'
            ELSE                      'OK'
        END          AS BudgetStatus;
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_CreateProject]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 1: usp_CreateProject
--  Creates a project + its default budget in one transaction
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_CreateProject]
    @PropertyID      INT,
    @UserID          INT,
    @ProjectName     NVARCHAR(150),
    @Description     NVARCHAR(500) = NULL,
    @StartDate       DATE,
    @ExpectedEndDate DATE          = NULL,
    @TotalBudget     DECIMAL(14,2),
    @LaborBudget     DECIMAL(14,2) = 0,
    @MaterialBudget  DECIMAL(14,2) = 0,
    @MiscBudget      DECIMAL(14,2) = 0,
    @NewProjectID    INT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Projects (PropertyID, UserID, ProjectName, Description,
                              StartDate, ExpectedEndDate, TotalBudget)
        VALUES (@PropertyID, @UserID, @ProjectName, @Description,
                @StartDate, @ExpectedEndDate, @TotalBudget);

        SET @NewProjectID = SCOPE_IDENTITY();

        INSERT INTO Budgets (ProjectID, TotalBudget, LaborBudget,
                             MaterialBudget, MiscBudget)
        VALUES (@NewProjectID, @TotalBudget, @LaborBudget,
                @MaterialBudget, @MiscBudget);

        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_DailyHealthCheck]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[usp_DailyHealthCheck]
AS
BEGIN
    SET NOCOUNT ON;

    -- Clear old unread alerts older than 7 days
    DELETE FROM ProjectAlerts WHERE CreatedAt < DATEADD(DAY, -7, GETDATE());

    -- Flag overdue projects
    INSERT INTO ProjectAlerts (ProjectID, AlertType, AlertMessage)
    SELECT ProjectID,
           'OVERDUE',
           'Project "' + ProjectName + '" is past its expected end date.'
    FROM Projects
    WHERE IsCompleted = 0
      AND ExpectedEndDate < CAST(GETDATE() AS DATE)
      AND ProjectID NOT IN (
          SELECT ProjectID FROM ProjectAlerts
          WHERE AlertType = 'OVERDUE'
            AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE));

    -- Flag budget-critical projects (>90% spent)
    INSERT INTO ProjectAlerts (ProjectID, AlertType, AlertMessage)
    SELECT
        p.ProjectID,
        'BUDGET_CRITICAL',
        'Project "' + p.ProjectName + '" has used over 90% of its budget.'
    FROM Projects p
    JOIN Budgets  b ON p.ProjectID = b.ProjectID
    WHERE p.IsCompleted = 0
      AND b.TotalBudget > 0
      AND (
          ISNULL((SELECT SUM(Amount)    FROM Expenses          WHERE ProjectID = p.ProjectID),0)
        + ISNULL((SELECT SUM(TotalCost) FROM MaterialPurchases WHERE ProjectID = p.ProjectID),0)
        + ISNULL((SELECT SUM(AmountPaid)FROM WagePayments      WHERE ProjectID = p.ProjectID),0)
      ) / b.TotalBudget * 100 >= 90
      AND p.ProjectID NOT IN (
          SELECT ProjectID FROM ProjectAlerts
          WHERE AlertType = 'BUDGET_CRITICAL'
            AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE));

    PRINT 'Daily health check complete.';
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_GetProjectReport]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 9: usp_GetProjectReport  (full summary for Reports Page)
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_GetProjectReport]
    @ProjectID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Overall summary
    SELECT * FROM vw_ProjectDashboard WHERE ProjectID = @ProjectID;

    -- Phase breakdown
    SELECT * FROM vw_PhaseWiseCost    WHERE ProjectID = @ProjectID
    ORDER BY Sequence;

    -- Worker summary
    SELECT * FROM vw_WorkerWageSummary WHERE ProjectID = @ProjectID;

    -- Material cost
    SELECT * FROM vw_MaterialCostByProject WHERE ProjectID = @ProjectID;
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_GetWeeklyAttendanceReport]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 6: usp_GetWeeklyAttendanceReport
--  Used by backend to build weekly payroll report
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_GetWeeklyAttendanceReport]
    @ProjectID  INT,
    @WeekStart  DATE,
    @WeekEnd    DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        w.WorkerID,
        w.FullName,
        w.SkillType,
        w.DailyWage,
        COUNT(CASE WHEN a.StatusID = 1 THEN 1 END) AS DaysPresent,
        COUNT(CASE WHEN a.StatusID = 2 THEN 1 END) AS DaysAbsent,
        COUNT(CASE WHEN a.StatusID = 3 THEN 1 END) AS HalfDays,
        SUM(a.WageForDay) AS TotalWageThisWeek,
        ISNULL(paid.PaidThisWeek, 0) AS PaidThisWeek,
        SUM(a.WageForDay) - ISNULL(paid.PaidThisWeek, 0) AS BalanceDue
    FROM Workers w
    JOIN Attendance a ON w.WorkerID = a.WorkerID
        AND a.ProjectID = @ProjectID
        AND a.AttendanceDate BETWEEN @WeekStart AND @WeekEnd
    LEFT JOIN (
        SELECT WorkerID, SUM(AmountPaid) AS PaidThisWeek
        FROM WagePayments
        WHERE ProjectID = @ProjectID
          AND PaymentDate BETWEEN @WeekStart AND @WeekEnd
        GROUP BY WorkerID
    ) paid ON paid.WorkerID = w.WorkerID
    GROUP BY w.WorkerID, w.FullName, w.SkillType, w.DailyWage, paid.PaidThisWeek
    ORDER BY w.FullName;
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_MarkAttendance]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 2: usp_MarkAttendance
--  Marks attendance for a worker and auto-calculates wage
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_MarkAttendance]
    @WorkerID       INT,
    @ProjectID      INT,
    @AttendanceDate DATE,
    @StatusID       TINYINT,   -- 1=Present, 2=Absent, 3=Half Day, 4=Leave
    @Notes          NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DailyWage   DECIMAL(10,2);
    DECLARE @WageForDay  DECIMAL(10,2);

    SELECT @DailyWage = DailyWage FROM Workers WHERE WorkerID = @WorkerID;

    SET @WageForDay = CASE @StatusID
        WHEN 1 THEN @DailyWage            -- Present: full wage
        WHEN 3 THEN @DailyWage * 0.5      -- Half Day: 50%
        ELSE 0                             -- Absent/Leave: 0
    END;

    -- UPSERT pattern (update if already marked, insert otherwise)
    IF EXISTS (
        SELECT 1 FROM Attendance
        WHERE WorkerID = @WorkerID AND ProjectID = @ProjectID
          AND AttendanceDate = @AttendanceDate)
    BEGIN
        UPDATE Attendance
        SET StatusID = @StatusID, WageForDay = @WageForDay, Notes = @Notes
        WHERE WorkerID = @WorkerID AND ProjectID = @ProjectID
          AND AttendanceDate = @AttendanceDate;
    END
    ELSE
    BEGIN
        INSERT INTO Attendance (WorkerID, ProjectID, AttendanceDate,
                                StatusID, WageForDay, Notes)
        VALUES (@WorkerID, @ProjectID, @AttendanceDate,
                @StatusID, @WageForDay, @Notes);
    END;
END;
GO
/****** Object:  StoredProcedure [dbo].[usp_PayWorkerWage]    Script Date: 02/05/2026 12:33:05 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ══════════════════════════════════════════════════════════════
--  SP 10: usp_PayWorkerWage
-- ══════════════════════════════════════════════════════════════
CREATE   PROCEDURE [dbo].[usp_PayWorkerWage]
    @WorkerID       INT,
    @ProjectID      INT,
    @AmountPaid     DECIMAL(12,2),
    @PaymentMethod  TINYINT       = 1,   -- default: Cash
    @PeriodFrom     DATE          = NULL,
    @PeriodTo       DATE          = NULL,
    @Notes          NVARCHAR(200) = NULL,
    @NewPaymentID   INT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO WagePayments (WorkerID, ProjectID, AmountPaid,
                              PaymentMethodID, PeriodFrom, PeriodTo, Notes)
    VALUES (@WorkerID, @ProjectID, @AmountPaid,
            @PaymentMethod, @PeriodFrom, @PeriodTo, @Notes);

    SET @NewPaymentID = SCOPE_IDENTITY();
END;
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Attendance', @level2type=N'COLUMN',@level2name=N'AttendanceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Workers' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Attendance', @level2type=N'COLUMN',@level2name=N'WorkerID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Projects — which site they attended' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Attendance', @level2type=N'COLUMN',@level2name=N'ProjectID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to AttendanceStatus: Present/Absent/Half Day/Leave' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Attendance', @level2type=N'COLUMN',@level2name=N'StatusID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Actual wage paid for this day (0 if absent, 50% if half day)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Attendance', @level2type=N'COLUMN',@level2name=N'WageForDay'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK — one budget record per project' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Budgets', @level2type=N'COLUMN',@level2name=N'BudgetID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Portion of budget allocated to labor costs' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Budgets', @level2type=N'COLUMN',@level2name=N'LaborBudget'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Portion allocated to materials' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Budgets', @level2type=N'COLUMN',@level2name=N'MaterialBudget'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Remaining allocation for equipment, transport, misc' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Budgets', @level2type=N'COLUMN',@level2name=N'MiscBudget'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ClientPayments', @level2type=N'COLUMN',@level2name=N'PaymentID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Money received from client in PKR' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ClientPayments', @level2type=N'COLUMN',@level2name=N'Amount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Expenses', @level2type=N'COLUMN',@level2name=N'ExpenseID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to ExpenseCategory: Labor/Material/Equipment/Transport/Misc' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Expenses', @level2type=N'COLUMN',@level2name=N'CategoryID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Path/URL to uploaded receipt image for proof' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Expenses', @level2type=N'COLUMN',@level2name=N'ReceiptURL'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'MaterialPurchases', @level2type=N'COLUMN',@level2name=N'PurchaseID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Computed column: Quantity × UnitPrice, persisted' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'MaterialPurchases', @level2type=N'COLUMN',@level2name=N'TotalCost'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Supplier invoice reference for audit trail' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'MaterialPurchases', @level2type=N'COLUMN',@level2name=N'InvoiceNumber'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Phases', @level2type=N'COLUMN',@level2name=N'PhaseID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to PhaseType — Foundation, Grey Structure, Finishing etc' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Phases', @level2type=N'COLUMN',@level2name=N'PhaseTypeID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Used only when PhaseTypeID = 8 (Custom)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Phases', @level2type=N'COLUMN',@level2name=N'CustomPhaseName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Ordering of phases within the project (1 = first)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Phases', @level2type=N'COLUMN',@level2name=N'Sequence'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Projects', @level2type=N'COLUMN',@level2name=N'ProjectID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Properties — which property this project is on' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Projects', @level2type=N'COLUMN',@level2name=N'PropertyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Filled when project is marked complete' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Projects', @level2type=N'COLUMN',@level2name=N'ActualEndDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Overall approved budget in PKR' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Projects', @level2type=N'COLUMN',@level2name=N'TotalBudget'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'1 = project closed, 0 = ongoing' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Projects', @level2type=N'COLUMN',@level2name=N'IsCompleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Properties', @level2type=N'COLUMN',@level2name=N'PropertyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Users — owner of this property' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Properties', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to PropertyType: Plot/House/Apartment/Commercial' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Properties', @level2type=N'COLUMN',@level2name=N'TypeID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to PropertyStatus: Under Construction/Completed etc' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Properties', @level2type=N'COLUMN',@level2name=N'StatusID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Numeric area value, unit determined by AreaUnitID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Properties', @level2type=N'COLUMN',@level2name=N'AreaSize'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to AreaUnit: Marla/Kanal/SqFt/SqM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Properties', @level2type=N'COLUMN',@level2name=N'AreaUnitID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tasks', @level2type=N'COLUMN',@level2name=N'TaskID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Phases — task belongs to this phase' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tasks', @level2type=N'COLUMN',@level2name=N'PhaseID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Contractors — nullable, who is responsible' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tasks', @level2type=N'COLUMN',@level2name=N'ContractorID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to TaskStatus: Pending/In Progress/Completed/Hold/Cancelled' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tasks', @level2type=N'COLUMN',@level2name=N'StatusID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Budgeted cost for this specific task in PKR' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tasks', @level2type=N'COLUMN',@level2name=N'EstimatedCost'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment primary key for user accounts' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Users', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Full display name of the owner/user' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Users', @level2type=N'COLUMN',@level2name=N'FullName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique login email; used as username' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Users', @level2type=N'COLUMN',@level2name=N'Email'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'BCrypt hashed password — never store plain text' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Users', @level2type=N'COLUMN',@level2name=N'PasswordHash'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional contact number' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Users', @level2type=N'COLUMN',@level2name=N'PhoneNumber'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'1 = active account, 0 = soft-deleted' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Users', @level2type=N'COLUMN',@level2name=N'IsActive'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'WagePayments', @level2type=N'COLUMN',@level2name=N'WagePaymentID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Start date of the pay period being settled' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'WagePayments', @level2type=N'COLUMN',@level2name=N'PeriodFrom'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'End date of the pay period being settled' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'WagePayments', @level2type=N'COLUMN',@level2name=N'PeriodTo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Auto-increment PK' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Workers', @level2type=N'COLUMN',@level2name=N'WorkerID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Contractors — null if independent worker' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Workers', @level2type=N'COLUMN',@level2name=N'ContractorID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Pakistani CNIC number (13 digits + dashes), unique identifier' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Workers', @level2type=N'COLUMN',@level2name=N'CNIC'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Default daily wage in PKR, can be overridden per attendance record' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Workers', @level2type=N'COLUMN',@level2name=N'DailyWage'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'e.g. Mason, Carpenter, Electrician, Helper, Plumber' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Workers', @level2type=N'COLUMN',@level2name=N'SkillType'
GO
USE [master]
GO
ALTER DATABASE [BuildWiseDB] SET  READ_WRITE 
GO
USE BuildWiseDB;
GO
