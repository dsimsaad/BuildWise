using Microsoft.Data.SqlClient;

namespace BuildWise.Services;

public sealed class PropertyPhaseSchemaService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PropertyPhaseSchemaService> _logger;

    public PropertyPhaseSchemaService(IConfiguration configuration, ILogger<PropertyPhaseSchemaService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("BuildWise");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        // This keeps phase records compatible with databases created before phases belonged to properties.
        const string sql = @"
IF COL_LENGTH('Phases', 'PropertyID') IS NULL
BEGIN
    ALTER TABLE Phases ADD PropertyID INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Phases_PropertyID' AND object_id = OBJECT_ID('Phases'))
BEGIN
    CREATE INDEX IX_Phases_PropertyID ON Phases(PropertyID);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Phases_Properties_PropertyID')
BEGIN
    ALTER TABLE Phases WITH NOCHECK
    ADD CONSTRAINT FK_Phases_Properties_PropertyID FOREIGN KEY(PropertyID) REFERENCES Properties(PropertyID);
END;";

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Startup should not fail only because a local or old database cannot be upgraded yet.
            _logger.LogDebug(ex, "Property phase schema check skipped.");
        }
    }
}
