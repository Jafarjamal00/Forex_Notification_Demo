using ForexNotificationSystem.Data;
using ForexNotificationSystem.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Websocket.Client;

public class FinnhubIngestService : BackgroundService
{
    private readonly ILogger<FinnhubIngestService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;

    public static Dictionary<string, PriceTick> Cache = new();

    private static int _tickCounter = 0;
    private const int SAVE_BATCH_SIZE = 10;

    public FinnhubIngestService(ILogger<FinnhubIngestService> logger, IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var apiKey = _config["Finnhub:ApiKey"];
        var url = new Uri($"wss://ws.finnhub.io?token={apiKey}");

        _logger.LogInformation("🚀 Connecting to Finnhub WebSocket: {Url}", url);

        using var client = new WebsocketClient(url);

        List<string> symbols;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            symbols = await db.ForexSymbols
                .Where(s => s.IsActive)
                .Select(s => s.Symbol!)
                .ToListAsync(stoppingToken);

            _logger.LogInformation("📊 Loaded {Count} symbols from database", symbols.Count);

            if (symbols.Count == 0)
            {
                _logger.LogWarning("⚠️ No symbols found in database! Please insert symbols into forex_symbol table.");
                _logger.LogWarning("Example SQL: INSERT INTO forex_symbol (symbol, is_active) VALUES ('OANDA:EUR_USD', true);");
            }
        }

        client.MessageReceived.Subscribe(async msg =>
        {
            try
            {
                var json = JObject.Parse(msg.Text);

                var msgType = json["type"]?.ToString();

                if (msgType == "ping")
                {
                    _logger.LogDebug("📡 Received ping from Finnhub");
                    return;
                }

                if (msgType != "trade" || json["data"] == null)
                {
                    return;
                }

                var ticksToSave = new List<PriceTick>();

                foreach (var tick in json["data"]!)
                {
                    var symbol = tick["s"]!.ToString();
                    var price = decimal.Parse(tick["p"]!.ToString());
                    var ts = long.Parse(tick["t"]!.ToString());

                    var priceTick = new PriceTick
                    {
                        symbol = symbol,
                        price = price,
                        bid = price * 0.9999m,
                        ask = price * 1.0001m,
                        ts = ts
                    };

                    Cache[symbol] = priceTick;
                    ticksToSave.Add(priceTick);
                    _tickCounter++;

                    _logger.LogInformation("📊 Tick: {Symbol} @ {Price}", symbol, price);
                }

                _logger.LogDebug("Processed {Count} ticks. Cache size: {CacheSize}", ticksToSave.Count, Cache.Count);

                if (_tickCounter >= SAVE_BATCH_SIZE && ticksToSave.Count > 0)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await db.PriceTicks.AddRangeAsync(ticksToSave, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("💾 Saved batch of {Count} ticks to database", ticksToSave.Count);
                    _tickCounter = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing Finnhub message");
            }
        });

        // ✅ Reconnection handler - reloads symbols from database
        client.ReconnectionHappened.Subscribe(async info =>
        {
            _logger.LogWarning("🔄 Reconnection happened, type: {Type}", info.Type);
            await Task.Delay(1000, stoppingToken);

            // ✅ Reload symbols from database on reconnection
            List<string> reconnectSymbols;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                reconnectSymbols = await db.ForexSymbols
                    .Where(s => s.IsActive)
                    .Select(s => s.Symbol!)
                    .ToListAsync(stoppingToken);
            }

            // Also include symbols currently in cache
            var allSymbols = reconnectSymbols.Concat(Cache.Keys).Distinct().ToList();

            _logger.LogInformation("Resubscribing to {Count} symbols from database...", allSymbols.Count);

            foreach (var symbol in allSymbols)
            {
                try
                {
                    client.Send($"{{\"type\":\"subscribe\",\"symbol\":\"{symbol}\"}}");
                    _logger.LogDebug("Resubscribed to {Symbol}", symbol);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resubscribe to {Symbol}", symbol);
                }
            }
        });

        client.DisconnectionHappened.Subscribe(info =>
        {
            _logger.LogWarning("⚠️ Disconnection happened, type: {Type}", info.Type);
        });

        await client.Start();

        _logger.LogInformation("✅ WebSocket connected. Subscribing to {Count} currency pairs from database...", symbols.Count);

        // ✅ Subscribe to all symbols from database
        foreach (var symbol in symbols)
        {
            try
            {
                client.Send($"{{\"type\":\"subscribe\",\"symbol\":\"{symbol}\"}}");
                _logger.LogInformation("Subscribed to {Symbol}", symbol);
                await Task.Delay(50, stoppingToken); // Small delay between subscriptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to {Symbol}", symbol);
            }
        }

        _logger.LogInformation("🎉 FinnhubIngestService started. Subscribed to {Count} currency pairs from database.", symbols.Count);

        // Keep service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}