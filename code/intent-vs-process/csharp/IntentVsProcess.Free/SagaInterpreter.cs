using IntentVsProcess.Domain;

namespace IntentVsProcess.Free;

// ─── Post 3: Saga interpreter — compensation that writes itself ───────
//
// The saga interpreter walks the AST, executes forward steps,
// accumulates a compensation stack, and on failure unwinds it.
//
// "This is the saga pattern without the saga framework — without
//  event buses, without state machines, without correlation IDs."

public static class SagaInterpreter
{
    /// <summary>
    /// Run a program with saga-style compensation.
    /// If any step throws, all previously accumulated compensations
    /// are executed in reverse order (stack unwinding).
    /// </summary>
    public static T RunWithSagaSync<T>(
        OrderProgram<T> program,
        Func<OrderStepBase, object> executor)
    {
        var compensations = new Stack<Action>();
        try
        {
            return RunForward(program, executor, compensations);
        }
        catch
        {
            // Unwind: execute accumulated compensations in reverse order
            foreach (var compensation in compensations)
            {
                compensation();
            }
            throw;
        }
    }

    private static T RunForward<T>(
        OrderProgram<T> program,
        Func<OrderStepBase, object> executor,
        Stack<Action> compensations)
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

                case Bind<T> bind when bind.Step is ICompensatable comp:
                    {
                        // Execute the forward step
                        var result = executor(comp.ForwardStep);
                        // Build the rollback step and push it onto the compensation stack
                        var rollbackStep = comp.CreateRollbackStep(result);
                        compensations.Push(() => executor(rollbackStep));
                        // Continue with the result
                        current = bind.Continue(result);
                        break;
                    }

                case Bind<T> bind:
                    {
                        var result = executor(bind.Step);
                        current = bind.Continue(result);
                        break;
                    }

                default:
                    throw new InvalidOperationException(
                        $"Unknown OrderProgram type: {current.GetType().Name}");
            }
        }
    }
}
