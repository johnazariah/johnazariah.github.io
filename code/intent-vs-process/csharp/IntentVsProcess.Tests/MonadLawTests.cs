using IntentVsProcess.Domain;
using IntentVsProcess.Free;
using Xunit;

namespace IntentVsProcess.Tests;

// ─── Post 5: Monad law verification ──────────────────────────────────
// The monad laws aren't abstract niceties — they're correctness
// guarantees. These tests verify that OrderProgram satisfies them.
//
//   Left identity:   Done(a).SelectMany(f) == f(a)
//   Right identity:  m.SelectMany(Done)    == m
//   Associativity:   (m.SelectMany(f)).SelectMany(g) ==
//                     m.SelectMany(x => f(x).SelectMany(g))

public class MonadLawTests
{
    // Helper: run a program to get a result for comparison
    private static T Run<T>(OrderProgram<T> program) =>
        OrderInterpreter.RunSync(program, OrderInterpreter.DefaultTestExecutor);

    // ── Left Identity ───────────────────────────────────────────
    // Done(a).SelectMany(f) == f(a)

    [Fact]
    public void LeftIdentity_DoneSelectManyF_EqualsFA()
    {
        var a = 42;
        Func<int, OrderProgram<string>> f = x => new Done<string>($"value: {x}");

        var left = new Done<int>(a).SelectMany(f);
        var right = f(a);

        Assert.Equal(Run(left), Run(right));
    }

    // ── Right Identity ──────────────────────────────────────────
    // m.SelectMany(Done) == m

    [Fact]
    public void RightIdentity_MSelectManyDone_EqualsM()
    {
        var m = new Done<int>(42);

        var left = m.SelectMany(x => new Done<int>(x));
        var right = m;

        Assert.Equal(Run(left), Run(right));
    }

    [Fact]
    public void RightIdentity_FailedPropagates()
    {
        var m = new Failed<int>("something went wrong");

        var bound = m.SelectMany(x => new Done<int>(x));

        Assert.IsType<Failed<int>>(bound);
    }

    // ── Associativity ───────────────────────────────────────────
    // (m.SelectMany(f)).SelectMany(g) == m.SelectMany(x => f(x).SelectMany(g))

    [Fact]
    public void Associativity_GroupingDoesNotMatter()
    {
        var m = new Done<int>(10);
        Func<int, OrderProgram<int>> f = x => new Done<int>(x + 5);
        Func<int, OrderProgram<string>> g = x => new Done<string>($"result: {x}");

        var left = m.SelectMany(f).SelectMany(g);
        var right = m.SelectMany(x => f(x).SelectMany(g));

        Assert.Equal(Run(left), Run(right));
    }

    [Fact]
    public void Associativity_WithSteps()
    {
        // Same law but with actual OrderStep operations
        var m = OrderProgramExtensions.Lift(
            new CheckStock([new Item("SKU-001", "Widget", 1)]));

        Func<StockResult, OrderProgram<PriceResult>> f = stock =>
            OrderProgramExtensions.Lift(
                new CalculatePrice([new Item("SKU-001", "Widget", 1)], null));

        Func<PriceResult, OrderProgram<string>> g = price =>
            new Done<string>($"total: {price.Total}");

        var left = m.SelectMany(f).SelectMany(g);
        var right = m.SelectMany(x => f(x).SelectMany(g));

        Assert.Equal(Run(left), Run(right));
    }

    // ── Failed short-circuits correctly ─────────────────────────

    [Fact]
    public void Failed_ShortCircuits_SelectMany()
    {
        var failed = new Failed<int>("boom");
        var wasCalled = false;

        var result = failed.SelectMany<int, string>(x =>
        {
            wasCalled = true;
            return new Done<string>("should not reach here");
        });

        Assert.False(wasCalled);
        Assert.IsType<Failed<string>>(result);
    }

    [Fact]
    public void Failed_ShortCircuits_Select()
    {
        var failed = new Failed<int>("boom");

        var result = failed.Select(x => x * 2);

        Assert.IsType<Failed<int>>(result);
    }
}
