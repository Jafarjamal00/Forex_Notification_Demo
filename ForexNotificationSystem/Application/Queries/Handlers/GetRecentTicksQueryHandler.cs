using ForexNotificationSystem.Data;
using ForexNotificationSystem.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForexNotificationSystem.Application.Queries.Handlers
{
    public class GetRecentTicksQueryHandler : IRequestHandler<GetRecentTicksQuery, List<PriceTick>>
    {
        private readonly AppDbContext _db;

        public GetRecentTicksQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<PriceTick>> Handle(GetRecentTicksQuery request, CancellationToken cancellationToken)
        {
            return await _db.PriceTicks
                .Where(x => x.symbol == request.Symbol)
                .OrderByDescending(x => x.ts)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);
        }
    }
}