using IntentVsProcess.Domain;
using IntentVsProcess.Free;
using Xunit;
using static IntentVsProcess.Free.OrderProgramExtensions;

namespace IntentVsProcess.Tests;

// ─── Post 3: Free Monad tests ─────────────────────────────────────────
// Two kinds of tests that were impossible before:
//   1. Behavioral: run with different executors (like interpreters)
//   2. Structural: test the SHAPE of the program without running it

public class FreeMonadTests
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

    // ── Behavioral tests: run with an executor ──────────────────

    [Fact]
    public void PlaceOrder_HappyPath_Succeeds()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);

        var result = OrderInterpreter.RunSync(
            program, OrderInterpreter.DefaultTestExecutor);

        Assert.IsType<OrderResult.Success>(result);
        Assert.Equal("txn-test-001", ((OrderResult.Success)result).TransactionId);
    }

    [Fact]
    public void PlaceOrder_OutOfStock_Throws()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);

        // Custom executor: stock not available
        object outOfStockExecutor(OrderStepBase step) => step switch
        {
            CheckStock => new StockResult(IsAvailable: false),
            _ => OrderInterpreter.DefaultTestExecutor(step)
        };

        var ex = Assert.Throws<OrderFailedException>(
            () => OrderInterpreter.RunSync(program, outOfStockExecutor));

        Assert.Equal("Guard condition failed", ex.Reason);
    }

    [Fact]
    public void PlaceOrder_PaymentFailed_Throws()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);

        object paymentFailedExecutor(OrderStepBase step) => step switch
        {
            ChargePayment => new ChargeResult(Succeeded: false),
            _ => OrderInterpreter.DefaultTestExecutor(step)
        };

        var ex = Assert.Throws<OrderFailedException>(
            () => OrderInterpreter.RunSync(program, paymentFailedExecutor));

        Assert.Equal("Guard condition failed", ex.Reason);
    }

    // ── Structural tests: test the shape without running ────────

    [Fact]
    public void PlaceOrder_HasFiveSteps()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        Assert.Equal(5, StructuralHelpers.CountSteps(program));
    }

    [Fact]
    public void PlaceOrder_ChecksStockFirst()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var steps = StructuralHelpers.StepNames(program);
        Assert.Equal("CheckStock", steps[0]);
    }

    [Fact]
    public void PlaceOrder_ChecksStockBeforeCharging()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);

        Assert.True(
            StructuralHelpers.AppearsBefore<CheckStock, ChargePayment, OrderResult>(program),
            "Must validate stock before charging");
    }

    [Fact]
    public void PlaceOrder_NeverChargesWithoutStockCheck()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);

        Assert.True(
            StructuralHelpers.NeverWithoutPreceding<ChargePayment, CheckStock, OrderResult>(program),
            "ChargePayment must be preceded by CheckStock");
    }

    [Fact]
    public void PlaceOrder_AlwaysSendsConfirmation()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);

        Assert.True(
            StructuralHelpers.AllPathsContain<SendConfirmation, OrderResult>(program),
            "Must send confirmation on happy path");
    }

    [Fact]
    public void PlaceOrder_StepsInCorrectOrder()
    {
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);
        var steps = StructuralHelpers.StepNames(program);

        Assert.Equal(
            ["CheckStock", "CalculatePrice", "ChargePayment", "ReserveInventory", "SendConfirmation"],
            steps);
    }

    // ── LINQ comprehension tests ────────────────────────────────

    [Fact]
    public void LINQProgram_IsADataStructure()
    {
        // The program is a VALUE — not an action
        var program = Free.OrderPrograms.PlaceOrder(HappyRequest);

        // It's a Bind node (the first step is CheckStock)
        Assert.IsType<Bind<OrderResult>>(program);
        var bind = (Bind<OrderResult>)program;
        Assert.IsType<CheckStock>(bind.Step);
    }

    [Fact]
    public void Select_TransformsResult()
    {
        var program = new Done<int>(42).Select(x => x * 2);
        var result = OrderInterpreter.RunSync(program, _ => throw new Exception());
        Assert.Equal(84, result);
    }

    [Fact]
    public void SelectMany_ChainsPrograms()
    {
        var program =
            from x in new Done<int>(10)
            from y in new Done<int>(20)
            select x + y;

        var result = OrderInterpreter.RunSync(program, _ => throw new Exception());
        Assert.Equal(30, result);
    }

    [Fact]
    public void Where_ShortCircuitsOnFalse()
    {
        var program =
            from x in new Done<int>(5)
            where x > 10  // guard fails
            select x;

        Assert.Throws<OrderFailedException>(
            () => OrderInterpreter.RunSync(program, _ => throw new Exception()));
    }

    [Fact]
    public void Where_ContinuesOnTrue()
    {
        var program =
            from x in new Done<int>(15)
            where x > 10  // guard passes
            select x;

        var result = OrderInterpreter.RunSync(program, _ => throw new Exception());
        Assert.Equal(15, result);
    }
}
