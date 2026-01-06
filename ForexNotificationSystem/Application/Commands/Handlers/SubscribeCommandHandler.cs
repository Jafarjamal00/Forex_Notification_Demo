using ForexNotificationSystem.Data;
using ForexNotificationSystem.Models;
using MediatR;

namespace ForexNotificationSystem.Application.Commands.Handlers
{
    public class SubscribeCommandHandler : IRequestHandler<SubscribeCommand>
    {
        private readonly AppDbContext _db;

        public SubscribeCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(SubscribeCommand request, CancellationToken cancellationToken)
        {
            _db.SubscriptionAudits.Add(new SubscriptionAudit
            {
                UserId = request.UserId,
                Symbol = request.Symbol,
                Action = "SUBSCRIBE",
                At = DateTime.Now
            });

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}