using ForexNotificationSystem.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System.Security.Claims;

[Authorize]
public class ForexHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ILogger<ForexHub> _logger;

    public ForexHub(IMediator mediator, ILogger<ForexHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";

        _logger.LogInformation("User {UserId} connected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";

        _logger.LogInformation("User {UserId} disconnected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Disconnection due to error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Subscribe(string symbol)
    {
        var userId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;

        _logger.LogInformation("User {UserId} SUBSCRIBING to {Symbol}", userId, symbol);

        await Groups.AddToGroupAsync(Context.ConnectionId, symbol);

        await _mediator.Send(new SubscribeCommand(userId, symbol));

        await Clients.Caller.SendAsync("subscribed", new { symbol, status = "success" });

        _logger.LogInformation("User {UserId} successfully SUBSCRIBED to {Symbol}", userId, symbol);
    }

    public async Task Unsubscribe(string symbol)
    {
        var userId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;

        _logger.LogInformation("User {UserId} UNSUBSCRIBING from {Symbol}", userId, symbol);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol);

        await _mediator.Send(new UnsubscribeCommand(userId, symbol));

        await Clients.Caller.SendAsync("unsubscribed", new { symbol, status = "success" });

        _logger.LogInformation("User {UserId} successfully UNSUBSCRIBED from {Symbol}", userId, symbol);
    }
}