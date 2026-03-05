using IntentVsProcess.Domain;

namespace IntentVsProcess.TaglessFinal;

// ─── Post 2: The algebra of intent ────────────────────────────────────
//
// One interface that captures the ENTIRE vocabulary of the domain —
// including how operations chain together. The TResult type parameter
// means "I don't know what this algebra produces; that depends on the
// interpreter."
//
// Compare with Post 1's four separate interfaces (IInventoryService,
// IPricingService, IPaymentGateway, IEmailService) — those abstracted
// individual services but left the *composition* hardwired in the method.

/// <summary>
/// The order algebra — domain operations + composition.
/// <para>
/// TResult is the carrier type:
/// <list type="bullet">
///   <item>Eval for synchronous interpreters (test, in-memory)</item>
///   <item>Task&lt;Eval&gt; for async interpreters (production)</item>
///   <item>string for narrative interpreters</item>
///   <item>List&lt;AuditEntry&gt; for dry-run/audit interpreters</item>
/// </list>
/// </para>
/// </summary>
public interface IOrderAlgebra<TResult>
{
    // ── Domain operations: the vocabulary of intent ──

    TResult CheckStock(List<Item> items);
    TResult CalculatePrice(List<Item> items, Coupon? coupon);
    TResult ChargePayment(PaymentMethod method, decimal amount);
    TResult ReserveInventory(List<Item> items);
    TResult SendConfirmation(Customer customer, PriceResult price);

    // ── Composition: how operations chain together ──

    /// <summary>
    /// Sequence two operations: run <paramref name="first"/>, extract the
    /// intermediate result of type <typeparamref name="T"/>, and pass it
    /// to <paramref name="next"/>.
    /// </summary>
    TResult Then<T>(TResult first, Func<T, TResult> next);

    /// <summary>Wrap a final OrderResult.</summary>
    TResult Done(OrderResult result);

    /// <summary>
    /// Guard: if <paramref name="predicate"/> is false, short-circuit with
    /// <paramref name="failureReason"/>. Otherwise evaluate <paramref name="onSuccess"/>.
    /// The continuation is lazy to avoid evaluating downstream steps on failure.
    /// </summary>
    TResult Guard(Func<bool> predicate, Func<TResult> onSuccess, string failureReason);
}
