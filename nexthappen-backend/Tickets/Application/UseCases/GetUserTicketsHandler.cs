using nexthappen_backend.Tickets.Domain;
using nexthappen_backend.Tickets.Domain.Entities;

namespace nexthappen_backend.Tickets.Application.UseCases;

public class GetUserTicketsHandler
{
    private readonly ITicketRepository _repository;

    public GetUserTicketsHandler(ITicketRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Ticket>> Handle(Guid userId)
        => _repository.GetByUserIdAsync(userId);
}