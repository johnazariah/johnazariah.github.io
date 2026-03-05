using IntentVsProcess.Domain;
using IntentVsProcess.Free;
using Xunit;

namespace IntentVsProcess.Tests;

// ─── Post 3: Saga / compensation tests ────────────────────────────────
// Verify that the saga interpreter rolls back compensatable steps
// when a later step fails.

public class SagaTests
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
    public void RunWithSagaSync_HappyPath_NoRollback()
    {
        var program = Free.OrderPrograms.PlaceOrderWithCompensation(HappyRequest);

        var rollbackLog = new List<string>();

        object executor(OrderStepBase step) => step switch
        {
            CheckStock => new StockResult(IsAvailable: true),
            CalculatePrice => new PriceResult(Total: 99.50m, Subtotal: 99.50m),
            ChargePayment => new ChargeResult(Succeeded: true, TransactionId: "txn-saga"),
            ReserveInventory => new ReservationResult(ReservationId: "res-saga"),
            SendConfirmation => Unit.Value,
            RefundPayment r => recordRollback("Refund:" + r.TransactionId),
            ReleaseInventory r => recordRollback("Release:" + r.ReservationId),
            _ => throw new InvalidOperationException($"Unexpected: {step.GetType().Name}")
        };

        object recordRollback(string entry)
        {
            rollbackLog.Add(entry);
            return Unit.Value;
        }

        var result = SagaInterpreter.RunWithSagaSync(program, executor);

        Assert.IsType<OrderResult.Success>(result);
        Assert.Empty(rollbackLog); // No failures → no rollbacks
    }

    [Fact]
    public void RunWithSagaSync_FailureAfterCharge_RollsBackCharge()
    {
        var program = Free.OrderPrograms.PlaceOrderWithCompensation(HappyRequest);

        var rollbackLog = new List<string>();

        object executor(OrderStepBase step) => step switch
        {
            CheckStock => new StockResult(IsAvailable: true),
            CalculatePrice => new PriceResult(Total: 99.50m, Subtotal: 99.50m),
            ChargePayment => new ChargeResult(Succeeded: true, TransactionId: "txn-saga"),
            ReserveInventory => throw new Exception("DB timeout!"),
            RefundPayment r => recordRollback("Refund:" + r.TransactionId),
            ReleaseInventory r => recordRollback("Release:" + r.ReservationId),
            _ => throw new InvalidOperationException($"Unexpected: {step.GetType().Name}")
        };

        object recordRollback(string entry)
        {
            rollbackLog.Add(entry);
            return Unit.Value;
        }

        Assert.Throws<Exception>(
            () => SagaInterpreter.RunWithSagaSync(program, executor));

        // Charge succeeded → should be rolled back (refunded)
        Assert.Contains("Refund:txn-saga", rollbackLog);
    }

    [Fact]
    public void PlaceOrderWithCompensation_HasCompensatableSteps()
    {
        var program = Free.OrderPrograms.PlaceOrderWithCompensation(HappyRequest);
        var steps = StructuralHelpers.Flatten(program);

        var compensatableCount = steps.Count(s => s is ICompensatable);
        Assert.True(compensatableCount >= 2,
            "PlaceOrderWithCompensation should have at least 2 compensatable steps (charge + reserve)");
    }

    [Fact]
    public void WithCompensation_PreservesForwardStep()
    {
        var forward = new ChargePayment(PaymentMethod.CreditCard, 50m);
        var comp = new WithCompensation<ChargeResult>(
            forward,
            result => new RefundPayment("txn-123"));

        Assert.Same(forward, comp.Forward);
        Assert.Equal(forward, ((ICompensatable)comp).ForwardStep);
    }
}
