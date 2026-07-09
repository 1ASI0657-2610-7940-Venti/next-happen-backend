using System.Security.Cryptography;
using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;

namespace NextHappen.Ticket.Application.Services;

/// <summary>
/// Responsable del ciclo de vida de las entradas: emisión (a partir de un pedido
/// pagado), consulta y validación en la puerta.
/// </summary>
public interface ITicketService
{
    Task<List<Domain.Entities.Ticket>> IssueTicketsForOrderAsync(Order order);
}

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepo;

    public TicketService(ITicketRepository ticketRepo)
    {
        _ticketRepo = ticketRepo;
    }

    /// <summary>
    /// Emite las entradas de un pedido pagado. Genera un token QR único por entrada.
    /// </summary>
    public async Task<List<Domain.Entities.Ticket>> IssueTicketsForOrderAsync(Order order)
    {
        var tickets = new List<Domain.Entities.Ticket>();
        var batchCodes = new HashSet<string>();
        for (int i = 0; i < order.Quantity; i++)
        {
            tickets.Add(new Domain.Entities.Ticket
            {
                Id = Guid.NewGuid(),
                UserId = order.UserId,
                EventId = order.EventId,
                OrderId = order.Id,
                Price = order.UnitPrice,
                QrCode = GenerateQrToken(),
                ShortCode = await GenerateUniqueShortCodeAsync(batchCodes),
                PurchaseDate = order.PaidAt ?? DateTime.UtcNow,
                Status = TicketStatus.Active
            });
        }

        await _ticketRepo.AddRangeAsync(tickets);
        return tickets;
    }

    public Task<List<Domain.Entities.Ticket>> GetByUserAsync(Guid userId)
        => _ticketRepo.GetByUserIdAsync(userId);

    /// <summary>Lista de entradas de un evento para el panel del organizador (sin exponer el QR).</summary>
    public async Task<List<EventTicketRow>> GetEventTicketsAsync(Guid eventId)
    {
        var tickets = await _ticketRepo.GetByEventIdAsync(eventId);
        return tickets
            .OrderByDescending(t => t.PurchaseDate)
            .Select(t => new EventTicketRow
            {
                Id = t.Id,
                ShortCode = t.ShortCode,
                Status = t.Status,
                Price = t.Price,
                PurchaseDate = t.PurchaseDate,
                ValidatedAt = t.ValidatedAt
            })
            .ToList();
    }

    public Task<Domain.Entities.Ticket?> GetByIdAsync(Guid ticketId)
        => _ticketRepo.GetByIdAsync(ticketId);

    /// <summary>
    /// Valida una entrada por su token QR. Marca la entrada como usada la primera vez
    /// e impide reingresos o el uso de entradas reembolsadas/canceladas.
    /// </summary>
    public async Task<ValidateTicketResponse> ValidateAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ValidateTicketResponse { Valid = false, Message = "Ingresa un código." };

        var raw = code.Trim();

        // Acepta tanto el token del QR ("NH-...") como el código corto legible ("7K4-P9Q").
        var ticket = await _ticketRepo.GetByQrCodeAsync(raw);
        if (ticket is null)
        {
            var normalized = NormalizeShortCode(raw);
            if (normalized.Length > 0)
                ticket = await _ticketRepo.GetByShortCodeAsync(normalized);
        }

        if (ticket is null)
            return new ValidateTicketResponse { Valid = false, Message = "Entrada no encontrada." };

        var baseResp = new ValidateTicketResponse
        {
            TicketId = ticket.Id,
            EventId = ticket.EventId,
            Status = ticket.Status
        };

        switch (ticket.Status)
        {
            case TicketStatus.Used:
                baseResp.Valid = false;
                baseResp.Message = $"Entrada ya utilizada el {ticket.ValidatedAt:dd/MM/yyyy HH:mm}.";
                return baseResp;

            case TicketStatus.Refunded:
                baseResp.Valid = false;
                baseResp.Message = "Entrada reembolsada; no es válida para el ingreso.";
                return baseResp;

            case TicketStatus.Cancelled:
                baseResp.Valid = false;
                baseResp.Message = "Entrada cancelada; no es válida para el ingreso.";
                return baseResp;

            case TicketStatus.Active:
                ticket.Status = TicketStatus.Used;
                ticket.ValidatedAt = DateTime.UtcNow;
                await _ticketRepo.UpdateAsync(ticket);
                baseResp.Valid = true;
                baseResp.Status = TicketStatus.Used;
                baseResp.Message = "Ingreso permitido.";
                return baseResp;

            default:
                baseResp.Valid = false;
                baseResp.Message = "Estado de entrada desconocido.";
                return baseResp;
        }
    }

    /// <summary>Genera un token opaco e imposible de adivinar para el QR.</summary>
    private static string GenerateQrToken()
        => "NH-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20));

    // Alfabeto sin caracteres ambiguos (sin O/0, I/1, etc.) para dictar por voz sin errores.
    private const string ShortCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int ShortCodeLength = 6;

    /// <summary>Genera un código corto único (no colisiona con la BD ni dentro del mismo pedido).</summary>
    private async Task<string> GenerateUniqueShortCodeAsync(HashSet<string> batchCodes)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var candidate = GenerateShortCode();
            if (batchCodes.Contains(candidate)) continue;
            if (await _ticketRepo.ShortCodeExistsAsync(candidate)) continue;
            batchCodes.Add(candidate);
            return candidate;
        }
        // Fallback extremadamente improbable: añade entropía para garantizar unicidad.
        var fallback = GenerateShortCode() + Convert.ToHexString(RandomNumberGenerator.GetBytes(2));
        batchCodes.Add(fallback);
        return fallback;
    }

    private static string GenerateShortCode()
    {
        var chars = new char[ShortCodeLength];
        Span<byte> bytes = stackalloc byte[ShortCodeLength];
        RandomNumberGenerator.Fill(bytes);
        for (int i = 0; i < ShortCodeLength; i++)
            chars[i] = ShortCodeAlphabet[bytes[i] % ShortCodeAlphabet.Length];
        return new string(chars);
    }

    /// <summary>Normaliza la entrada del organizador: mayúsculas y solo caracteres del alfabeto.</summary>
    private static string NormalizeShortCode(string input)
    {
        var upper = input.ToUpperInvariant();
        var sb = new System.Text.StringBuilder(upper.Length);
        foreach (var c in upper)
            if (ShortCodeAlphabet.IndexOf(c) >= 0) sb.Append(c);
        return sb.ToString();
    }
}
