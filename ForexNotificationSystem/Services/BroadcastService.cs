using Microsoft.AspNetCore.SignalR;
using Serilog;
using System.Collections.Concurrent;

public class BroadcastService : BackgroundService
{
    private readonly IHubContext<ForexHub> _hub;
    private readonly ILogger<BroadcastService> _logger;

    public BroadcastService(IHubContext<ForexHub> hub, ILogger<BroadcastService> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastService started - will broadcast every 500ms");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cacheSnapshot = FinnhubIngestService.Cache.ToList();

                if (cacheSnapshot.Count == 0)
                {
                    _logger.LogDebug("Cache is empty, nothing to broadcast");
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                _logger.LogDebug("Broadcasting {Count} symbols to SignalR groups", cacheSnapshot.Count);

                foreach (var kv in cacheSnapshot)
                {
                    try
                    {
                        var symbol = kv.Key;
                        var priceTick = kv.Value;

                        // Send to SignalR group (only users subscribed to this symbol)
                        await _hub.Clients.Group(symbol).SendAsync("priceUpdate", priceTick, stoppingToken);

                        _logger.LogDebug("Broadcasted {Symbol} @ {Price} to group", symbol, priceTick.price);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error broadcasting symbol {Symbol}", kv.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BroadcastService main loop");
            }

            // Wait 500ms before next broadcast
            await Task.Delay(500, stoppingToken);
        }

        _logger.LogWarning("BroadcastService stopping");
    }
}