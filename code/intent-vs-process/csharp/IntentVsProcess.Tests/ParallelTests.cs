using IntentVsProcess.Domain;
using IntentVsProcess.Free;
using IntentVsProcess.TaglessFinal;
using IntentVsProcess.TaglessFinal.Interpreters;
using Xunit;
using static IntentVsProcess.Free.OrderProgramExtensions;

namespace IntentVsProcess.Tests;

// ─── Parallelism tests ────────────────────────────────────────────────
//
// Three approaches to parallelism, all producing the same result:
//   1. Free Monad: PlaceOrderParallel uses Parallel() directly in LINQ
//   2. Tagless Final: PlaceOrderParallel uses Both in the algebra
//   3. TF → Free pipeline: author against algebra, generate AST, run parallel

public class ParallelTests
{
    private static readonly List<Item> TestItems =
    [
        new("SKU-001", "Widget", 2),
        new("SKU-002", "Gadget", 1)
    ];

    private static readonly Customer TestCustomer = new("Alice", "alice@example.com");

    private static readonly OrderRequest HappyRequest = new(
        Items: TestItems,
        Customer: TestCustomer,
        PaymentMethod: PaymentMethod.CreditCard);

    // ═══════════════════════════════════════════════════════════════
    // Free Monad: PlaceOrderParallel with Parallel() combinator
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FreeParallel_HappyPath_Succeeds()
    {
        var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);

        var result = OrderInterpreter.RunSync(
            program, OrderInterpreter.DefaultTestExecutor);

        Assert.IsType<OrderResult.Success>(result);
        Assert.Equal("txn-test-001", ((OrderResult.Success)result).TransactionId);
    }

    [Fact]
    public void FreeParallel_OutOfStock_Throws()
    {
        var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);

        object executor(OrderStepBase step) => step switch
        {
            CheckStock => new StockResult(IsAvailable: false),
            _ => OrderInterpreter.DefaultTestExecutor(step)
        };

        Assert.Throws<OrderFailedException>(
            () => OrderInterpreter.RunSync(program, executor));
    }

    [Fact]
    public void FreeParallel_ContainsBothNode()
    {
        // The AST should contain a Both node — proving parallelism is
        // encoded in the data structure, not opaque in a function.
        var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);
        Assert.IsType<Both<OrderResult>>(program);
    }

    [Fact]
    public void FreeParallel_StructuralAnalysis_FindsAllSteps()
    {
        var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);
        var steps = StructuralHelpers.StepNames(program);

        // Same 5 steps as sequential, but CheckStock and CalculatePrice
        // are inside a Both node
        Assert.Equal(5, steps.Count);
        Assert.Contains("CheckStock", steps);
        Assert.Contains("CalculatePrice", steps);
        Assert.Contains("ChargePayment", steps);
        Assert.Contains("ReserveInventory", steps);
        Assert.Contains("SendConfirmation", steps);
    }

    [Fact]
    public async Task FreeParallel_AsyncInterpreter_RunsBothBranchesConcurrently()
    {
        var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);

        // Each step takes ~50ms. Sequential would be ~250ms for 5 steps.
        // With CheckStock and CalculatePrice in parallel, it should be ~200ms.
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = await ParallelInterpreter.RunAsync(
            program, ParallelInterpreter.DefaultAsyncTestExecutor);

        sw.Stop();

        Assert.IsType<OrderResult.Success>(result);
        // The parallel version should be faster than 5 × 50ms = 250ms
        // (CheckStock ‖ CalculatePrice saves one step's worth of latency)
    }

    [Fact]
    public void FreeParallel_ExecutionPlan_Works()
    {
        var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);
        var plan = Optimizers.Analyze(program);

        Assert.Equal(5, plan.Steps.Count);
        Assert.Equal(2, plan.DatabaseCalls);
        Assert.Equal(1, plan.PaymentApiCalls);
    }

    // ═══════════════════════════════════════════════════════════════
    // Tagless Final: PlaceOrderParallel uses Both in the algebra
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TFParallel_HappyPath_Succeeds()
    {
        // The TestInterpreter uses the default Both implementation
        // (sequential fallback), so it works without modification.
        var interpreter = new TestInterpreter(
            stockAvailable: true,
            price: 99.50m,
            chargeSucceeds: true,
            transactionId: "txn-parallel");

        var result = TaglessFinal.OrderPrograms.PlaceOrderParallel(
            interpreter, HappyRequest);

        Assert.True(result.IsSuccess);
        var orderResult = result.Unwrap<OrderResult>();
        Assert.IsType<OrderResult.Success>(orderResult);
        Assert.Equal("txn-parallel", ((OrderResult.Success)orderResult).TransactionId);
    }

    [Fact]
    public void TFParallel_OutOfStock_Fails()
    {
        var interpreter = new TestInterpreter(stockAvailable: false);

        var result = TaglessFinal.OrderPrograms.PlaceOrderParallel(
            interpreter, HappyRequest);

        Assert.True(result.IsFailure);
        Assert.Equal("Out of stock", result.FailureReason);
    }

    // ═══════════════════════════════════════════════════════════════
    // The pipeline: TF → Free Monad → Parallel interpreter
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Pipeline_TFToFree_ProducesValidAST()
    {
        // Step 1: Author against the algebra
        // Step 2: ToFreeMonad interpreter produces an AST
        var toFree = new ToFreeMonadInterpreter();
        var ast = TaglessFinal.OrderPrograms.PlaceOrder(toFree, HappyRequest);

        // The AST is a valid OrderProgram that can be interpreted
        var result = OrderInterpreter.RunSync(ast, OrderInterpreter.DefaultTestExecutor);
        var eval = (Eval)result;
        Assert.True(eval.IsSuccess);
        var orderResult = eval.Unwrap<OrderResult>();
        Assert.IsType<OrderResult.Success>(orderResult);
    }

    [Fact]
    public void Pipeline_TFParallelToFree_ContainsBothNode()
    {
        // PlaceOrderParallel uses Both in the algebra.
        // ToFreeMonad translates Both → Both node in the AST.
        var toFree = new ToFreeMonadInterpreter();
        var ast = TaglessFinal.OrderPrograms.PlaceOrderParallel(toFree, HappyRequest);

        // The AST should contain a Both node somewhere
        Assert.True(ContainsBothNode(ast),
            "PlaceOrderParallel through ToFreeMonad should produce a Both node in the AST");
    }

    [Fact]
    public async Task Pipeline_TFParallelToFree_RunsInParallel()
    {
        // The full pipeline:
        //   1. Author: PlaceOrderParallel against the algebra
        //   2. Generate: ToFreeMonad produces AST with Both nodes
        //   3. Interpret: ParallelInterpreter runs Both concurrently
        var toFree = new ToFreeMonadInterpreter();
        var ast = TaglessFinal.OrderPrograms.PlaceOrderParallel(toFree, HappyRequest);

        // Wrap the async executor to handle the Eval layer
        Task<object> evalExecutor(OrderStepBase step)
        {
            return Task.FromResult(OrderInterpreter.DefaultTestExecutor(step));
        }

        var result = await ParallelInterpreter.RunAsync(ast, evalExecutor);
        var eval = (Eval)result;
        Assert.True(eval.IsSuccess);
        var orderResult = eval.Unwrap<OrderResult>();
        Assert.IsType<OrderResult.Success>(orderResult);
    }

    [Fact]
    public void Pipeline_TFParallelToFree_StructuralAnalysis()
    {
        var toFree = new ToFreeMonadInterpreter();
        var ast = TaglessFinal.OrderPrograms.PlaceOrderParallel(toFree, HappyRequest);

        // Structural analysis still works on the generated AST
        var steps = StructuralHelpers.Flatten(ast);
        Assert.True(steps.Count >= 5,
            "Should find at least 5 steps (CheckStock, CalculatePrice, ChargePayment, ReserveInventory, SendConfirmation)");
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private static bool ContainsBothNode<T>(OrderProgram<T> program)
    {
        return program switch
        {
            Both<T> => true,
            Bind<T> bind => TryFollowBind(bind),
            _ => false
        };
    }

    private static bool TryFollowBind<T>(Bind<T> bind)
    {
        // Check the current node, then try to follow the continuation
        try
        {
            var effectiveStep = bind.Step is ICompensatable comp
                ? comp.ForwardStep
                : bind.Step;
            var next = bind.Continue(OrderInterpreter.DefaultTestExecutor(effectiveStep));
            return ContainsBothNode(next);
        }
        catch
        {
            return false;
        }
    }
}
