namespace IntentVsProcess.HKT;

// ─── Brands: one per type constructor ─────────────────────────────────
//
// Each brand + wrapper pair tells IKind what underlying type it represents.
// To add a new wrapper (e.g., for Result<T> or Either<L,R>),
// create a new brand struct and a new record implementing IKind.

// ── Task<T> ──

/// <summary>Brand identifying the Task&lt;&gt; type constructor.</summary>
public readonly struct TaskBrand;

/// <summary>Wraps Task&lt;T&gt; as IKind&lt;TaskBrand, T&gt;.</summary>
public record TaskKind<T>(Task<T> Value) : IKind<TaskBrand, T>;

// ── Identity (synchronous, no wrapping) ──

/// <summary>Brand identifying the identity functor (just T itself).</summary>
public readonly struct IdBrand;

/// <summary>Wraps a plain T as IKind&lt;IdBrand, T&gt;.</summary>
public record IdKind<T>(T Value) : IKind<IdBrand, T>;

// ── List<T> (for audit/dry-run) ──

/// <summary>Brand identifying the List&lt;&gt; type constructor.</summary>
public readonly struct ListBrand;

/// <summary>Wraps List&lt;T&gt; as IKind&lt;ListBrand, T&gt;.</summary>
public record ListKind<T>(List<T> Value) : IKind<ListBrand, T>;

// ── Extension methods for unwrapping ──

public static class KindExtensions
{
    public static Task<T> Unwrap<T>(this IKind<TaskBrand, T> kind) =>
        ((TaskKind<T>)kind).Value;

    public static T Unwrap<T>(this IKind<IdBrand, T> kind) =>
        ((IdKind<T>)kind).Value;

    public static List<T> Unwrap<T>(this IKind<ListBrand, T> kind) =>
        ((ListKind<T>)kind).Value;
}
