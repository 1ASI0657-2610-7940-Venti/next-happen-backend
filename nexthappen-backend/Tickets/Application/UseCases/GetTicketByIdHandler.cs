using nexthappen_backend.Tickets.Domain;
using nexthappen_backend.Tickets.Domain.Entities;

namespace nexthappen_backend.Tickets.Application.UseCases;

public class GetTicketByIdHandler
{
    private readonly ITicketRepository _repo;

    public GetTicketByIdHandler(ITicketRepository repo)
    {
        _repo = repo;
    }

    public Task<Ticket?> Handle(Guid ticketId)
        => _repo.GetByIdAsync(ticketId);
}