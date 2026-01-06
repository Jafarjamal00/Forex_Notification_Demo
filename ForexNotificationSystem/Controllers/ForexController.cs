using ForexNotificationSystem.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForexNotificationSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ForexController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ForexController> _logger;

        public ForexController(IMediator mediator, ILogger<ForexController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("recent-ticks")]
        public async Task<IActionResult> GetRecentTicks([FromQuery] string symbol, [FromQuery] int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest(new { message = "Symbol is required" });

            _logger.LogInformation("Getting recent ticks for {Symbol}, limit: {Limit}", symbol, limit);

            var query = new GetRecentTicksQuery(symbol, limit);
            var result = await _mediator.Send(query);

            _logger.LogInformation("Retrieved {Count} ticks for {Symbol}", result.Count, symbol);

            return Ok(result);
        }

        [HttpGet("symbols")]
        public IActionResult GetAvailableSymbols()
        {
            var symbols = FinnhubIngestService.Cache.Keys.ToList();

            _logger.LogInformation("Retrieved {Count} available symbols", symbols.Count);

            return Ok(new { count = symbols.Count, symbols = symbols });
        }

        [HttpGet("current-price")]  
        public IActionResult GetCurrentPrice([FromQuery] string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest(new { message = "Symbol is required" });

            if (FinnhubIngestService.Cache.TryGetValue(symbol, out var priceTick))
            {
                _logger.LogInformation("Retrieved current price for {Symbol}: {Price}", symbol, priceTick.price);
                return Ok(priceTick);
            }

            _logger.LogWarning("Symbol {Symbol} not found in cache", symbol);
            return NotFound(new { message = $"Symbol {symbol} not found" });
        }
    }
}