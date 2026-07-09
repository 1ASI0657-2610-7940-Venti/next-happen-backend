using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;

namespace NextHappen.Ticket.Application.Services;

/// <summary>
/// Agrega las ventas de entradas para el panel de métricas del organizador
/// (ingresos, entradas vendidas, validadas, reembolsos y desglose por día).
/// </summary>
public class SalesService
{
    private readonly ITicketRepository _ticketRepo;

    public SalesService(ITicketRepository ticketRepo)
    {
        _ticketRepo = ticketRepo;
    }

    public async Task<SalesSummary> GetForEventAsync(Guid eventId)
    {
        var tickets = await _ticketRepo.GetByEventIdAsync(eventId);
        return Aggregate(eventId, tickets);
    }

    public async Task<List<SalesSummary>> GetForEventsAsync(IEnumerable<Guid> eventIds)
    {
        var ids = eventIds.Distinct().ToList();
        var tickets = await _ticketRepo.GetByEventIdsAsync(ids);
        var grouped = tickets.GroupBy(t => t.EventId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return ids.Select(id =>
            Aggregate(id, grouped.TryGetValue(id, out var list) ? list : new List<Domain.Entities.Ticket>())
        ).ToList();
    }

    private static SalesSummary Aggregate(Guid eventId, List<Domain.Entities.Ticket> tickets)
    {
        // Vendidas = todas menos las reembolsadas/canceladas (esas no cuentan como ingreso).
        var sold = tickets.Where(t => t.Status != TicketStatus.Refunded && t.Status != TicketStatus.Cancelled).ToList();
        var refunded = tickets.Where(t => t.Status == TicketStatus.Refunded).ToList();

        var byDay = sold
            .GroupBy(t => t.PurchaseDate.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new DailySales
            {
                Date = g.Key,
                Tickets = g.Count(),
                Revenue = g.Sum(t => t.Price)
            })
            .ToList();

        return new SalesSummary
        {
            EventId = eventId,
            TicketsSold = sold.Count,
            TicketsValidated = sold.Count(t => t.Status == TicketStatus.Used),
            TicketsRefunded = refunded.Count,
            GrossRevenue = sold.Sum(t => t.Price),
            RefundedAmount = refunded.Sum(t => t.Price),
            NetRevenue = sold.Sum(t => t.Price),
            ByDay = byDay
        };
    }
}
