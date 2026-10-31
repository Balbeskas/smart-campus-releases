using Campus.Canteen.Payments.Domain;
using Campus.Canteen.Payments.Persistence;

namespace Campus.Canteen.Sanity;

/// <summary>Проверка двойной записи суммы на фазах Expand and Contract.</summary>
public class CheckAmountTests
{
    [Fact]
    public void DualWrite_fills_old_and_new_columns()
    {
        var check = new CanteenCheck();
        CheckAmount.Write(check, 250m, AmountColumnMode.DualWrite);
        Assert.Equal(250m, check.Amount);
        Assert.Equal(250m, check.LegacyPrice);
    }

    [Fact]
    public void DualWrite_reads_legacy_price_for_not_migrated_row()
    {
        var historical = new CanteenCheck { Amount = null, LegacyPrice = 180m };
        Assert.Equal(180m, CheckAmount.Read(historical, AmountColumnMode.DualWrite));
    }

    [Fact]
    public void AmountOnly_does_not_touch_legacy_column()
    {
        var check = new CanteenCheck();
        CheckAmount.Write(check, 99m, AmountColumnMode.AmountOnly);
        Assert.Equal(99m, check.Amount);
        Assert.Null(check.LegacyPrice);
    }
}
