using IntentVsProcess.Domain;

namespace IntentVsProcess.Free;

// ─── Parallel-aware async interpreter ─────────────────────────────────
//
// When the AST contains Both nodes (from the applicative Parallel combinator),
// this interpreter runs both branches concurrently via Task.WhenAll.
// Sequential Bind nodes are still executed one at a time.
//
// This is the payoff for the Tagless Final → Free Monad pipeline:
//   1. Author programs against the algebra (ergonomic, extensible)
//   2. One interpreter produces a Free Monad AST (inspectable)
//   3. This interpreter discovers Both nodes and runs them in parallel
//
// The business logic doesn't mention parallelism. The AST encodes
// data independence. The interpreter exploits it.

public static class ParallelInterpreter
{
    /// <summary>
    /// Run a program asynchronously, executing Both branches in parallel.
    /// The executor maps each step to its result asynchronously.
    /// </summary>
    public static async Task<T> RunAsync<T>(
        OrderProgram<T> program,
        Func<OrderStepBase, Task<object>> executor)
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
                    var result = await executor(bind.Step);
                    current = bind.Continue(result);
                    break;

                case Both<T> both:
                    // This is where parallelism happens:
                    // both branches are independent, so run them concurrently.
                    var leftTask = RunAsync(both.Left, executor);
                    var rightTask = RunAsync(both.Right, executor);
                    await Task.WhenAll(leftTask, rightTask);
                    current = both.Combine(leftTask.Result, rightTask.Result);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown OrderProgram type: {current.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Default async test executor — wraps the sync test executor in a Task.
    /// Adds a small delay to each step to make parallelism observable in timing tests.
    /// </summary>
    public static async Task<object> DefaultAsyncTestExecutor(OrderStepBase step)
    {
        // Simulate I/O latency so parallel vs sequential is measurable
        await Task.Delay(50);
        return OrderInterpreter.DefaultTestExecutor(step);
    }
}
