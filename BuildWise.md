# BuildWise — Team Handoff Guide

**Project:** Construction Labor & Project Cost Tracker  
**Stack:** ASP.NET Core MVC (.NET 10) + SQL Server + Entity Framework Core  
**DB Name:** BuildWise  

---

## 1. Setup (For Anyone Joining)

### Prerequisites
- .NET 10 SDK — https://dotnet.microsoft.com/download  
- SQL Server or SQL Server Express — https://www.microsoft.com/sql-server  
- SSMS (to restore the database) — https://learn.microsoft.com/en-us/ssms/download  
- Git — https://git-scm.com  
- Visual Studio 2022 (or VS Code + C# extension)  

### Steps

```bash
# 1. Clone the repo
git clone https://github.com/YOUR_USERNAME/BuildWise.git
cd BuildWise

# 2. Restore DB — open BuildWiseDB.sql in SSMS and execute it

# 3. Open appsettings.json and update the server name:
#    "Server=espionge\\SQLEXPRESS" --> change "espionge" to YOUR PC name
#    Common values:  .\\SQLEXPRESS  |  localhost\\SQLEXPRESS  |  .

# 4. Build and run
dotnet restore
dotnet run --urls "http://localhost:5200"
```

### Connection String Location
File: `appsettings.json`
```json
"ConnectionStrings": {
  "BuildWise": "Server=espionge\\SQLEXPRESS;Database=BuildWise;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
Only change the `Server=` part to your machine name. Everything else stays the same.

---

## 2. What's Already Done

| Area | Status |
|------|--------|
| SQL Server Database (BuildWise) | DONE — 19 tables, 9 lookup tables, 7 views, 9 triggers |
| EF Core Models (37 .cs files in /Models) | DONE — auto-generated from DB via scaffold |
| DB Connection in Program.cs | DONE |
| Projects CRUD (Controller + 5 Views) | DONE |
| Workers CRUD (Controller + 5 Views) | DONE |

### Entity summary
Workers, Attendance, WagePayments, Projects, Phases, Tasks, TaskWorkers,  
Expenses, ClientPayments, Materials, MaterialPurchases, MaterialUsages,  
Suppliers, Contractors, Properties, Users, Budget, BudgetAuditLog, ProjectAlerts  

---

## 3. Backend Guide — C# OOP Principles

### How DB Connection Works (Automatic)

EF Core handles everything. The `BuildWiseDbContext` is injected via constructor automatically.
You never `new` it — ASP.NET's DI container manages its lifetime.

```csharp
// In Program.cs (already done)
builder.Services.AddDbContext<BuildWiseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BuildWise")));

// In any Controller or Service — just declare it in constructor:
public class ProjectService : IProjectService
{
    private readonly BuildWiseDbContext _db;
    public ProjectService(BuildWiseDbContext db) { _db = db; }  // DI injects it auto

    public async Task<List<Project>> GetByUserAsync(int userId)
        => await _db.Projects.Where(p => p.UserId == userId).ToListAsync();
}
```

### OOP Principles Used in This Project

#### 1. Abstraction — via Interfaces
Every service has an interface. Controllers depend on the interface, not the class.
This means you can swap implementations without touching the controller.

```csharp
// Define what it does (Interface = contract)
public interface IAttendanceService
{
    Task MarkAttendanceAsync(int workerId, int projectId, DateOnly date, byte statusId);
    Task<List<VwDailyAttendance>> GetDailySheetAsync(int projectId, DateOnly date);
}

// Implement how it does it
public class AttendanceService : IAttendanceService { ... }
```

#### 2. Encapsulation — via Services
DB logic is never in controllers. Business rules live inside service methods, hidden from the outside.

```csharp
// BAD — controller doing DB work directly
public IActionResult Create(Attendance a) { _db.Attendances.Add(a); _db.SaveChanges(); }

// GOOD — controller delegates to service
public async Task<IActionResult> Create(Attendance a)
    => RedirectToAction("Index", await _attendanceService.MarkAttendanceAsync(a));
```

#### 3. Dependency Injection (DI) — Loose Coupling
Register services in `Program.cs`, inject them via constructors. Never use `new`.

```csharp
// Program.cs — register
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

// Controller — receive via constructor (framework injects automatically)
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendance;
    public AttendanceController(IAttendanceService attendance) { _attendance = attendance; }
}
```

#### 4. Inheritance — via Controller base class
All controllers inherit from `Controller`. You get `View()`, `RedirectToAction()`,
`ModelState`, `User.Identity` for free.

```csharp
public class ProjectsController : Controller  // inherits all MVC features
```

#### 5. Polymorphism — via interface usage
Because all services implement interfaces, you can mock them in tests (see testing section).

---

### Folder Structure to Create

```
Services/
  Interfaces/
    IAuthService.cs
    IProjectService.cs
    IAttendanceService.cs
    IExpenseService.cs
    IDashboardService.cs
  AuthService.cs
  ProjectService.cs
  AttendanceService.cs
  ExpenseService.cs
  DashboardService.cs

ViewModels/
  LoginViewModel.cs
  AttendanceBulkViewModel.cs
  DashboardViewModel.cs
```

### Build Order (Do This in Order)

| # | What to Build | Why |
|---|--------------|-----|
| 1 | `AuthService` + Login/Register views | Everything requires knowing who is logged in |
| 2 | `ProjectService` + Dashboard | Core entity — everything links to a project |
| 3 | `AttendanceService` + DailySheet view | The killer feature — bulk daily attendance |
| 4 | `WagePaymentService` | Calculate owed vs paid per worker |
| 5 | `ExpenseService` + Expense views | Material and cost tracking |
| 6 | `DashboardService` | Reads DB views — profit/loss, phase progress |

### Important Business Rules (in Attendance)

```csharp
// Wage calculation logic (put this in AttendanceService)
decimal wage = statusId switch
{
    1 => worker.DailyWage,          // Present
    2 => 0,                          // Absent
    3 => worker.DailyWage / 2,      // Half Day
    4 => 0,                          // Leave
    _ => 0
};
```

### Using DB Views (already in DbContext)

The heavy calculations are done in SQL Views. Just query them:

```csharp
// Profit/loss per project — already calculated in SQL
var dashboard = await _db.VwProjectDashboards
    .Where(v => v.ProjectId == projectId)
    .FirstOrDefaultAsync();

// Worker wage summary
var summary = await _db.VwWorkerWageSummaries
    .Where(v => v.WorkerId == workerId && v.ProjectId == projectId)
    .FirstOrDefaultAsync();
```

---

## 4. Testing Quickly

### Option A — Run and click (no test project needed)

```bash
dotnet run --urls "http://localhost:5200"
# Open http://localhost:5200/Projects
# Open http://localhost:5200/Workers
```

### Option B — Unit Test a Service (proper way)

Install: `dotnet add package Moq` and `dotnet add package xunit`

```csharp
// Because services use interfaces, you can mock the DB context
public class AttendanceServiceTests
{
    [Fact]
    public void HalfDay_Returns_HalfWage()
    {
        // Arrange
        var worker = new Worker { DailyWage = 1000 };

        // Act
        decimal wage = /* your wage calculation method */ ;

        // Assert
        Assert.Equal(500, wage);
    }
}
```

### Option C — Test API endpoints with Swagger

Add Swagger for easy API testing without a frontend:

```bash
dotnet add package Swashbuckle.AspNetCore
```

```csharp
// Program.cs — add these two lines
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// After var app = builder.Build():
app.UseSwagger();
app.UseSwaggerUI();
```

Then open: `http://localhost:5200/swagger`

---

## 5. New Controller + Views Cheat Sheet

```bash
# Scaffold any model to get full CRUD instantly:
dotnet aspnet-codegenerator controller \
  -name ExpensesController \
  -m Expense \
  -dc BuildWiseDbContext \
  --relativeFolderPath Controllers \
  --useDefaultLayout \
  --referenceScriptLibraries
```

Then add its nav link in `Views/Shared/_Layout.cshtml`:
```html
<li class="nav-item">
    <a class="nav-link text-dark" asp-controller="Expenses" asp-action="Index">Expenses</a>
</li>
```

---

## 6. What Still Needs Building

| What | File(s) to Create |
|------|------------------|
| Login / Register | AccountController.cs, Views/Account/Login.cshtml |
| Dashboard | DashboardController.cs, Views/Dashboard/Index.cshtml |
| Daily Attendance Sheet | AttendanceController.cs, Views/Attendance/DailySheet.cshtml |
| Expense Tracking | ExpensesController.cs, Views/Expenses/ |
| Material Purchases | MaterialsController.cs, Views/Materials/ |
| Wage Payments | WagePaymentsController.cs, Views/WagePayments/ |
| All service classes | Services/ folder (create from scratch) |

---

*BuildWise — DB done. Backend + frontend next.*
