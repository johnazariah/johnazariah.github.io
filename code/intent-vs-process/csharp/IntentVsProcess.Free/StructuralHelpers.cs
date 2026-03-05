namespace IntentVsProcess.Free;

// ─── Post 3: Structural analysis — test the SHAPE without running ─────
//
// "This is the testing equivalent of SQL EXPLAIN — verify the plan
//  without executing it."

public static class StructuralHelpers
{
    /// <summary>
    /// Walk the program AST and collect all steps as a flat list.
    /// Uses the default test executor to produce dummy results so
    /// continuations can be followed without real I/O.
    /// </summary>
    public static List<OrderStepBase> Flatten<T>(OrderProgram<T> program)
    {
        var steps = new List<OrderStepBase>();
        var current = program;

        while (current is Bind<T> bind)
        {
            steps.Add(bind.Step);
            try
            {
                // Use the forward step for compensatable steps so the default
                // executor can produce the right result type.
                var effectiveStep = bind.Step is ICompensatable comp
                    ? comp.ForwardStep
                    : bind.Step;
                current = bind.Continue(OrderInterpreter.DefaultTestExecutor(effectiveStep));
            }
            catch
            {
                break;
            }
        }

        return steps;
    }

    /// <summary>Count the number of steps in the program.</summary>
    public static int CountSteps<T>(OrderProgram<T> program) =>
        Flatten(program).Count;

    /// <summary>
    /// Get the type names of each step in order.
    /// For compensatable steps, returns the forward step's type name.
    /// </summary>
    public static List<string> StepNames<T>(OrderProgram<T> program) =>
        Flatten(program)
            .Select(s => GetEffectiveStep(s).GetType().Name)
            .ToList();

    /// <summary>
    /// Assert that a step of type TFirst appears before a step of type TSecond.
    /// </summary>
    public static bool AppearsBefore<TFirst, TSecond, T>(OrderProgram<T> program)
        where TFirst : OrderStepBase
        where TSecond : OrderStepBase
    {
        var steps = Flatten(program);
        var firstIdx = steps.FindIndex(s => GetEffectiveStep(s) is TFirst);
        var secondIdx = steps.FindIndex(s => GetEffectiveStep(s) is TSecond);
        return firstIdx >= 0 && secondIdx >= 0 && firstIdx < secondIdx;
    }

    /// <summary>
    /// Assert that TTarget never appears without TPrereq preceding it.
    /// </summary>
    public static bool NeverWithoutPreceding<TTarget, TPrereq, T>(OrderProgram<T> program)
        where TTarget : OrderStepBase
        where TPrereq : OrderStepBase
    {
        var steps = Flatten(program);
        var targetIdx = steps.FindIndex(s => GetEffectiveStep(s) is TTarget);
        if (targetIdx < 0) return true; // target not present — vacuously true
        var prereqIdx = steps.FindIndex(s => GetEffectiveStep(s) is TPrereq);
        return prereqIdx >= 0 && prereqIdx < targetIdx;
    }

    /// <summary>
    /// Assert that the (happy-path) flattened program contains a step of type TStep.
    /// </summary>
    public static bool AllPathsContain<TStep, T>(OrderProgram<T> program)
        where TStep : OrderStepBase
    {
        var steps = Flatten(program);
        return steps.Any(s => GetEffectiveStep(s) is TStep);
    }

    /// <summary>
    /// For compensatable steps, return the forward step; otherwise, the step itself.
    /// </summary>
    private static OrderStepBase GetEffectiveStep(OrderStepBase step) =>
        step is ICompensatable comp ? comp.ForwardStep : step;
}
