namespace IntentVsProcess.Domain;

// ─── Result types for each domain operation ───────────────────────────
// Using readonly record structs so that default(T) produces a valid, non-null value.
// This matters for structural analysis of Free Monad programs (see StructuralHelpers).

/// <summary>Result of checking inventory availability.</summary>
public readonly record struct StockResult(bool IsAvailable);

/// <summary>Result of price calculation.</summary>
public readonly record struct PriceResult(
    decimal Total,
    decimal Subtotal = 0m,
    decimal Discount = 0m);

/// <summary>Result of a payment charge attempt.</summary>
public readonly record struct ChargeResult(
    bool Succeeded,
    string? TransactionId = null);

/// <summary>Result of reserving inventory.</summary>
public readonly record struct ReservationResult(string? ReservationId = null);

// ─── Final order result ───────────────────────────────────────────────

/// <summary>
/// The final outcome of placing an order.
/// This is what callers receive — either success with a transaction ID, or failure with a reason.
/// </summary>
public abstract record OrderResult
{
    public sealed record Success(string TransactionId) : OrderResult;
    public sealed record Failure(string Reason) : OrderResult;

    public bool Succeeded => this is Success;
    public string? Error => (this as Failure)?.Reason;

    public static OrderResult Ok(string transactionId) => new Success(transactionId);
    public static OrderResult Failed(string reason) => new Failure(reason);
}
