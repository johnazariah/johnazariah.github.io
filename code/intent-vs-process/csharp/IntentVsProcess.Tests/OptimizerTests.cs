using IntentVsProcess.Domain;
using IntentVsProcess.Free;
using Xunit;

namespace IntentVsProcess.Tests;

// ─── Post 3: Optimizer tests ──────────────────────────────────────────
// The execution plan analyzer — "SQL EXPLAIN for your business logic."

public class OptimizerTests
{
    private static readonly List<Item> TestItems =
    [
        new("SKU-001", "Widget", 2),
        new("SKU-002", "Gadget", 1)
    ];

    private static readonly Customer TestCustomer = new("Alice", "alice@example.com");

    private static readonly OrderRequest HappyRequest = new(
        Items: TestItems,
        Customer: TestCustomer,
        PaymentMethod: PaymentMethod.CreditCard);

    [Fact]
    public void Analyze_CountsDatabaseCalls()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var plan = Optimizers.Analyze(program);

        // CheckStock + ReserveInventory = 2 DB calls
        Assert.Equal(2, plan.DatabaseCalls);
    }

    [Fact]
    public void Analyze_CountsPaymentCalls()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var plan = Optimizers.Analyze(program);

        Assert.Equal(1, plan.PaymentApiCalls);
    }

    [Fact]
    public void Analyze_CountsEmailCalls()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var plan = Optimizers.Analyze(program);

        Assert.Equal(1, plan.EmailCalls);
    }

    [Fact]
    public void Analyze_EstimatesNonZeroLatency()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var plan = Optimizers.Analyze(program);

        Assert.True(plan.EstimatedLatency > TimeSpan.Zero);
    }

    [Fact]
    public void Analyze_EstimatesNonZeroCost()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var plan = Optimizers.Analyze(program);

        Assert.True(plan.EstimatedCost > 0m);
    }

    [Fact]
    public void Analyze_ListsAllSteps()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var plan = Optimizers.Analyze(program);

        Assert.Equal(5, plan.Steps.Count);
        Assert.Contains(plan.Steps, s => s.Contains("Check stock"));
        Assert.Contains(plan.Steps, s => s.Contains("Calculate price"));
        Assert.Contains(plan.Steps, s => s.Contains("Charge"));
        Assert.Contains(plan.Steps, s => s.Contains("Reserve"));
        Assert.Contains(plan.Steps, s => s.Contains("Confirm"));
    }

    [Fact]
    public void Analyze_WithCompensation_MarksCompensatableSteps()
    {
        var program = Free.OrderPrograms.PlaceOrderWithCompensation(HappyRequest);
        var plan = Optimizers.Analyze(program);

        Assert.Contains(plan.Steps, s => s.Contains("[compensatable]"));
    }

    [Fact]
    public void ExecutionPlan_ToStringIsReadable()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var plan = Optimizers.Analyze(program);

        var output = plan.ToString();
        Assert.Contains("Execution Plan", output);
        Assert.Contains("DB calls:", output);
        Assert.Contains("Payment calls:", output);
    }
}
