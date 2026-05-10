# BuildWise - User Guide

Welcome to BuildWise, a comprehensive Project Management & Cost Analysis platform designed for the construction industry. This guide covers how to get the application running on your local machine.

---

## 🚀 What is BuildWise?

BuildWise helps you track construction labor, project costs, and day-to-day operations. 
It features a **Project-Centric Workspace** where you can switch between active projects and see real-time data on expenses, wages, tasks, and budgets.

Key Features:
- **Project & Phase Tracking**: Manage construction phases, tasks, and budgets.
- **Worker Management & Attendance**: Track workers on site and calculate wages.
- **Cost Analysis**: Visualize expenses vs. budget with dynamic dashboard charts.

---

## 🛠 Prerequisites

Before running the application, make sure you have the following installed on your machine:
- **.NET 10 SDK**: [Download Here](https://dotnet.microsoft.com/download)
- **SQL Server (or Express/Docker)**: [Download Here](https://www.microsoft.com/sql-server/sql-server-downloads)
- **SQL Server Management Studio (SSMS)**: [Download Here](https://aka.ms/ssmsfullsetup)

---

## ⚙️ Setup Instructions

### 1. Database Setup
1. Open **SSMS** and connect to your local SQL Server instance.
2. Click **File → Open → File** and locate the `BuildWise_Full_Script.sql` (if provided in your SQL dump folder).
3. Execute the script to create the database and tables.
4. You should see `BuildWiseDB` appear in your databases list.

### 2. Configuration (`appsettings.json`)
The connection string dictates how the application talks to your database. **Everyone's connection string is different**, so the `appsettings.json` file is ignored by Git to avoid conflicts.

1. Locate `appsettings.Example.json` in the root folder.
2. Create a new file named `appsettings.json` in the exact same location.
3. Copy the contents of `appsettings.Example.json` into `appsettings.json`.
4. Update the connection string to match your SQL Server setup:

**For Windows (SSMS Local):**
```json
"ConnectionStrings": {
  "BuildWise": "Server=YOUR_PC_NAME\\SQLEXPRESS;Database=BuildWiseDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

**For Mac / Docker:**
```json
"ConnectionStrings": {
  "BuildWise": "Server=localhost,1433;Database=BuildWiseDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
}
```

### 3. Run the Application
Open a terminal in the root folder of the project and run:

```bash
dotnet restore
dotnet run
```

Once the build is complete, the terminal will display the local URL (e.g., `http://localhost:5057`). Open that URL in your browser to start using BuildWise!

---

## ❗ Troubleshooting

- **"Cannot connect to database"**: Ensure SQL Server is running. Double-check your server name, database name, and credentials in `appsettings.json`.
- **"Invalid column name"**: Make sure you have the latest EF Core migrations applied or the latest database script executed.
- **"dotnet not recognized"**: Ensure you have installed the .NET SDK and restarted your terminal.
