using System.Diagnostics;
using Campus.Canteen.Payments.Domain;
using Campus.Canteen.Payments.Persistence;
using Campus.Canteen.Payments.Tracing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Campus.Canteen.Payments.Controllers;

public record PayRequest(string StudentCard, string CashierId, string Items, decimal Amount);

public record CheckDto(Guid Id, string StudentCard, string CashierId, string Items, decimal Amount, string Status, DateTimeOffset CreatedAt);

/// <summary>Безналичная оплата в столовой кампуса: оплата, просмотр, возврат чека.</summary>
[ApiController]
[Route("api/v1/checks")]
public class CheckoutController : ControllerBase
{
    /// <summary>Префикс технических карт sanity-тестов: только их чеки разрешено удалять.</summary>
    public const string SanityCardPrefix = "SANITY-";

    private readonly PaymentsDbContext _db;
    private readonly ILogger<CheckoutController> _log;

    public CheckoutController(PaymentsDbContext db, ILogger<CheckoutController> log)
    {
        _db = db;
        _log = log;
    }

    [HttpPost]
    public async Task<ActionResult<CheckDto>> Pay(PayRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0 || req.Amount > 5000)
            return ValidationProblem("Сумма чека должна быть в диапазоне 0,01–5000 руб.");
        if (string.IsNullOrWhiteSpace(req.StudentCard))
            return ValidationProblem("Не указан номер студенческой карты.");

        var check = new CanteenCheck
        {
            Id = Guid.NewGuid(),
            StudentCard = req.StudentCard,
            CashierId = req.CashierId,
            Items = req.Items,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var op = CampusTracing.StartOperation("pay", check.Id, req.CashierId);
        CheckAmount.Write(check, req.Amount, _db.Mode);   // двойная запись price + amount в фазе Expand
        op?.SetTag("campus.amount_mode", _db.Mode.ToString());

        _db.Checks.Add(check);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("[{Jira}] Чек {CheckId} оплачен: {Amount} руб., кассир {Cashier}",
            CampusTracing.JiraIssue, check.Id, req.Amount, req.CashierId);
        return CreatedAtAction(nameof(Get), new { id = check.Id }, ToDto(check));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CheckDto>> Get(Guid id, CancellationToken ct)
    {
        using var op = CampusTracing.StartOperation("get", id);
        var check = await _db.Checks.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (check is null)
        {
            op?.SetStatus(ActivityStatusCode.Error, "check not found");
            return NotFound();
        }
        return ToDto(check);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<ActionResult<CheckDto>> Refund(Guid id, CancellationToken ct)
    {
        using var op = CampusTracing.StartOperation("refund", id);
        var check = await _db.Checks.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (check is null) return NotFound();
        if (check.Status == CheckStatus.Refunded) return Conflict("Чек уже возвращен.");

        check.Status = CheckStatus.Refunded;
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("[{Jira}] Возврат по чеку {CheckId}", CampusTracing.JiraIssue, id);
        return ToDto(check);
    }

    /// <summary>Удаление чека — только для технических карт sanity-тестов (Cleanup после проверки релиза).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSanityCheck(Guid id, CancellationToken ct)
    {
        using var op = CampusTracing.StartOperation("cleanup", id);
        var check = await _db.Checks.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (check is null) return NotFound();
        if (!check.StudentCard.StartsWith(SanityCardPrefix, StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status403Forbidden, "Удалять можно только технические чеки sanity-тестов.");
        if (check.Status != CheckStatus.Refunded)
            return Conflict("Перед удалением по чеку должен быть выполнен возврат.");

        _db.Checks.Remove(check);
        await _db.SaveChangesAsync(ct);
        op?.SetTag("campus.sanity_cleanup", true);
        return NoContent();
    }

    private CheckDto ToDto(CanteenCheck c) =>
        new(c.Id, c.StudentCard, c.CashierId, c.Items, CheckAmount.Read(c, _db.Mode), c.Status.ToString(), c.CreatedAt);
}
