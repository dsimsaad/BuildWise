# BuildWise - Developer Guide

**Stack:** ASP.NET Core MVC (.NET 10) + SQL Server + Entity Framework Core

This document covers the architectural patterns, state management, and backend mechanics of the BuildWise application.

---

## 🏛 Architecture & DB Operations

### 1. The Database (`BuildWiseDB`)
The database contains robust relational architecture handling 19 tables, lookup tables, and several SQL Views.
- Heavy aggregations (e.g., project costs, profit/loss, phase progress, worker wage summaries) are pushed down to **SQL Views** (like `vw_ProjectDashboard` and `vw_ExpenseHistory`).
- Entity Framework Core maps these views to models so they can be queried effortlessly via LINQ.

### 2. Entity Framework Core (EF Core)
We use a Database-First approach configured via `BuildWiseDbContext` which handles the dependency injection automatically.
You do not instantiate context objects manually; they are injected into controllers and services via constructors.

---

## 🔐 Authentication & Security

- **Authentication:** Cookie-based local session combined with Firebase (optional/hybrid) configured via `firebase-admin-sdk.json` (also gitignored).
- **Authorization:** `[Authorize]` attributes are applied across all core modules to ensure users can only view their own projects and workers.
- **Tenant Filtering:** Most queries include `p.UserId == userId` to enforce tenant isolation.

---

## 🗂 Project-Centric Workspace (State Management)

BuildWise shifted from a global dashboard to a **Project-Centric Workspace**.

- **Session State:** The active project is tracked via `HttpContext.Session.GetInt32("SelectedProjectId")`.
- **Dropdown Selector:** The top navigation bar contains a selector. When changed, it updates the session state and filters data globally.
- **Dashboard Logic:** If a specific project is selected, the dashboard queries its distinct phases, tasks, and budgets. If "All Projects" is selected, it aggregates high-level metrics for the entire business.

---

## 💻 Best Practices & Conventions

### MVC Pattern
- **Controllers** should remain thin. Heavy logic should be pushed into the `BusinessLayer` (`BLL` classes) or SQL Views.
- **Views** use simple Bootstrap/Vanilla CSS. Avoid complex logic in Razor views.

### Configuration (`appsettings.json`)
Never commit `appsettings.json` to source control. Due to cross-platform compatibility (Mac users utilizing SQL Server Docker containers and Windows users using SSMS), connection strings vary per machine. 
Always use `appsettings.Example.json` as a reference when setting up your local environment.

### Code Generation Cheat Sheet
To quickly scaffold a new CRUD controller using the .NET CLI:
```bash
dotnet aspnet-codegenerator controller \
  -name EntityNameController \
  -m EntityModel \
  -dc BuildWiseDbContext \
  --relativeFolderPath Controllers \
  --useDefaultLayout \
  --referenceScriptLibraries
```

---

## 🗺 Roadmap & Next Steps

1. **Refine "All Projects" View:** Improve the aggregated dashboard charts to show a "Portfolio Overview" when no specific project is selected.
2. **Phase-Level Details:** Drill down further into specific project phases from the main dashboard.
3. **Material Tracking:** Enhance the construction module to link material costs directly to project budget line items.
4. **Project Archive:** Add functionality to "Complete" or "Archive" a project so it moves out of the active dropdown.
