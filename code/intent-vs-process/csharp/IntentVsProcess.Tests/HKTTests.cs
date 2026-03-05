using IntentVsProcess.Domain;
using IntentVsProcess.HKT;
using Xunit;

namespace IntentVsProcess.Tests;

// ─── Post 4: HKT brand pattern tests ─────────────────────────────────
// Demonstrates the type-safe alternative to Eval-based Tagless Final.
// Same tests, no casts (the brand pattern ensures type safety).

public class HKTTests
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
    public void PlaceOrder_HappyPath_Succeeds()
    {
        var interpreter = new TestInterpreterHKT(
            stockAvailable: true,
            price: 99.50m,
            chargeSucceeds: true,
            transactionId: "txn-hkt-001");

        var result = OrderProgramsHKT.PlaceOrder(interpreter, HappyRequest);
        var orderResult = result.Unwrap();

        Assert.IsType<OrderResult.Success>(orderResult);
        Assert.Equal("txn-hkt-001", ((OrderResult.Success)orderResult).TransactionId);
    }

    [Fact]
    public void PlaceOrder_OutOfStock_Throws()
    {
        var interpreter = new TestInterpreterHKT(stockAvailable: false);

        Assert.Throws<OrderFailedException>(
            () => OrderProgramsHKT.PlaceOrder(interpreter, HappyRequest));
    }

    [Fact]
    public void PlaceOrder_PaymentFailed_Throws()
    {
        var interpreter = new TestInterpreterHKT(
            stockAvailable: true,
            price: 99.50m,
            chargeSucceeds: false);

        Assert.Throws<OrderFailedException>(
            () => OrderProgramsHKT.PlaceOrder(interpreter, HappyRequest));
    }

    // ── Brand safety: compile-time guarantees ───────────────────

    [Fact]
    public void IdKind_UnwrapsCorrectly()
    {
        IKind<IdBrand, int> kind = new IdKind<int>(42);
        Assert.Equal(42, kind.Unwrap());
    }

    [Fact]
    public async Task TaskKind_UnwrapsCorrectly()
    {
        IKind<TaskBrand, string> kind = new TaskKind<string>(Task.FromResult("hello"));
        Assert.Equal("hello", await kind.Unwrap());
    }
}
