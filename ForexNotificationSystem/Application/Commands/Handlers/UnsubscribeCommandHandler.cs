using ForexNotificationSystem.Data;
using ForexNotificationSystem.Models;
using MediatR;

namespace ForexNotificationSystem.Application.Commands.Handlers
{
    public class UnsubscribeCommandHandler : IRequestHandler<UnsubscribeCommand>
    {
        private readonly AppDbContext _db;

        public UnsubscribeCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(UnsubscribeCommand request, CancellationToken cancellationToken)
        {
            _db.SubscriptionAudits.Add(new SubscriptionAudit
            {
                UserId = request.UserId,
                Symbol = request.Symbol,
                Action = "UNSUBSCRIBE",
                At = DateTime.Now
            });

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}