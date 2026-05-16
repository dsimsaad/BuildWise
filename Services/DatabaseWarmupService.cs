using BuildWise.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Services;

public sealed class DatabaseWarmupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseWarmupService> _logger;

    public DatabaseWarmupService(IServiceScopeFactory scopeFactory, ILogger<DatabaseWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WarmDatabaseAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(4));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await WarmDatabaseAsync(stoppingToken);
        }
    }

    private async System.Threading.Tasks.Task WarmDatabaseAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BuildWiseDbContext>();
            await context.Database.ExecuteSqlRawAsync("SELECT 1", stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Database warmup skipped.");
        }
    }
}
