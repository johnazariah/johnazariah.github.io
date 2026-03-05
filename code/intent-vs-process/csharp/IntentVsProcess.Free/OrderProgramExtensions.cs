namespace IntentVsProcess.Free;

// ─── Post 3: LINQ extensions — the monadic plumbing ──────────────────
//
// These enable LINQ query syntax for OrderProgram<T>.
// Select  = Functor (map)
// SelectMany = Monad (bind / flatMap)
// Where = Guard (business rule)
//
// The monad laws (left identity, right identity, associativity) hold
// by construction — see Post 5 for why this matters.

public static class OrderProgramExtensions
{
    /// <summary>
    /// Lift a single step into a one-step program.
    /// The continuation casts the object result back to the step's result type.
    /// </summary>
    public static OrderProgram<T> Lift<T>(OrderStep<T> step) =>
        new Bind<T>(step, result => new Done<T>((T)result));

    /// <summary>
    /// Functor: map a function over the program's result.
    /// </summary>
    public static OrderProgram<TResult> Select<T, TResult>(
        this OrderProgram<T> source,
        Func<T, TResult> selector) =>
        source.SelectMany(value => new Done<TResult>(selector(value)));

    /// <summary>
    /// Monad bind: sequence this program with a function that produces the next program.
    /// This is the heart of the Free Monad — it threads the continuation through.
    /// </summary>
    public static OrderProgram<TResult> SelectMany<T, TResult>(
        this OrderProgram<T> source,
        Func<T, OrderProgram<TResult>> selector) =>
        source switch
        {
            Done<T> done => selector(done.Value),
            Failed<T> failed => new Failed<TResult>(failed.Reason),
            Bind<T> bind => new Bind<TResult>(
                bind.Step,
                x => bind.Continue(x).SelectMany(selector)),
            _ => throw new InvalidOperationException(
                $"Unknown OrderProgram type: {source.GetType()}")
        };

    /// <summary>
    /// LINQ query syntax support — the three-argument overload required by
    /// the compiler for 'from x in ... from y in ... select f(x, y)'.
    /// </summary>
    public static OrderProgram<TResult> SelectMany<T, TIntermediate, TResult>(
        this OrderProgram<T> source,
        Func<T, OrderProgram<TIntermediate>> collectionSelector,
        Func<T, TIntermediate, TResult> resultSelector) =>
        source.SelectMany(x =>
            collectionSelector(x).Select(y => resultSelector(x, y)));

    /// <summary>
    /// Guard: LINQ's 'where' clause becomes a business rule.
    /// If the predicate fails, the program short-circuits to Failed.
    /// 'where stock.IsAvailable' reads like a requirement, not control flow.
    /// </summary>
    public static OrderProgram<T> Where<T>(
        this OrderProgram<T> source,
        Func<T, bool> predicate) =>
        source.SelectMany<T, T>(value =>
            predicate(value)
                ? new Done<T>(value)
                : new Failed<T>("Guard condition failed"));
}
