using IntentVsProcess.Domain;

namespace IntentVsProcess.Free;

// ─── Post 3: Execution plan analysis — SQL EXPLAIN for business logic ─

/// <summary>
/// An execution plan describing what a program WOULD do without running it.
/// "Show this to your boss. Show this to your compliance team."
/// </summary>
public record ExecutionPlan
{
    public int DatabaseCalls { get; init; }
    public int PaymentApiCalls { get; init; }
    public int EmailCalls { get; init; }
    public TimeSpan EstimatedLatency { get; init; }
    public decimal EstimatedCost { get; init; }
    public List<string> Steps { get; init; } = [];

    public override string ToString()
    {
        var stepList = string.Join("\n", Steps.Select((s, i) => $"  {i + 1}. {s}"));
        return $"""
            Execution Plan
            ──────────────
            DB calls: {DatabaseCalls}
            Payment calls: {PaymentApiCalls}
            Email calls: {EmailCalls}
            Estimated latency: {EstimatedLatency.TotalMilliseconds}ms
            Estimated cost: ${EstimatedCost:F3}
            Steps:
            {stepList}
            """;
    }
}

public static class Optimizers
{
    /// <summary>
    /// Walk the program AST and produce an execution plan without running anything.
    /// Counts DB calls, payment API calls, email calls, estimates latency and cost.
    /// </summary>
    public static ExecutionPlan Analyze<T>(OrderProgram<T> program)
    {
        var steps = StructuralHelpers.Flatten(program);

        int dbCalls = 0, paymentCalls = 0, emailCalls = 0;
        double latencyMs = 0;
        decimal cost = 0m;
        var descriptions = new List<string>();

        foreach (var step in steps)
        {
            bool compensatable = step is ICompensatable;
            var effective = step is ICompensatable comp ? comp.ForwardStep : step;
            string tag = compensatable ? " [compensatable]" : "";

            switch (effective)
            {
                case CheckStock cs:
                    dbCalls++;
                    latencyMs += 50;
                    descriptions.Add($"DB: Check stock for {cs.Items.Count} item(s){tag}");
                    break;

                case CalculatePrice cp:
                    latencyMs += 10;
                    descriptions.Add($"Compute: Calculate price for {cp.Items.Count} item(s){tag}");
                    break;

                case ChargePayment ch:
                    paymentCalls++;
                    latencyMs += 800;
                    cost += 0.03m;
                    descriptions.Add($"API: Charge {ch.Amount:C} via {ch.Method}{tag}");
                    break;

                case ReserveInventory ri:
                    dbCalls++;
                    latencyMs += 50;
                    descriptions.Add($"DB: Reserve {ri.Items.Count} item(s){tag}");
                    break;

                case SendConfirmation sc:
                    emailCalls++;
                    latencyMs += 100;
                    cost += 0.001m;
                    descriptions.Add($"Email: Confirm to {sc.Customer.Email}{tag}");
                    break;

                default:
                    descriptions.Add($"Other: {effective.GetType().Name}{tag}");
                    break;
            }
        }

        return new ExecutionPlan
        {
            DatabaseCalls = dbCalls,
            PaymentApiCalls = paymentCalls,
            EmailCalls = emailCalls,
            EstimatedLatency = TimeSpan.FromMilliseconds(latencyMs),
            EstimatedCost = cost,
            Steps = descriptions
        };
    }
}
