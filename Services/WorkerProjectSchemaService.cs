using BuildWise.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Services;

public sealed class WorkerProjectSchemaService
{
    // This guard prevents multiple startup requests from trying to alter the same tables at once.
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static bool _ensured;
    private readonly BuildWiseDbContext _context;

    public WorkerProjectSchemaService(BuildWiseDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        if (_ensured) return;

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (_ensured) return;

            var connection = (SqlConnection)_context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            // These statements keep older databases compatible with the current worker project model.
            await ExecuteQuietlyAsync(connection, @"
IF COL_LENGTH('Workers', 'ProjectID') IS NULL
BEGIN
    ALTER TABLE Workers ADD ProjectID int NULL;
END;", cancellationToken);

            await ExecuteQuietlyAsync(connection, @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Workers_ProjectID' AND object_id = OBJECT_ID('Workers'))
BEGIN
    CREATE INDEX IX_Workers_ProjectID ON Workers(ProjectID);
END;", cancellationToken);

            await ExecuteQuietlyAsync(connection, @"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Workers_Projects')
BEGIN
    ALTER TABLE Workers WITH CHECK ADD CONSTRAINT FK_Workers_Projects FOREIGN KEY(ProjectID) REFERENCES Projects(ProjectID);
END;", cancellationToken);

            await ExecuteQuietlyAsync(connection, @"
UPDATE w
SET ProjectID = p.ProjectID
FROM Workers w
OUTER APPLY (
    SELECT TOP 1 ProjectID
    FROM Projects p
    WHERE p.UserID = w.UserID
    ORDER BY CASE WHEN p.ProjectName = 'main' THEN 0 ELSE 1 END, p.ProjectName
) p
WHERE w.ProjectID IS NULL
  AND w.UserID IS NOT NULL
  AND p.ProjectID IS NOT NULL;", cancellationToken);

            await ExecuteQuietlyAsync(connection, @"
IF OBJECT_ID('WorkerProjectAssignments', 'U') IS NULL
BEGIN
    CREATE TABLE WorkerProjectAssignments (
        WorkerProjectAssignmentID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        WorkerID int NOT NULL,
        ProjectID int NOT NULL,
        AssignedAt datetime NOT NULL CONSTRAINT DF_WorkerProjectAssignments_AssignedAt DEFAULT GETDATE(),
        CONSTRAINT UQ_WorkerProjectAssignments UNIQUE (WorkerID, ProjectID),
        CONSTRAINT FK_WorkerProjectAssignments_Workers FOREIGN KEY (WorkerID) REFERENCES Workers(WorkerID) ON DELETE CASCADE,
        CONSTRAINT FK_WorkerProjectAssignments_Projects FOREIGN KEY (ProjectID) REFERENCES Projects(ProjectID) ON DELETE CASCADE
    );
END;", cancellationToken);

            await ExecuteQuietlyAsync(connection, @"
INSERT INTO WorkerProjectAssignments (WorkerID, ProjectID)
SELECT w.WorkerID, w.ProjectID
FROM Workers w
WHERE w.ProjectID IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM WorkerProjectAssignments wpa
      WHERE wpa.WorkerID = w.WorkerID
        AND wpa.ProjectID = w.ProjectID
  );", cancellationToken);

            _ensured = true;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async System.Threading.Tasks.Task ExecuteQuietlyAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
