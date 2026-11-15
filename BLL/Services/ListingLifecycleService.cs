using ARABULUCU.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ARABULUCU.BLL.Services;

public sealed class ListingLifecycleService
{
    public static readonly TimeSpan InactivityWindow = TimeSpan.FromDays(30);

    private readonly ApplicationDbContext _dbContext;

    public ListingLifecycleService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<int> ArchiveInactiveListingsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(InactivityWindow);
        var listings = await _dbContext.LostItems
            .Where(item => !item.IsArchived && !item.IsDelivered &&
                item.LastActivityAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var listing in listings)
        {
            listing.IsArchived = true;
            _dbContext.ListingStatusHistories.Add(new ENT.Entities.ListingStatusHistory
            {
                LostItemId = listing.Id,
                Status = "Archived",
                Details = "30 gün boyunca işlem görmediği için otomatik arşivlendi."
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return listings.Count;
    }
}

public sealed class ListingArchiveHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ListingArchiveHostedService> _logger;

    public ListingArchiveHostedService(IServiceScopeFactory scopeFactory, ILogger<ListingArchiveHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ListingLifecycleService>();
                var archived = await service.ArchiveInactiveListingsAsync(stoppingToken);
                if (archived > 0)
                {
                    _logger.LogInformation("{Count} inactive listings archived.", archived);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Automatic listing archival failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
