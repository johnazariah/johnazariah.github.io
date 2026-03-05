namespace IntentVsProcess.HKT;

// ─── The Brand Pattern: Simulating Higher-Kinded Types in C# ─────────
//
// C# can't express "a type constructor" as a type parameter.
// You can write Task<int>, but you can't write F<int> where F itself
// is a parameter (like Task<> or List<>).
//
// The brand pattern fixes this with two pieces:
//   1. A "brand" — a marker type that identifies which constructor we mean
//   2. IKind<TBrand, T> — a wrapper interface that carries the actual value
//
// This is the ONE PIECE OF MACHINERY you build once and reuse everywhere.
// It's the tax C# charges for not having HKTs.
// F# charges the same tax but with less syntax.
// Haskell and Scala don't charge it at all.

/// <summary>
/// A value of type IKind&lt;TBrand, T&gt; represents "TBrand applied to T."
/// For example, IKind&lt;TaskBrand, int&gt; represents Task&lt;int&gt;.
/// </summary>
public interface IKind<TBrand, out T> { }
