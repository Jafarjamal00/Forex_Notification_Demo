using ForexNotificationSystem.Data;
using ForexNotificationSystem.Models;
using Newtonsoft.Json.Linq;
using Serilog;
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

        _logger.LogInformation("Connecting to Finnhub WebSocket: {Url}", url);

        using var client = new WebsocketClient(url);

        var symbols = new List<string>
        {
            "OANDA:EUR_USD",
            "OANDA:GBP_USD",
            "OANDA:USD_JPY",
            "OANDA:USD_CHF",
            "OANDA:USD_CAD",
            "OANDA:AUD_USD",
            "OANDA:NZD_USD",
    
            "OANDA:EUR_GBP",
            "OANDA:EUR_JPY",
            "OANDA:EUR_CHF",
            "OANDA:EUR_AUD",
            "OANDA:EUR_CAD",
    
            "OANDA:GBP_JPY",
            "OANDA:GBP_CHF",
            "OANDA:GBP_AUD",
    
            "OANDA:AUD_JPY",
            "OANDA:AUD_CAD",
            "OANDA:CAD_JPY",
            "OANDA:CHF_JPY",
            "OANDA:NZD_JPY",
    
            "OANDA:USD_SEK",
            "OANDA:USD_NOK",
            "OANDA:USD_DKK",
            "OANDA:USD_ZAR",
            "OANDA:USD_SGD",
    
            "OANDA:USD_INR",
            "OANDA:USD_MXN",
            "OANDA:USD_BRL",
            "OANDA:USD_TRY",
            "OANDA:USD_PLN",
        };

        client.MessageReceived.Subscribe(async msg =>
        {
            try
            {
                var json = JObject.Parse(msg.Text);

                if (json["type"]?.ToString() != "trade" || json["data"] == null)
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
                }

                _logger.LogDebug("Processed {Count} ticks. Cache size: {CacheSize}", ticksToSave.Count, Cache.Count);

                if (_tickCounter >= SAVE_BATCH_SIZE && ticksToSave.Count > 0)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await db.PriceTicks.AddRangeAsync(ticksToSave, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Saved batch of {Count} ticks to database", ticksToSave.Count);
                    _tickCounter = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Finnhub message");
            }
        });

        // Handle reconnection - resubscribe to all symbols
        client.ReconnectionHappened.Subscribe(async info =>
        {
            _logger.LogInformation("Reconnection happened, type: {Type}", info.Type);

            await Task.Delay(1000, stoppingToken);

            foreach (var symbol in symbols)
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
            _logger.LogWarning("Disconnection happened, type: {Type}", info.Type);
        });

        await client.Start();

        _logger.LogInformation("Subscribing to {Count} currency pairs...", symbols.Count);

        // Subscribe to all symbols
        foreach (var symbol in symbols)
        {
            client.Send($"{{\"type\":\"subscribe\",\"symbol\":\"{symbol}\"}}");
            _logger.LogDebug("Subscribed to {Symbol}", symbol);
        }

        _logger.LogInformation("FinnhubIngestService started. Subscribed to {Count} currency pairs", symbols.Count);

        // Keep service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}