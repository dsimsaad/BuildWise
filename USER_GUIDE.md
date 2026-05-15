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
2. Click **File -> Open -> File** and locate `BuildWiseDB/BuildWiseDB.sql`.
3. Execute the script to create the database and main tables.
4. Open `migrate.sql` and execute it too. This applies the latest table/column fixes for the current project version.
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

### 3. Run the Latest Local Version
Open a terminal in the root folder of the project and run:

**Mac / Linux terminal:**
```bash
dotnet restore
PORT=$((5000 + RANDOM % 1000))
dotnet run --no-launch-profile --urls "http://localhost:$PORT"
```

**Windows PowerShell:**
```powershell
dotnet restore
$port = Get-Random -Minimum 5000 -Maximum 5999
dotnet run --no-launch-profile --urls "http://localhost:$port"
```

This starts the app on a different local port most of the time. That means you usually do not need to stop an older `localhost` window before opening the latest version.

The URL to open will be the port shown in the command. For example, if the terminal says `Now listening on: http://localhost:5482`, open `http://localhost:5482`.

If you prefer the old fixed-port behavior, you can still run:

```bash
dotnet run
```

---

## ❗ Troubleshooting

- **"Cannot connect to database"**: Ensure SQL Server is running. Double-check your server name, database name, and credentials in `appsettings.json`.
- **"Invalid column name"**: Make sure you have the latest EF Core migrations applied or the latest database script executed.
- **"dotnet not recognized"**: Ensure you have installed the .NET SDK and restarted your terminal.
