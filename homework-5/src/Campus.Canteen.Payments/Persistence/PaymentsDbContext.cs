using Campus.Canteen.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Campus.Canteen.Payments.Persistence;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options, AmountColumnMode mode) : base(options)
    {
        Mode = mode;
    }

    public AmountColumnMode Mode { get; }

    public DbSet<CanteenCheck> Checks => Set<CanteenCheck>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var e = b.Entity<CanteenCheck>();
        e.ToTable("canteen_checks");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.StudentCard).HasColumnName("student_card").HasMaxLength(32);
        e.Property(x => x.CashierId).HasColumnName("cashier_id").HasMaxLength(32);
        e.Property(x => x.Items).HasColumnName("items");
        e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(10, 2);
        e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");

        // Колонка price существует только до фазы Contract
        if (Mode == AmountColumnMode.DualWrite)
            e.Property(x => x.LegacyPrice).HasColumnName("price").HasPrecision(10, 2);
        else
            e.Ignore(x => x.LegacyPrice);
    }
}

/// <summary>EF кэширует модель; ключ кэша включает режим, чтобы модели DualWrite и AmountOnly не смешивались.</summary>
public class AmountModeCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), (context as PaymentsDbContext)?.Mode, designTime);
}
