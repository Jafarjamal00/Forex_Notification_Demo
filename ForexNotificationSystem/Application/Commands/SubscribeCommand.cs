using MediatR;

namespace ForexNotificationSystem.Application.Commands
{
    public record SubscribeCommand(string UserId, string Symbol) : IRequest;
}