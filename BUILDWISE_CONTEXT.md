# BuildWise - Project Status & Roadmap

## 🚀 Overview
BuildWise is a Project Management & Cost Analysis platform for construction. It has recently transitioned from a global view to a **Project-Centric Workspace**.

## 🛠 Tech Stack
- **Framework:** ASP.NET Core 8.0 MVC
- **Database:** SQL Server (EF Core)
- **Auth:** Firebase Authentication (Cookie-based local session)
- **State:** ASP.NET Session (`SelectedProjectId`)
- **Frontend:** Vanilla CSS, Chart.js, minimalist layout.

## 📍 Current State
1. **Project Switching:** Users select an "Active Project" via a top-nav dropdown. The `SelectedProjectId` is stored in the Session.
2. **Dynamic Dashboard:** The dashboard filters all metrics (Expenses, Wages, Progress) based on the active project. If "All Projects" is selected, it shows aggregated business totals.
3. **UI/UX Cleanup:** 
   - Minimalist sidebar (Logout moved to profile).
   - Minimalist topbar (Search/Bell removed).
   - Profile Hover Menu (Contains Logout).
   - "Add New Project" integrated into the Project Selector.
4. **Security:** `[Authorize]` attributes are applied to all core modules (Workers, Projects, Budget, etc.).

## 🔐 Configuration (IMPORTANT)
- **appsettings.json:** Ignored by Git. Use **Windows Authentication** locally.
- **appsettings.Example.json:** Use this as a template for new team members.
- **Firebase:** Configured via `firebase-admin-sdk.json` (also gitignored).

## 🗺 Roadmap / Next Steps
1. **Refine "All Projects" View:** Improve the aggregated dashboard charts to show a "Portfolio Overview" when no specific project is selected.
2. **Phase-Level Details:** Drill down further into specific project phases from the main dashboard.
3. **Material Tracking:** Enhance the construction module to link material costs directly to project budget line items.
4. **Project Archive:** Add functionality to "Complete" or "Archive" a project so it moves out of the active dropdown.
