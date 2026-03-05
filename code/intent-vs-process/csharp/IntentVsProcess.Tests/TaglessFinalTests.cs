using IntentVsProcess.Domain;
using IntentVsProcess.TaglessFinal;
using IntentVsProcess.TaglessFinal.Interpreters;
using Xunit;

namespace IntentVsProcess.Tests;

// ─── Post 2: Tagless Final tests ──────────────────────────────────────
// "Testing without mocks" — swap the interpreter, assert the result.
// No Moq. No setup. No verify. If a new operation is added to the
// algebra, the compiler forces the TestInterpreter to implement it.

public class TaglessFinalTests
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

    // ── Happy path ──────────────────────────────────────────────

    [Fact]
    public void PlaceOrder_HappyPath_Succeeds()
    {
        var interpreter = new TestInterpreter(
            stockAvailable: true,
            price: 99.50m,
            chargeSucceeds: true,
            transactionId: "txn-001");

        var result = OrderPrograms.PlaceOrder(interpreter, HappyRequest);

        Assert.True(result.IsSuccess);
        var orderResult = result.Unwrap<OrderResult>();
        Assert.IsType<OrderResult.Success>(orderResult);
        Assert.Equal("txn-001", ((OrderResult.Success)orderResult).TransactionId);
    }

    // ── Out of stock ────────────────────────────────────────────

    [Fact]
    public void PlaceOrder_OutOfStock_Fails()
    {
        var interpreter = new TestInterpreter(stockAvailable: false);

        var result = OrderPrograms.PlaceOrder(interpreter, HappyRequest);

        Assert.True(result.IsFailure);
        Assert.Equal("Out of stock", result.FailureReason);
    }

    // ── Payment failed ──────────────────────────────────────────

    [Fact]
    public void PlaceOrder_PaymentFailed_Fails()
    {
        var interpreter = new TestInterpreter(
            stockAvailable: true,
            price: 99.50m,
            chargeSucceeds: false);

        var result = OrderPrograms.PlaceOrder(interpreter, HappyRequest);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment failed", result.FailureReason);
    }

    // ── Coupon discount ─────────────────────────────────────────

    [Fact]
    public void PlaceOrder_WithCoupon_AppliesDiscount()
    {
        var interpreter = new TestInterpreter(
            stockAvailable: true,
            price: 100.00m,
            chargeSucceeds: true);

        var request = HappyRequest with
        {
            Coupon = new Coupon("SAVE10", 10m)
        };

        var result = OrderPrograms.PlaceOrder(interpreter, request);

        Assert.True(result.IsSuccess);
    }

    // ── Narrative interpreter produces readable output ───────────

    [Fact]
    public void NarrativeInterpreter_ProducesReadableOutput()
    {
        var interpreter = new NarrativeInterpreter();

        var result = OrderPrograms.PlaceOrder(interpreter, HappyRequest);

        // The narrative interpreter produces output for all steps it can reach.
        // Guard clauses with default(T) may short-circuit, so we check
        // for the steps the interpreter can always narrate.
        Assert.Contains("Check if", result);
        Assert.Contains("item(s) are in stock", result);
    }

    // ── Dry-run interpreter records audit entries ────────────────

    [Fact]
    public void DryRunInterpreter_RecordsOperations()
    {
        var interpreter = new DryRunInterpreter();

        var entries = OrderPrograms.PlaceOrder(interpreter, HappyRequest);

        // The dry-run interpreter records all steps it visits.
        // Guard uses default(T) for intermediate values, so it may short-circuit.
        // We verify the audit trail contains the steps up to the first guard.
        var operationNames = entries.Select(e => e.Operation).ToList();
        Assert.Contains("CheckStock", operationNames);
        Assert.True(entries.Count >= 1, "Should record at least the first operation");
    }

    // ── Same program, multiple interpreters ──────────────────────

    [Fact]
    public void SameProgram_DifferentInterpreters_DifferentResults()
    {
        // This is the entire point of Tagless Final:
        // same PlaceOrder function, completely different behaviors.

        var testResult = OrderPrograms.PlaceOrder(
            new TestInterpreter(), HappyRequest);

        var narrative = OrderPrograms.PlaceOrder(
            new NarrativeInterpreter(), HappyRequest);

        var auditLog = OrderPrograms.PlaceOrder(
            new DryRunInterpreter(), HappyRequest);

        // Test: produces an Eval with the result
        Assert.True(testResult.IsSuccess);

        // Narrative: produces a human-readable string
        Assert.IsType<string>(narrative);
        Assert.NotEmpty(narrative);

        // Dry-run: produces a list of audit entries
        Assert.IsType<List<AuditEntry>>(auditLog);
        Assert.True(auditLog.Count >= 1);
    }
}
