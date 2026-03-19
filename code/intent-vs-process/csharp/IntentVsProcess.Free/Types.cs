using IntentVsProcess.Domain;

namespace IntentVsProcess.Free;

// ─── Post 3: The instruction set — operations as data ─────────────────
//
// These are VALUES, not actions. ChargePayment(Visa, 99.50m) doesn't
// charge anyone — it's a note that says "a charge should happen."

// ── Step hierarchy ──────────────────────────────────────────────────

/// <summary>Non-generic base for all order steps (needed for pattern matching).</summary>
public abstract record OrderStepBase;

/// <summary>A step that produces a result of type T when interpreted.</summary>
public abstract record OrderStep<T> : OrderStepBase;

// ── Concrete steps — the vocabulary of the order domain ─────────────

public record CheckStock(List<Item> Items) : OrderStep<StockResult>;
public record CalculatePrice(List<Item> Items, Coupon? Coupon) : OrderStep<PriceResult>;
public record ChargePayment(PaymentMethod Method, decimal Amount) : OrderStep<ChargeResult>;
public record ReserveInventory(List<Item> Items) : OrderStep<ReservationResult>;
public record SendConfirmation(Customer Customer, PriceResult Price) : OrderStep<Unit>;

// ── Compensation steps ──────────────────────────────────────────────

public record RefundPayment(string TransactionId) : OrderStep<Unit>;
public record ReleaseInventory(string ReservationId) : OrderStep<Unit>;

// ── Compensation interface ──────────────────────────────────────────

/// <summary>
/// Non-generic interface for compensatable steps.
/// The saga interpreter uses this to extract the forward step and
/// build a rollback action without knowing the generic type parameter.
/// </summary>
public interface ICompensatable
{
    OrderStepBase ForwardStep { get; }
    OrderStepBase CreateRollbackStep(object result);
}

/// <summary>
/// Wraps a step with its compensation (rollback) action.
/// The saga interpreter accumulates these on a stack and unwinds on failure.
/// </summary>
public record WithCompensation<T>(
    OrderStep<T> Forward,
    Func<T, OrderStepBase> Rollback) : OrderStep<T>, ICompensatable
{
    OrderStepBase ICompensatable.ForwardStep => Forward;
    OrderStepBase ICompensatable.CreateRollbackStep(object result) => Rollback((T)result);
}

// ─── Program AST — the Free Monad ─────────────────────────────────────
//
// Done  = Return / Pure  — inject a value into the program
// Failed = short-circuit — the program has failed
// Bind  = Then / FlatMap — do a step, then feed the result to a continuation
//
// This is Free<OrderStep, T>. The "Free" means: adds NO interpretation
// of its own — pure syntax, pure intent, waiting for an interpreter.

public abstract record OrderProgram<T>;

/// <summary>The program has finished successfully with this value.</summary>
public record Done<T>(T Value) : OrderProgram<T>;

/// <summary>The program has failed (guard failed, business rule violated).</summary>
public record Failed<T>(string Reason) : OrderProgram<T>;

/// <summary>
/// Do this step, then feed the result to Continue to get the rest of the program.
/// Step is OrderStepBase (non-generic) so we can store any step type.
/// Continue takes object (the boxed result) and returns the next program.
/// </summary>
public record Bind<T>(OrderStepBase Step, Func<object, OrderProgram<T>> Continue) : OrderProgram<T>;

/// <summary>
/// Run two independent sub-programs in parallel, then combine their results.
/// This is the APPLICATIVE combinator — it marks computations that don't
/// depend on each other and can be executed concurrently.
/// Monadic bind (Bind/Then) is inherently sequential: the next step depends
/// on the previous result. Both is the escape hatch for independence.
/// </summary>
public record Both<T>(
    OrderProgram<object> Left,
    OrderProgram<object> Right,
    Func<object, object, OrderProgram<T>> Combine) : OrderProgram<T>;
