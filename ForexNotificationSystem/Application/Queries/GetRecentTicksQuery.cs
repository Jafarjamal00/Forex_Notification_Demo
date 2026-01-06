using ForexNotificationSystem.Models;
using MediatR;

namespace ForexNotificationSystem.Application.Queries
{
    public record GetRecentTicksQuery(string Symbol, int Limit = 50) : IRequest<List<PriceTick>>;
}