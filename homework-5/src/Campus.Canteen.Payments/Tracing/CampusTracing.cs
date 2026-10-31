using System.Diagnostics;

namespace Campus.Canteen.Payments.Tracing;

/// <summary>
/// Единый цифровой след (Digital Traceability): каждая бизнес-операция столовой
/// получает теги задачи Jira и доменного идентификатора, по которым инженер
/// сопровождения находит трассу в Jaeger/Grafana Tempo по номеру чека или задачи.
/// </summary>
public static class CampusTracing
{
    public const string SourceName = "Campus.Canteen.Payments";
    public const string JiraIssue = "CAMPUS-104";

    public static readonly ActivitySource Source = new(SourceName, "1.9.0");

    /// <summary>
    /// Помечает текущий HTTP-спан (Activity.Current, создан инструментированием ASP.NET Core)
    /// и открывает дочерний доменный спан операции.
    /// </summary>
    public static Activity? StartOperation(string operation, Guid checkId, string? cashierId = null)
    {
        Activity.Current?.SetTag("jira.issue", JiraIssue);
        Activity.Current?.SetTag("campus.check_id", checkId.ToString());

        var activity = Source.StartActivity($"canteen.{operation}");
        activity?.SetTag("jira.issue", JiraIssue);
        activity?.SetTag("campus.check_id", checkId.ToString());
        activity?.SetTag("campus.module", "canteen");
        if (cashierId is not null)
            activity?.SetTag("campus.cashier_id", cashierId);
        return activity;
    }
}
