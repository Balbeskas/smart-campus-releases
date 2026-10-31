using Campus.Canteen.Payments.Domain;

namespace Campus.Canteen.Payments.Persistence;

/// <summary>
/// Режим работы с суммой чека на разных фазах Expand and Contract.
/// Задается в конфигурации: CanteenMigration:AmountMode.
/// </summary>
public enum AmountColumnMode
{
    /// <summary>Фаза Expand/Migrate: пишем в price и amount, читаем amount с запасным price.</summary>
    DualWrite,

    /// <summary>После Contract: колонки price в БД больше нет, работаем только с amount.</summary>
    AmountOnly
}

/// <summary>
/// Единая точка записи и чтения суммы чека. Контроллер не знает, сколько колонок
/// сейчас в таблице, — это решает режим, выбранный на время релиза.
/// </summary>
public static class CheckAmount
{
    public static void Write(CanteenCheck check, decimal amount, AmountColumnMode mode)
    {
        check.Amount = amount;                 // новая структура
        if (mode == AmountColumnMode.DualWrite)
            check.LegacyPrice = amount;        // старая структура — для экземпляров v1.8.x и отчетов бухгалтерии
    }

    public static decimal Read(CanteenCheck check, AmountColumnMode mode) =>
        mode == AmountColumnMode.DualWrite
            ? check.Amount ?? check.LegacyPrice ?? 0m   // строка еще не перенесена 02_migrate.sql
            : check.Amount ?? 0m;
}
