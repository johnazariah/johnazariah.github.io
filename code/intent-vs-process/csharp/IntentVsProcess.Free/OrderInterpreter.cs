using IntentVsProcess.Domain;

namespace IntentVsProcess.Free;

// ─── Post 3: Interpreters — giving meaning to the AST ─────────────────
//
// The interpreter is a fold over the program tree.
// Same concept as the Trampoline's `execute` from the 2020 post.

public static class OrderInterpreter
{
    /// <summary>
    /// Run a program synchronously using the provided executor for each step.
    /// The executor maps each OrderStepBase to its result (boxed as object).
    /// </summary>
    public static T RunSync<T>(OrderProgram<T> program, Func<OrderStepBase, object> executor)
    {
        var current = program;
        while (true)
        {
            switch (current)
            {
                case Done<T> done:
                    return done.Value;
                case Failed<T> failed:
                    throw new OrderFailedException(failed.Reason);
                case Bind<T> bind:
                    var result = executor(bind.Step);
                    current = bind.Continue(result);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown OrderProgram type: {current.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Default test executor — produces deterministic results for every step type.
    /// Used by structural helpers, tests, and monad law verification.
    /// </summary>
    public static object DefaultTestExecutor(OrderStepBase step) => step switch
    {
        ICompensatable comp => DefaultTestExecutor(comp.ForwardStep),
        CheckStock => new StockResult(IsAvailable: true),
        CalculatePrice => new PriceResult(Total: 99.50m, Subtotal: 99.50m),
        ChargePayment => new ChargeResult(Succeeded: true, TransactionId: "txn-test-001"),
        ReserveInventory => new ReservationResult(ReservationId: "res-test-001"),
        SendConfirmation => Unit.Value,
        RefundPayment => Unit.Value,
        ReleaseInventory => Unit.Value,
        _ => throw new InvalidOperationException($"Unknown step: {step.GetType().Name}")
    };
}
