using IntentVsProcess.Domain;
using IntentVsProcess.Free;

namespace IntentVsProcess.TaglessFinal.Interpreters;

// ─── ToFreeMonad interpreter: Tagless Final → Free Monad AST ──────────
//
// This is the "Choose Both" approach from Post 4.
// Author programs against the algebra (ergonomic, extensible),
// then use this interpreter to produce a Free Monad AST (inspectable).
//
// The key insight: Both in the algebra becomes Both in the AST.
// A parallel-aware interpreter can then discover and exploit it.
//
// This interpreter is itself a natural transformation — it maps
// algebra methods to AST constructors. The math guarantees that the
// resulting AST represents the SAME program.

/// <summary>
/// Interpreter that translates Tagless Final programs into Free Monad ASTs.
/// The resulting OrderProgram can be inspected, optimized, and interpreted
/// with any Free Monad interpreter — including the parallel one.
/// </summary>
public class ToFreeMonadInterpreter : IOrderAlgebra<OrderProgram<Eval>>
{
    public OrderProgram<Eval> CheckStock(List<Item> items) =>
        OrderProgramExtensions.Lift(new CheckStock(items))
            .Select(r => Eval.Of(r));

    public OrderProgram<Eval> CalculatePrice(List<Item> items, Coupon? coupon) =>
        OrderProgramExtensions.Lift(new CalculatePrice(items, coupon))
            .Select(r => Eval.Of(r));

    public OrderProgram<Eval> ChargePayment(PaymentMethod method, decimal amount) =>
        OrderProgramExtensions.Lift(new ChargePayment(method, amount))
            .Select(r => Eval.Of(r));

    public OrderProgram<Eval> ReserveInventory(List<Item> items) =>
        OrderProgramExtensions.Lift(new ReserveInventory(items))
            .Select(r => Eval.Of(r));

    public OrderProgram<Eval> SendConfirmation(Customer customer, PriceResult price) =>
        OrderProgramExtensions.Lift(new SendConfirmation(customer, price))
            .Select(r => Eval.Of(r));

    public OrderProgram<Eval> Then<T>(
        OrderProgram<Eval> first,
        Func<T, OrderProgram<Eval>> next) =>
        first.SelectMany(eval =>
            eval.IsFailure
                ? new Done<Eval>(eval)
                : next(eval.Unwrap<T>()));

    public OrderProgram<Eval> Done(OrderResult result) =>
        new Done<Eval>(Eval.Of(result));

    public OrderProgram<Eval> Guard(
        Func<bool> predicate,
        Func<OrderProgram<Eval>> onSuccess,
        string failureReason) =>
        predicate() ? onSuccess() : new Done<Eval>(Eval.Failed(failureReason));

    /// <summary>
    /// The crucial method: Both in the algebra becomes Both in the AST.
    /// This preserves the independence information so a parallel interpreter
    /// can discover it later.
    /// </summary>
    public OrderProgram<Eval> Both<A, B>(
        OrderProgram<Eval> left,
        OrderProgram<Eval> right,
        Func<A, B, OrderProgram<Eval>> combine) =>
        new Both<Eval>(
            left.Select(e => (object)e),
            right.Select(e => (object)e),
            (l, r) =>
            {
                var leftEval = (Eval)l;
                var rightEval = (Eval)r;
                if (leftEval.IsFailure) return new Done<Eval>(leftEval);
                if (rightEval.IsFailure) return new Done<Eval>(rightEval);
                return combine(leftEval.Unwrap<A>(), rightEval.Unwrap<B>());
            });
}
