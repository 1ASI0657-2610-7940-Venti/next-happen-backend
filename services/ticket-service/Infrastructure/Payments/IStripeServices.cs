using Stripe;
using Stripe.Checkout;

namespace NextHappen.Ticket.Infrastructure.Payments;

public interface ISessionService
{
    Task<Session> CreateAsync(SessionCreateOptions options);
    Task<Session> GetAsync(string sessionId);
}

public interface IRefundService
{
    Task<Refund> CreateAsync(RefundCreateOptions options);
}