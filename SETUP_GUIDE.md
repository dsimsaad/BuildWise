# BuildWise — Setup Guide for Your Friend

Hi! Here's how to get BuildWise running on your computer.

---

## What's in the ZIP

| Folder/File | What it is |
|-------------|-----------|
| `Controllers/` | Backend logic — handles web requests |
| `Models/` | C# classes that map to every database table |
| `Views/` | HTML pages (.cshtml files) |
| `wwwroot/` | Public files — CSS, JavaScript, Bootstrap, images |
| `appsettings.json` | App config — **you need to edit this** (connection string) |
| `Program.cs` | App startup file |
| `BuildWiseApp.csproj` | Project file — lists all packages needed |
| `BuildWise.md` | Full developer guide |
| `.gitignore` | Tells Git which files to ignore (ignore this file) |

> `bin/` and `obj/` folders are NOT in the zip — that's fine, .NET will recreate them automatically.

---

## Step 1 — Install Required Software

Install these (if you don't have them already):

| Software | Download |
|----------|----------|
| .NET 10 SDK | https://dotnet.microsoft.com/download |
| SQL Server Express | https://www.microsoft.com/sql-server/sql-server-downloads |
| SSMS (SQL Server Management Studio) | https://aka.ms/ssmsfullsetup |
| Visual Studio 2022 Community (free) | https://visualstudio.microsoft.com |

---

## Step 2 — Restore the Database

1. Open **SSMS**
2. Connect to your local SQL Server
3. Click **File → Open → File** and open the file: `BuildWise_Full_Script.sql`
4. Click the **Execute** button (or press F5)
5. You should see: `BuildWise` appear in your databases list on the left

---

## Step 3 — Extract the ZIP

Right-click `BuildWise_Share.zip` → **Extract All** → Choose a location like `C:\Projects\BuildWise\`

---

## Step 4 — Update the Connection String

Open `appsettings.json` in any text editor (Notepad is fine). Find this line:

```json
"BuildWise": "Server=espionge\\SQLEXPRESS;Database=BuildWise;Trusted_Connection=True;TrustServerCertificate=True;"
```

Change `espionge` to **your computer name**.

To find your computer name:
- Press `Win + R` → type `sysdm.cpl` → press Enter
- Your computer name is shown under "Computer name"

Or use `.` which means "this computer":
```json
"BuildWise": "Server=.\\SQLEXPRESS;Database=BuildWise;Trusted_Connection=True;TrustServerCertificate=True;"
```

---

## Step 5 — Run the App

Open a terminal (Command Prompt or PowerShell) in the project folder:

```bash
dotnet restore
dotnet run --urls "http://localhost:5200"
```

Then open your browser and go to: **http://localhost:5200**

You should see the BuildWise website!

---

## What bin/ and obj/ Are (They're Not in the ZIP)

These folders get created automatically when you run `dotnet restore` and `dotnet build`:

| Folder | What it contains | Why excluded from ZIP |
|--------|-----------------|----------------------|
| `bin/` | The compiled .exe and .dll files — the actual runnable app | Very large (100MB+), gets rebuilt automatically |
| `obj/` | Temporary build files used during compilation | Not needed, gets rebuilt automatically |

**You never need to manually touch `bin/` or `obj/`.** Just run `dotnet run` and .NET handles everything.

---

## What wwwroot/ Is

`wwwroot/` is the **public folder** — anything here is served directly to the browser:

```
wwwroot/
  css/          ← Your custom CSS styles
  js/           ← Your custom JavaScript
  lib/
    bootstrap/  ← Bootstrap 5 (buttons, tables, navbar styling)
    jquery/     ← jQuery library
```

When your browser loads the page, it requests Bootstrap CSS from here. **Frontend developers work here** when styling the app.

---

## What .gitignore Is

`.gitignore` is a file that tells **Git** (version control software) which files to NOT upload when sharing code. It's already configured to exclude `bin/`, `obj/`, and Visual Studio temporary files.

**If you're not using Git, you can completely ignore this file.** It doesn't affect how the app runs.

---

## If You Get an Error

**"Cannot connect to database"**
→ Make sure SQL Server is running. Open SSMS and try connecting first. Check your computer name in `appsettings.json`.

**"dotnet not recognized"**
→ .NET SDK is not installed, or you need to restart your terminal after installing it.

**"Port 5200 already in use"**
→ Change `5200` to another number like `5300`:
```bash
dotnet run --urls "http://localhost:5300"
```

---

## What's Built So Far

| Feature | Status |
|---------|--------|
| Full database (19 tables, 7 reporting views, 9 triggers) | Done |
| Projects — list, create, edit, delete, view | Done |
| Workers — list, create, edit, delete, view | Done |
| Login / Register | Not built yet |
| Attendance tracking | Not built yet |
| Expense tracking | Not built yet |
| Dashboard | Not built yet |

See `BuildWise.md` for the full backend development guide.

---

*BuildWise — Construction Labor & Project Cost Tracker*
