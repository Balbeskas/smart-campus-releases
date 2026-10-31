namespace Campus.Canteen.Payments.Domain;

/// <summary>Кассовый чек столовой кампуса (таблица canteen_checks).</summary>
public class CanteenCheck
{
    public Guid Id { get; set; }

    /// <summary>Номер студенческой карты, с которой списана оплата.</summary>
    public string StudentCard { get; set; } = string.Empty;

    /// <summary>Идентификатор кассира (АРМ столовой).</summary>
    public string CashierId { get; set; } = string.Empty;

    /// <summary>Состав заказа, например «Борщ; Котлета с пюре; Компот».</summary>
    public string Items { get; set; } = string.Empty;

    /// <summary>
    /// Сумма чека — НОВАЯ колонка amount (CAMPUS-104).
    /// До выполнения 02_migrate.sql у исторических строк она пустая (NULL).
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Устаревшая колонка price. Нужна только на время перехода (фаза Expand),
    /// после 03_contract.sql удаляется из БД и не попадает в модель EF.
    /// </summary>
    public decimal? LegacyPrice { get; set; }

    public CheckStatus Status { get; set; } = CheckStatus.Paid;

    public DateTimeOffset CreatedAt { get; set; }
}

public enum CheckStatus
{
    Paid = 1,
    Refunded = 2
}
