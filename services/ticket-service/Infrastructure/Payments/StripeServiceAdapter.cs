using Stripe;
using Stripe.Checkout;

namespace NextHappen.Ticket.Infrastructure.Payments;

public class StripeSessionService : ISessionService
{
    private readonly SessionService _inner = new();

    public Task<Session> CreateAsync(SessionCreateOptions options)
        => _inner.CreateAsync(options);

    public Task<Session> GetAsync(string sessionId)
        => _inner.GetAsync(sessionId);
}

public class StripeRefundServiceAdapter : IRefundService
{
    private readonly RefundService _inner = new();

    public Task<Refund> CreateAsync(RefundCreateOptions options)
        => _inner.CreateAsync(options);
}