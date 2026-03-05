namespace IntentVsProcess.TaglessFinal;

// ─── Eval: the carrier type for Tagless Final interpreters ────────────
//
// C# doesn't have higher-kinded types, so IOrderAlgebra<TResult> uses a
// single TResult type for all operations. For interpreters that need to
// carry intermediate typed values (like the test interpreter), we use Eval
// — a tagged union that boxes an object and unwraps it with a cast.
//
// This is the "C# tax" for not having HKTs. See IntentVsProcess.HKT for
// the fully type-safe alternative using the brand pattern.

/// <summary>
/// Carrier type for Tagless Final evaluation.
/// Wraps an intermediate result with success/failure semantics.
/// </summary>
public abstract record Eval
{
    /// <summary>Wraps a successfully produced value.</summary>
    public sealed record Success(object? Value) : Eval;

    /// <summary>Represents a failed computation.</summary>
    public sealed record Fail(string Reason) : Eval;

    /// <summary>Wrap a typed value into Eval.</summary>
    public static Eval Of<T>(T value) => new Success(value);

    /// <summary>Create a failure.</summary>
    public static Eval Failed(string reason) => new Fail(reason);

    /// <summary>
    /// Extract the typed value. Throws if this is a Fail.
    /// The cast is the price we pay for not having HKTs.
    /// </summary>
    public T Unwrap<T>() => this is Success s
        ? (T)s.Value!
        : throw new InvalidOperationException(
            $"Cannot unwrap {(this as Fail)?.Reason ?? "unknown failure"}");

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Fail;
    public string? FailureReason => (this as Fail)?.Reason;
}
