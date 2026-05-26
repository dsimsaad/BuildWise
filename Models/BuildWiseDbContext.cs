using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Models;

public partial class BuildWiseDbContext : DbContext
{
    public BuildWiseDbContext()
    {
    }

    public BuildWiseDbContext(DbContextOptions<BuildWiseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AreaUnit> AreaUnits { get; set; } = null!;
    public virtual DbSet<Attendance> Attendances { get; set; } = null!;
    public virtual DbSet<AttendanceStatus> AttendanceStatuses { get; set; } = null!;
    public virtual DbSet<Budget> Budgets { get; set; } = null!;
    public virtual DbSet<BudgetAuditLog> BudgetAuditLogs { get; set; } = null!;
    public virtual DbSet<ClientPayment> ClientPayments { get; set; } = null!;
    public virtual DbSet<Contractor> Contractors { get; set; } = null!;
    public virtual DbSet<Expense> Expenses { get; set; } = null!;
    public virtual DbSet<ExpenseCategory> ExpenseCategories { get; set; } = null!;
    public virtual DbSet<Material> Materials { get; set; } = null!;
    public virtual DbSet<MaterialPurchase> MaterialPurchases { get; set; } = null!;
    public virtual DbSet<MaterialUnit> MaterialUnits { get; set; } = null!;
    public virtual DbSet<MaterialUsage> MaterialUsages { get; set; } = null!;
    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
    public virtual DbSet<Phase> Phases { get; set; } = null!;
    public virtual DbSet<PhaseType> PhaseTypes { get; set; } = null!;
    public virtual DbSet<Project> Projects { get; set; } = null!;
    public virtual DbSet<ProjectAlert> ProjectAlerts { get; set; } = null!;
    public virtual DbSet<Property> Properties { get; set; } = null!;
    public virtual DbSet<PropertyStatus> PropertyStatuses { get; set; } = null!;
    public virtual DbSet<PropertyType> PropertyTypes { get; set; } = null!;
    public virtual DbSet<Supplier> Suppliers { get; set; } = null!;
    public virtual DbSet<Task> Tasks { get; set; } = null!;
    public virtual DbSet<TaskStatus> TaskStatuses { get; set; } = null!;
    public virtual DbSet<TaskWorker> TaskWorkers { get; set; } = null!;
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<VwContractorSummary> VwContractorSummaries { get; set; } = null!;
    public virtual DbSet<VwDailyAttendance> VwDailyAttendances { get; set; } = null!;
    public virtual DbSet<VwExpenseHistory> VwExpenseHistories { get; set; } = null!;
    public virtual DbSet<VwMaterialCostByProject> VwMaterialCostByProjects { get; set; } = null!;
    public virtual DbSet<VwPhaseWiseCost> VwPhaseWiseCosts { get; set; } = null!;
    public virtual DbSet<VwProjectDashboard> VwProjectDashboards { get; set; } = null!;
    public virtual DbSet<VwWorkerWageSummary> VwWorkerWageSummaries { get; set; } = null!;
    public virtual DbSet<WagePayment> WagePayments { get; set; } = null!;
    public virtual DbSet<Worker> Workers { get; set; } = null!;
    public virtual DbSet<WorkerProjectAssignment> WorkerProjectAssignments { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=BuildWise");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AreaUnit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__AreaUnit__44F5EC95441F94E8");

            entity.ToTable("AreaUnit");

            entity.HasIndex(e => e.UnitName, "UQ__AreaUnit__B5EE667824F8D709").IsUnique();

            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.UnitName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69263C33420A29");

            entity.ToTable("Attendance", tb => tb.HasTrigger("trg_PreventFutureDateAttendance"));

            entity.HasIndex(e => new { e.ProjectId, e.AttendanceDate }, "IX_Attendance_ProjectDate");

            entity.HasIndex(e => new { e.WorkerId, e.AttendanceDate }, "IX_Attendance_WorkerDate");

            entity.HasIndex(e => new { e.WorkerId, e.ProjectId, e.AttendanceDate }, "UQ_Attendance").IsUnique();

            entity.Property(e => e.AttendanceId)
                .HasComment("Auto-increment PK")
                .HasColumnName("AttendanceID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.ProjectId)
                .HasComment("FK to Projects — which site they attended")
                .HasColumnName("ProjectID");
            entity.Property(e => e.StatusId)
                .HasDefaultValue((byte)1)
                .HasComment("FK to AttendanceStatus: Present/Absent/Half Day/Leave")
                .HasColumnName("StatusID");
            entity.Property(e => e.WageForDay)
                .HasComment("Actual wage paid for this day (0 if absent, 50% if half day)")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.WorkerId)
                .HasComment("FK to Workers")
                .HasColumnName("WorkerID");

            entity.HasOne(d => d.Project).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__Proje__46B27FE2");

            entity.HasOne(d => d.Status).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__Statu__47A6A41B");

            entity.HasOne(d => d.Worker).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.WorkerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__Worke__45BE5BA9");
        });

        modelBuilder.Entity<AttendanceStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Attendan__C8EE2043237C452A");

            entity.ToTable("AttendanceStatus");

            entity.HasIndex(e => e.StatusName, "UQ__Attendan__05E7698A52FCB386").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.BudgetId).HasName("PK__Budgets__E38E79C4A6457FAA");

            entity.ToTable(tb => tb.HasTrigger("trg_LogBudgetUpdate"));

            entity.HasIndex(e => e.ProjectId, "UQ__Budgets__761ABED17412DCF4").IsUnique();

            entity.Property(e => e.BudgetId)
                .HasComment("Auto-increment PK — one budget record per project")
                .HasColumnName("BudgetID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LaborBudget)
                .HasDefaultValue(0m)
                .HasComment("Portion of budget allocated to labor costs")
                .HasColumnType("decimal(14, 2)");
            entity.Property(e => e.MaterialBudget)
                .HasDefaultValue(0m)
                .HasComment("Portion allocated to materials")
                .HasColumnType("decimal(14, 2)");
            entity.Property(e => e.MiscBudget)
                .HasDefaultValue(0m)
                .HasComment("Remaining allocation for equipment, transport, misc")
                .HasColumnType("decimal(14, 2)");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.TotalBudget).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Project).WithOne(p => p.Budget)
                .HasForeignKey<Budget>(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Budgets__Project__37703C52");
        });

        modelBuilder.Entity<BudgetAuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__BudgetAu__5E5499A8014A3FA1");

            entity.ToTable("BudgetAuditLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.BudgetId).HasColumnName("BudgetID");
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ChangedByMsg)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("System");
            entity.Property(e => e.NewBudget).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.OldBudget).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
        });

        modelBuilder.Entity<ClientPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__ClientPa__9B556A5864E1E165");

            entity.Property(e => e.PaymentId)
                .HasComment("Auto-increment PK")
                .HasColumnName("PaymentID");
            entity.Property(e => e.Amount)
                .HasComment("Money received from client in PKR")
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.PaymentMethodId).HasColumnName("PaymentMethodID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.ClientPayments)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK__ClientPay__Payme__40F9A68C");

            entity.HasOne(d => d.Project).WithMany(p => p.ClientPayments)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClientPay__Proje__3F115E1A");
        });

        modelBuilder.Entity<Contractor>(entity =>
        {
            entity.HasKey(e => e.ContractorId).HasName("PK__Contract__E964EB5D1EDCB5FF");

            entity.Property(e => e.ContractorId).HasColumnName("ContractorID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.ContractCost).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SpecialityNotes).HasMaxLength(300);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId).HasName("PK__Expenses__1445CFF3DEA83886");

            entity.ToTable(tb => tb.HasTrigger("trg_PreventNegativeExpense"));

            entity.HasIndex(e => e.ExpenseDate, "IX_Expenses_Date");

            entity.HasIndex(e => e.PhaseId, "IX_Expenses_PhaseID");

            entity.HasIndex(e => e.ProjectId, "IX_Expenses_ProjectID");

            entity.Property(e => e.ExpenseId)
                .HasComment("Auto-increment PK")
                .HasColumnName("ExpenseID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CategoryId)
                .HasComment("FK to ExpenseCategory: Labor/Material/Equipment/Transport/Misc")
                .HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.ExpenseDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.PaymentMethodId).HasColumnName("PaymentMethodID");
            entity.Property(e => e.PhaseId).HasColumnName("PhaseID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ReceiptUrl)
                .HasMaxLength(500)
                .HasComment("Path/URL to uploaded receipt image for proof")
                .HasColumnName("ReceiptURL");

            entity.HasOne(d => d.Category).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Expenses__Catego__30C33EC3");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK__Expenses__Paymen__32AB8735");

            entity.HasOne(d => d.Phase).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.PhaseId)
                .HasConstraintName("FK__Expenses__PhaseI__2FCF1A8A");

            entity.HasOne(d => d.Project).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Expenses__Projec__2EDAF651");
        });

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ExpenseC__19093A2BD5F55E62");

            entity.ToTable("ExpenseCategory");

            entity.HasIndex(e => e.CategoryName, "UQ__ExpenseC__8517B2E0D1B789A1").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C506131756EBF23D");

            entity.HasIndex(e => e.MaterialName, "UQ__Material__9C87053C39999378").IsUnique();

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.DefaultUnitId).HasColumnName("DefaultUnitID");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaterialName).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Materials)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Materials_Users");

            entity.HasOne(d => d.DefaultUnit).WithMany(p => p.Materials)
                .HasForeignKey(d => d.DefaultUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Materials__Defau__1EA48E88");
        });

        modelBuilder.Entity<MaterialPurchase>(entity =>
        {
            entity.HasKey(e => e.PurchaseId).HasName("PK__Material__6B0A6BDE9F7014BF");

            entity.HasIndex(e => e.ProjectId, "IX_MatPurchase_ProjectID");

            entity.Property(e => e.PurchaseId)
                .HasComment("Auto-increment PK")
                .HasColumnName("PurchaseID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Supplier invoice reference for audit trail");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.PurchaseDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.Quantity).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalCost)
                .HasComputedColumnSql("([Quantity]*[UnitPrice])", true)
                .HasComment("Computed column: Quantity × UnitPrice, persisted")
                .HasColumnType("decimal(23, 5)");
            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialPurchases)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MaterialP__Mater__236943A5");

            entity.HasOne(d => d.Project).WithMany(p => p.MaterialPurchases)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MaterialP__Proje__22751F6C");

            entity.HasOne(d => d.Supplier).WithMany(p => p.MaterialPurchases)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__MaterialP__Suppl__245D67DE");

            entity.HasOne(d => d.Unit).WithMany(p => p.MaterialPurchases)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MaterialP__UnitI__25518C17");
        });

        modelBuilder.Entity<MaterialUnit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__Material__44F5EC9538EE1416");

            entity.ToTable("MaterialUnit");

            entity.HasIndex(e => e.UnitName, "UQ__Material__B5EE66782D88D194").IsUnique();

            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.UnitName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MaterialUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__Material__29B197C048DA9E6C");

            entity.Property(e => e.UsageId).HasColumnName("UsageID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PhaseId).HasColumnName("PhaseID");
            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.QuantityUsed).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.UsageDate).HasDefaultValueSql("(CONVERT([date],getdate()))");

            entity.HasOne(d => d.Phase).WithMany(p => p.MaterialUsages)
                .HasForeignKey(d => d.PhaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MaterialU__Phase__2B0A656D");

            entity.HasOne(d => d.Purchase).WithMany(p => p.MaterialUsages)
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MaterialU__Purch__2A164134");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.MethodId).HasName("PK__PaymentM__FC681FB1277AC705");

            entity.ToTable("PaymentMethod");

            entity.HasIndex(e => e.MethodName, "UQ__PaymentM__218CFB177207F2C1").IsUnique();

            entity.Property(e => e.MethodId).HasColumnName("MethodID");
            entity.Property(e => e.MethodName)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Phase>(entity =>
        {
            entity.HasKey(e => e.PhaseId).HasName("PK__Phases__5BA26D42C41233AC");

            entity.ToTable(tb => tb.HasTrigger("trg_AutoCompleteProject"));

            entity.HasIndex(e => e.ProjectId, "IX_Phases_ProjectID");
            entity.HasIndex(e => e.PropertyId, "IX_Phases_PropertyID");

            entity.HasIndex(e => new { e.ProjectId, e.Sequence }, "UQ_Phase_Project_Seq").IsUnique();

            entity.Property(e => e.PhaseId)
                .HasComment("Auto-increment PK")
                .HasColumnName("PhaseID");
            entity.Property(e => e.CustomPhaseName)
                .HasMaxLength(100)
                .HasComment("Used only when PhaseTypeID = 8 (Custom)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.PhaseTypeId)
                .HasComment("FK to PhaseType — Foundation, Grey Structure, Finishing etc")
                .HasColumnName("PhaseTypeID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.PropertyId).HasColumnName("PropertyID");
            entity.Property(e => e.Sequence)
                .HasDefaultValue((byte)1)
                .HasComment("Ordering of phases within the project (1 = first)");

            entity.HasOne(d => d.PhaseType).WithMany(p => p.Phases)
                .HasForeignKey(d => d.PhaseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Phases__PhaseTyp__0A9D95DB");

            entity.HasOne(d => d.Project).WithMany(p => p.Phases)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__Phases__ProjectI__09A971A2");

            entity.HasOne(d => d.Property).WithMany(p => p.Phases)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_Phases_Properties_PropertyID");
        });

        modelBuilder.Entity<PhaseType>(entity =>
        {
            entity.HasKey(e => e.PhaseTypeId).HasName("PK__PhaseTyp__4D86607FAD8FC7F2");

            entity.ToTable("PhaseType");

            entity.HasIndex(e => e.PhaseName, "UQ__PhaseTyp__DB942EE30EF9AF4D").IsUnique();

            entity.Property(e => e.PhaseTypeId).HasColumnName("PhaseTypeID");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__761ABED08098D436");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("trg_SyncProjectBudget");
                    tb.HasTrigger("trg_UpdateTimestamp_Projects");
                });

            entity.HasIndex(e => e.PropertyId, "IX_Projects_PropertyID");

            entity.HasIndex(e => e.UserId, "IX_Projects_UserID");

            entity.Property(e => e.ProjectId)
                .HasComment("Auto-increment PK")
                .HasColumnName("ProjectID");
            entity.Property(e => e.ActualEndDate).HasComment("Filled when project is marked complete");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsCompleted).HasComment("1 = project closed, 0 = ongoing");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.PropertyId)
                .HasComment("FK to Properties — which property this project is on")
                .HasColumnName("PropertyID");
            entity.Property(e => e.TotalBudget)
                .HasComment("Overall approved budget in PKR")
                .HasColumnType("decimal(14, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Property).WithMany(p => p.Projects)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Projects__Proper__01142BA1");

            entity.HasOne(d => d.User).WithMany(p => p.Projects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Projects__UserID__02084FDA");
        });

        modelBuilder.Entity<ProjectAlert>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__ProjectA__EBB16AED16EE43CC");

            entity.Property(e => e.AlertId).HasColumnName("AlertID");
            entity.Property(e => e.AlertMessage).HasMaxLength(500);
            entity.Property(e => e.AlertType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectAlerts)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProjectAl__Proje__70A8B9AE");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("PK__Properti__70C9A755CA0B00B5");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Properties"));

            entity.HasIndex(e => e.UserId, "IX_Properties_UserID");

            entity.HasIndex(e => e.ProjectId, "IX_Properties_ProjectID");

            entity.Property(e => e.PropertyId)
                .HasComment("Auto-increment PK")
                .HasColumnName("PropertyID");
            entity.Property(e => e.AreaSize)
                .HasComment("Numeric area value, unit determined by AreaUnitID")
                .HasColumnType("decimal(10, 4)");
            entity.Property(e => e.AreaUnitId)
                .HasComment("FK to AreaUnit: Marla/Kanal/SqFt/SqM")
                .HasColumnName("AreaUnitID");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Location).HasMaxLength(300);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PropertyName).HasMaxLength(150);
            entity.Property(e => e.ProjectId)
                .HasComment("Optional FK to Projects — parent project for this property")
                .HasColumnName("ProjectID");
            entity.Property(e => e.StatusId)
                .HasDefaultValue((byte)1)
                .HasComment("FK to PropertyStatus: Under Construction/Completed etc")
                .HasColumnName("StatusID");
            entity.Property(e => e.TypeId)
                .HasComment("FK to PropertyType: Plot/House/Apartment/Commercial")
                .HasColumnName("TypeID");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId)
                .HasComment("FK to Users — owner of this property")
                .HasColumnName("UserID");

            entity.HasOne(d => d.AreaUnit).WithMany(p => p.Properties)
                .HasForeignKey(d => d.AreaUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__AreaU__7C4F7684");

            entity.HasOne(d => d.Project).WithMany(p => p.Properties)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Properties_Projects_ProjectID");

            entity.HasOne(d => d.Status).WithMany(p => p.Properties)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__Statu__7A672E12");

            entity.HasOne(d => d.Type).WithMany(p => p.Properties)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__TypeI__797309D9");

            entity.HasOne(d => d.User).WithMany(p => p.Properties)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__UserI__787EE5A0");
        });

        modelBuilder.Entity<PropertyStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Property__C8EE2043829596AD");

            entity.ToTable("PropertyStatus");

            entity.HasIndex(e => e.StatusName, "UQ__Property__05E7698AAF0F992C").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PropertyType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK__Property__516F0395F8B0A991");

            entity.ToTable("PropertyType");

            entity.HasIndex(e => e.TypeName, "UQ__Property__D4E7DFA8EE59639D").IsUnique();

            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE66694319EE9B5");

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SupplierName).HasMaxLength(100);
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Tasks__7C6949D15C18C8F1");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("trg_AutoCompletePhase");
                    tb.HasTrigger("trg_UpdateTimestamp_Tasks");
                });

            entity.HasIndex(e => e.ContractorId, "IX_Tasks_ContractorID");

            entity.HasIndex(e => e.PhaseId, "IX_Tasks_PhaseID");

            entity.Property(e => e.TaskId)
                .HasComment("Auto-increment PK")
                .HasColumnName("TaskID");
            entity.Property(e => e.ContractorId)
                .HasComment("FK to Contractors — nullable, who is responsible")
                .HasColumnName("ContractorID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.EstimatedCost)
                .HasDefaultValue(0m)
                .HasComment("Budgeted cost for this specific task in PKR")
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PhaseId)
                .HasComment("FK to Phases — task belongs to this phase")
                .HasColumnName("PhaseID");
            entity.Property(e => e.StatusId)
                .HasDefaultValue((byte)1)
                .HasComment("FK to TaskStatus: Pending/In Progress/Completed/Hold/Cancelled")
                .HasColumnName("StatusID");
            entity.Property(e => e.TaskName).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Contractor).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ContractorId)
                .HasConstraintName("FK__Tasks__Contracto__10566F31");

            entity.HasOne(d => d.Phase).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.PhaseId)
                .HasConstraintName("FK__Tasks__PhaseID__0F624AF8");

            entity.HasOne(d => d.Status).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tasks__StatusID__114A936A");
        });

        modelBuilder.Entity<TaskStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__TaskStat__C8EE2043FA6571EA");

            entity.ToTable("TaskStatus");

            entity.HasIndex(e => e.StatusName, "UQ__TaskStat__05E7698AD6EAA427").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TaskWorker>(entity =>
        {
            entity.HasKey(e => e.TaskWorkerId).HasName("PK__TaskWork__BA349280E655B290");

            entity.HasIndex(e => new { e.TaskId, e.WorkerId }, "UQ_TaskWorker").IsUnique();

            entity.Property(e => e.TaskWorkerId).HasColumnName("TaskWorkerID");
            entity.Property(e => e.AssignedDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.TaskId).HasColumnName("TaskID");
            entity.Property(e => e.WorkerId).HasColumnName("WorkerID");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskWorkers)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("FK__TaskWorke__TaskI__18EBB532");

            entity.HasOne(d => d.Worker).WithMany(p => p.TaskWorkers)
                .HasForeignKey(d => d.WorkerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaskWorke__Worke__19DFD96B");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC90800E77");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534C0CC46CD").IsUnique();

            entity.Property(e => e.UserId)
                .HasComment("Auto-increment primary key for user accounts")
                .HasColumnName("UserID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasComment("Unique login email; used as username");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasComment("Full display name of the owner/user");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasComment("1 = active account, 0 = soft-deleted");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .IsUnicode(false)
                .HasComment("BCrypt hashed password — never store plain text");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("Optional contact number");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Profession).HasMaxLength(100);
            entity.Property(e => e.ProfileImageUrl)
                .HasMaxLength(500)
                .HasColumnName("ProfileImageURL");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<VwContractorSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ContractorSummary");

            entity.Property(e => e.ContractCost).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ContractorId).HasColumnName("ContractorID");
            entity.Property(e => e.ContractorName).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwDailyAttendance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DailyAttendance");

            entity.Property(e => e.AttendanceStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.SkillType).HasMaxLength(100);
            entity.Property(e => e.WageForDay).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.WorkerId).HasColumnName("WorkerID");
            entity.Property(e => e.WorkerName).HasMaxLength(100);
        });

        modelBuilder.Entity<VwExpenseHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ExpenseHistory");

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.ExpenseId).HasColumnName("ExpenseID");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PhaseId).HasColumnName("PhaseID");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.ReceiptUrl)
                .HasMaxLength(500)
                .HasColumnName("ReceiptURL");
        });

        modelBuilder.Entity<VwMaterialCostByProject>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MaterialCostByProject");

            entity.Property(e => e.MaterialName).HasMaxLength(100);
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.SupplierName).HasMaxLength(100);
            entity.Property(e => e.TotalCost).HasColumnType("decimal(38, 5)");
            entity.Property(e => e.TotalQuantityPurchased).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.UnitName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwPhaseWiseCost>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PhaseWiseCost");

            entity.Property(e => e.DisplayPhaseName).HasMaxLength(100);
            entity.Property(e => e.ExpenseCost).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.MaterialCost).HasColumnType("decimal(38, 5)");
            entity.Property(e => e.PhaseId).HasColumnName("PhaseID");
            entity.Property(e => e.PhaseName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.TotalPhaseCost).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwProjectDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ProjectDashboard");

            entity.Property(e => e.OwnerName).HasMaxLength(100);
            entity.Property(e => e.PhaseProgressPct).HasColumnName("PhaseProgress_Pct");
            entity.Property(e => e.ProfitLoss).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.PropertyLocation).HasMaxLength(300);
            entity.Property(e => e.PropertyName).HasMaxLength(150);
            entity.Property(e => e.RemainingBudget).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TaskProgressPct).HasColumnName("TaskProgress_Pct");
            entity.Property(e => e.TotalBudget).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.TotalClientPayments).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalExpenses).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalMaterials).HasColumnType("decimal(38, 5)");
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalWagesPaid).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwWorkerWageSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_WorkerWageSummary");

            entity.Property(e => e.DailyWage).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.SkillType).HasMaxLength(100);
            entity.Property(e => e.TotalWageEarned).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalWagePaid).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.WageDue).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.WorkerId).HasColumnName("WorkerID");
            entity.Property(e => e.WorkerName).HasMaxLength(100);
        });

        modelBuilder.Entity<WagePayment>(entity =>
        {
            entity.HasKey(e => e.WagePaymentId).HasName("PK__WagePaym__2FFA88C8635198DF");

            entity.HasIndex(e => new { e.WorkerId, e.ProjectId }, "IX_WagePay_WorkerID");

            entity.Property(e => e.WagePaymentId)
                .HasComment("Auto-increment PK")
                .HasColumnName("WagePaymentID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.PaymentMethodId).HasColumnName("PaymentMethodID");
            entity.Property(e => e.PeriodFrom).HasComment("Start date of the pay period being settled");
            entity.Property(e => e.PeriodTo).HasComment("End date of the pay period being settled");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.WorkerId).HasColumnName("WorkerID");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.WagePayments)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK__WagePayme__Payme__4F47C5E3");

            entity.HasOne(d => d.Project).WithMany(p => p.WagePayments)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WagePayme__Proje__4D5F7D71");

            entity.HasOne(d => d.Worker).WithMany(p => p.WagePayments)
                .HasForeignKey(d => d.WorkerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WagePayme__Worke__4C6B5938");
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(e => e.WorkerId).HasName("PK__Workers__077C8806AAB344EE");

            entity.HasIndex(e => e.Cnic, "UQ__Workers__AA570FD4566FD91A").IsUnique();
            entity.HasIndex(e => e.ProjectId, "IX_Workers_ProjectID");

            entity.Property(e => e.WorkerId)
                .HasComment("Auto-increment PK")
                .HasColumnName("WorkerID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Cnic)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength()
                .HasComment("Pakistani CNIC number (13 digits + dashes), unique identifier")
                .HasColumnName("CNIC");
            entity.Property(e => e.ContractorId)
                .HasComment("FK to Contractors — null if independent worker")
                .HasColumnName("ContractorID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DailyWage)
                .HasComment("Default daily wage in PKR, can be overridden per attendance record")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SkillType)
                .HasMaxLength(100)
                .HasComment("e.g. Mason, Carpenter, Electrician, Helper, Plumber");

            entity.HasOne(d => d.Contractor).WithMany(p => p.Workers)
                .HasForeignKey(d => d.ContractorId)
                .HasConstraintName("FK__Workers__Contrac__72C60C4A");

            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Workers_Projects");
        });

        modelBuilder.Entity<WorkerProjectAssignment>(entity =>
        {
            entity.ToTable("WorkerProjectAssignments");

            entity.HasKey(e => e.WorkerProjectAssignmentId);

            entity.HasIndex(e => new { e.WorkerId, e.ProjectId }, "UQ_WorkerProjectAssignments").IsUnique();

            entity.Property(e => e.WorkerProjectAssignmentId).HasColumnName("WorkerProjectAssignmentID");
            entity.Property(e => e.WorkerId).HasColumnName("WorkerID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Worker).WithMany(p => p.WorkerProjectAssignments)
                .HasForeignKey(d => d.WorkerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WorkerProjectAssignments_Workers");

            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WorkerProjectAssignments_Projects");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
