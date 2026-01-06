using MediatR;

namespace ForexNotificationSystem.Application.Commands
{
    public record UnsubscribeCommand(string UserId, string Symbol) : IRequest;
}