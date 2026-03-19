---
    layout: post
    title: "Intent vs Process - Part 5: Choosing Both"
    tags: [C#, software-architecture, free-monad, tagless-final, functional-programming, optimization]
    author: johnazariah
    summary: "Tagless Final is ergonomic. Free Monads are inspectable. What if you didn't have to choose? The applicative combinator unlocks parallelism — and the pipeline from algebra to AST to concurrent execution."
---

_This series is dedicated to [Christian Smith](https://www.linkedin.com/in/christian-smith-9562658/), with gratitude for all the insightful conversations that shaped the ideas in these posts._

> **Series: Your Clean Architecture Has a Dirty Secret**
>
> This is Part 5 of a 7-part series on separating intent from process in real-world C#.
>
> 1. [Your Clean Architecture Has a Dirty Secret](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html)
> 2. [The Algebra of Intent](/2026/03/05/02-the-algebra-of-intent.html)
> 3. [Intent You Can See (and Optimize)](/2026/03/05/03-intent-you-can-see-and-optimize.html)
> 4. [Two Sides of the Same Coin](/2026/03/05/04-two-sides-of-the-same-coin.html)
> 5. **Choosing Both** ← you are here
> 6. [Standing on the Shoulders of Giants](/2026/03/05/05-standing-on-the-shoulders-of-giants.html)
> 7. [The Strangler Fig](/2026/03/05/06-the-strangler-fig.html)

---

# Choosing Both

At the end of [Post 4](/2026/03/05/04-two-sides-of-the-same-coin.html), we said something tantalizing:

> The most powerful option: write your programs against an algebra (Tagless Final), but have one of your interpreters *produce a Free Monad AST* that you can then optimize and interpret in a second pass.

That was a promise. This post delivers it — by solving a concrete problem: **parallelism**.

- [The Problem: Monadic Bind is Sequential](#the-problem-monadic-bind-is-sequential)
- [The Solution: an Applicative Combinator](#the-solution-an-applicative-combinator)
- [Free Monad: Parallel in the AST](#free-monad-parallel-in-the-ast)
- [Tagless Final: Both in the Algebra](#tagless-final-both-in-the-algebra)
- [The Pipeline: Algebra → AST → Parallel Execution](#the-pipeline-algebra--ast--parallel-execution)
- [The Parallel Interpreter](#the-parallel-interpreter)
- [Testing All Three Paths](#testing-all-three-paths)
- [What This Proves](#what-this-proves)

---

## The Problem: Monadic Bind is Sequential

Look at our order flow from Post 3:

```csharp
from stock in Lift(new CheckStock(request.Items))
where stock.IsAvailable
from price in Lift(new CalculatePrice(request.Items, request.Coupon))
from charge in Lift(new ChargePayment(request.PaymentMethod, price.Total))
...
```

Read the data dependencies: `ChargePayment` needs `price.Total`, which comes from `CalculatePrice`. That's a genuine dependency — you can't charge without knowing the amount.

But `CheckStock` and `CalculatePrice`? Neither uses the other's result. They operate on the same input (`request.Items`) but produce independent outputs. In principle, they could run concurrently.

The problem is `from ... in ... from ... in ...` — LINQ's monadic bind — which is **inherently sequential**. Each `from` clause depends on all the previous clauses. In the AST, this becomes a chain of `Bind` nodes where each continuation is an opaque function. You can't look inside a `Func<StockResult, OrderProgram<T>>` to discover that it doesn't actually *use* the `StockResult` yet.

This is the fundamental tension we flagged in Post 3's "Honest Note on Analyzability":

> Monadic bind hides the next effect behind an opaque function.

If we want parallelism, we need a different combinator — one that *structurally* expresses independence.

---

## The Solution: an Applicative Combinator

The answer comes from a layer *below* monads in the abstraction hierarchy: **applicative functors**. Where monadic bind says "do A, then use A's result to decide what B is," an applicative combinator says "do A and B independently, then combine their results."

We add one new construct to the Free Monad AST:

```csharp
public record Both<T>(
    OrderProgram<object> Left,
    OrderProgram<object> Right,
    Func<object, object, OrderProgram<T>> Combine) : OrderProgram<T>;
```

Three fields:
- `Left` and `Right` — two sub-programs that don't depend on each other
- `Combine` — a function that takes both results and produces the rest of the program

This is the data-level equivalent of "these two things are independent." An interpreter can look at a `Both` node and *know* — structurally, without analyzing continuations — that the two branches can run concurrently.

Compare with `Bind`:

| | `Bind` (monadic) | `Both` (applicative) |
|---|---|---|
| Structure | Step + continuation | Left + Right + combiner |
| Dependency | Next depends on previous | Branches are independent |
| Parallelism | ❌ Sequential by construction | ✅ Parallel by construction |
| Expressiveness | Can express data-dependent flow | Cannot — branches can't see each other |

You don't *replace* `Bind` with `Both`. You use `Both` for the independent parts and `Bind` for the dependent parts. The program's structure encodes the data-flow graph.

---

## Free Monad: Parallel in the AST

With a `Parallel` helper that lifts two programs into a `Both` node:

```csharp
public static OrderProgram<(TA, TB)> Parallel<TA, TB>(
    OrderProgram<TA> left,
    OrderProgram<TB> right) =>
    new Both<(TA, TB)>(
        left.Select(a => (object)a!),
        right.Select(b => (object)b!),
        (a, b) => new Done<(TA, TB)>(((TA)a, (TB)b)));
```

The parallel order flow becomes:

```csharp
public static OrderProgram<OrderResult> PlaceOrderParallel(OrderRequest request) =>
    from stockAndPrice in Parallel(
        Lift(new CheckStock(request.Items)),
        Lift(new CalculatePrice(request.Items, request.Coupon)))
    let stock = stockAndPrice.Item1
    let price = stockAndPrice.Item2
    where stock.IsAvailable
    from charge in Lift(new ChargePayment(request.PaymentMethod, price.Total))
    where charge.Succeeded
    from _ in Lift(new ReserveInventory(request.Items))
    from __ in Lift(new SendConfirmation(request.Customer, price))
    select OrderResult.Ok(charge.TransactionId!);
```

Read it: "From the stock check *and* the price calculation (in parallel), where stock is available, from the charge..."

Same five operations. Same business logic. But the *structure* is different. The first node in the AST is now `Both` instead of `Bind` — and any interpreter can see that.

```
Before (sequential):
  Bind(CheckStock, stock =>
    Bind(CalculatePrice, price =>
      Bind(ChargePayment, ...)))

After (parallel where possible):
  Both(
    Bind(CheckStock, Done),          ← left branch
    Bind(CalculatePrice, Done),      ← right branch
    (stock, price) =>
      Bind(ChargePayment, ...))      ← sequential from here
```

The AST itself is the execution plan. No heuristics. No analysis of opaque functions. The programmer declared independence; the interpreter exploits it.

---

## Tagless Final: Both in the Algebra

The same idea, on the Tagless Final side. Add `Both` to the algebra interface:

```csharp
public interface IOrderAlgebra<TResult>
{
    // ... existing operations ...

    /// <summary>
    /// Run two independent computations and combine their results.
    /// Default: sequential (left then right). Override for parallelism.
    /// </summary>
    TResult Both<A, B>(
        TResult left, TResult right,
        Func<A, B, TResult> combine) =>
        Then<A>(left, a => Then<B>(right, b => combine(a, b)));
}
```

The default implementation is sequential — `Then left, Then right, combine`. This means **every existing interpreter works without modification**. The `TestInterpreter`, `NarrativeInterpreter`, `DryRunInterpreter` — they all inherit the default and behave exactly as before.

The parallel program against the algebra:

```csharp
public static TResult PlaceOrderParallel<TResult>(
    IOrderAlgebra<TResult> alg,
    OrderRequest request) =>
    alg.Both<StockResult, PriceResult>(
        alg.CheckStock(request.Items),
        alg.CalculatePrice(request.Items, request.Coupon),
        (stock, price) => alg.Guard(
            () => stock.IsAvailable,
            () => alg.Then<ChargeResult>(
                alg.ChargePayment(request.PaymentMethod, price.Total),
                charge => alg.Guard(
                    () => charge.Succeeded,
                    () => alg.Then<ReservationResult>(
                        alg.ReserveInventory(request.Items),
                        _ => alg.Then<Unit>(
                            alg.SendConfirmation(request.Customer, price),
                            __ => alg.Done(OrderResult.Ok(charge.TransactionId!))
                        )
                    ),
                    "Payment failed"
                )
            ),
            "Out of stock"
        )
    );
```

Same intent. Same five operations. The `Both` call says "these two are independent" — but the algebra is still opaque. You can't inspect it. You can't discover the `Both` by analyzing the program.

Unless you use the right interpreter.

---

## The Pipeline: Algebra → AST → Parallel Execution

This is where the two approaches meet.

The `ToFreeMonadInterpreter` translates every algebra call into an AST node. Including `Both`:

```csharp
public class ToFreeMonadInterpreter : IOrderAlgebra<OrderProgram<Eval>>
{
    // Each algebra operation becomes a Bind node with a Lift
    public OrderProgram<Eval> CheckStock(List<Item> items) =>
        OrderProgramExtensions.Lift(new CheckStock(items))
            .Select(r => Eval.Of(r));

    // ... other operations follow the same pattern ...

    // Both in the algebra becomes Both in the AST
    public OrderProgram<Eval> Both<A, B>(
        OrderProgram<Eval> left,
        OrderProgram<Eval> right,
        Func<A, B, OrderProgram<Eval>> combine) =>
        new Both<Eval>(
            left.Select(e => (object)e),
            right.Select(e => (object)e),
            (l, r) =>
            {
                var leftEval = (Eval)l;
                var rightEval = (Eval)r;
                if (leftEval.IsFailure) return new Done<Eval>(leftEval);
                if (rightEval.IsFailure) return new Done<Eval>(rightEval);
                return combine(leftEval.Unwrap<A>(), rightEval.Unwrap<B>());
            });
}
```

The full pipeline:

```csharp
// Step 1: Author against the algebra (ergonomic, extensible)
var toFree = new ToFreeMonadInterpreter();

// Step 2: Generate AST — Both in the algebra becomes Both in the AST
var ast = OrderPrograms.PlaceOrderParallel(toFree, request);

// Step 3: The AST now contains a Both node — structurally inspectable!
Assert.True(ContainsBothNode(ast)); // ← this test passes

// Step 4: Run with the parallel interpreter
var result = await ParallelInterpreter.RunAsync(ast, executor);
```

Three representations of the same thing:

| Step | Representation | Parallelism |
|---|---|---|
| Author | Tagless Final (`alg.Both(...)`) | Declared in the algebra |
| Generate | Free Monad AST (`Both<T>` node) | Visible in the data |
| Execute | `Task.WhenAll` | Realized at runtime |

The programmer declares intent. The AST captures it as data. The interpreter exploits it. No magic. No heuristics. Just structure flowing through the pipeline.

---

## The Parallel Interpreter

The parallel interpreter is a straightforward async fold over the AST. The only interesting case is `Both`:

```csharp
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
                // Both branches are independent → run concurrently
                var leftTask = RunAsync(both.Left, executor);
                var rightTask = RunAsync(both.Right, executor);
                await Task.WhenAll(leftTask, rightTask);
                current = both.Combine(leftTask.Result, rightTask.Result);
                break;
        }
    }
}
```

`Bind` → await, continue. `Both` → fan out, `Task.WhenAll`, combine. The sync interpreter (`RunSync`) handles `Both` by running left-then-right sequentially — same result, different scheduling.

---

## Testing All Three Paths

The companion code tests all three approaches to parallelism:

```csharp
// Path 1: Free Monad — PlaceOrderParallel uses Parallel() directly
[Fact]
public void FreeParallel_HappyPath_Succeeds()
{
    var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);
    var result = OrderInterpreter.RunSync(program, OrderInterpreter.DefaultTestExecutor);
    Assert.IsType<OrderResult.Success>(result);
}

// Path 2: Tagless Final — PlaceOrderParallel uses alg.Both
[Fact]
public void TFParallel_HappyPath_Succeeds()
{
    var interpreter = new TestInterpreter(stockAvailable: true, price: 99.50m,
        chargeSucceeds: true, transactionId: "txn-parallel");
    var result = OrderPrograms.PlaceOrderParallel(interpreter, HappyRequest);
    Assert.True(result.IsSuccess);
}

// Path 3: The pipeline — TF → Free → Parallel
[Fact]
public async Task Pipeline_TFParallelToFree_RunsInParallel()
{
    var toFree = new ToFreeMonadInterpreter();
    var ast = OrderPrograms.PlaceOrderParallel(toFree, HappyRequest);
    var result = await ParallelInterpreter.RunAsync(ast, evalExecutor);
    Assert.IsType<OrderResult.Success>(result);
}
```

And a structural test — the whole point of the Free Monad:

```csharp
[Fact]
public void FreeParallel_ContainsBothNode()
{
    var program = Free.OrderPrograms.PlaceOrderParallel(HappyRequest);
    Assert.IsType<Both<OrderResult>>(program);
    // The AST's root node IS a Both — parallelism is visible in the data!
}
```

All 12 parallel tests pass alongside the original 45 tests.

---

## What This Proves

This post answered three questions:

**Can you parallelize with a Free Monad?** Yes — add an applicative combinator (`Both`) to the AST. The programmer marks independent steps; the interpreter runs them concurrently. No heuristic analysis of opaque continuations. The structure *is* the execution plan.

**Can you parallelize with Tagless Final?** Yes — add `Both` to the algebra with a default sequential implementation. Existing interpreters work unchanged. A parallel-aware interpreter overrides it.

**Can you have both ergonomics and inspectability?** Yes — and this is the real payoff. Author programs against the algebra (extensible, familiar DI), then use the `ToFreeMonadInterpreter` to generate an AST (inspectable, optimizable). The `Both` in your algebra becomes a `Both` node in the AST. The parallel interpreter discovers it and runs it concurrently.

The post from [2023's "Honest Note on Analyzability"](/2026/03/05/03-intent-you-can-see-and-optimize.html#an-honest-note-on-analyzability) said:

> Monadic bind hides the next effect behind an opaque function.

That's still true. But the solution isn't to analyze what's hidden — it's to **not hide it in the first place**. Where you have independence, say so with `Both`. Where you have genuine data dependency, use `Bind`. The program's type structure carries the data-flow graph. The interpreter reads it.

This is the same insight that drives [Haxl](https://github.com/facebook/Haxl) (Facebook's Haskell data-fetching library) and [Fetch](https://47degrees.github.io/fetch/) (the Scala equivalent). Both use applicative combinators to batch and parallelize data fetches. What we've built here is a simplified version of the same idea, in C#, integrated with the Tagless Final / Free Monad duality from the rest of the series.

---

> **Companion code**: The parallel combinator, `ToFreeMonadInterpreter`, `ParallelInterpreter`, and all 12 parallel tests are in the [companion repository](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/). Available in [C#](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/csharp/) (57 tests), [F#](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/fsharp/) (27 tests), and [Haskell](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/haskell/) (29 tests).

---

> **Next**: [Standing on the Shoulders of Giants](/2026/03/05/05-standing-on-the-shoulders-of-giants.html) — the foundations: monads, free constructions, Yoneda, algebraic effects, and why a half-century of mathematics gives us confidence that our C# code is correct.
>
> *Not interested in category theory? Skip straight to [The Strangler Fig](/2026/03/05/06-the-strangler-fig.html) — the Monday morning migration plan for getting your legacy codebase from here to there, one service at a time.*

---

*This is Part 5 of the series **"Your Clean Architecture Has a Dirty Secret."** The full series:*

1. *[Your Clean Architecture Has a Dirty Secret](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html) — the diagnosis*
2. *[The Algebra of Intent](/2026/03/05/02-the-algebra-of-intent.html) — Tagless Final*
3. *[Intent You Can See (and Optimize)](/2026/03/05/03-intent-you-can-see-and-optimize.html) — Free Monads*
4. *[Two Sides of the Same Coin](/2026/03/05/04-two-sides-of-the-same-coin.html) — the duality*
5. *[Choosing Both](/2026/03/19/intent-vs-process-choosing-both.html) — parallelism*
6. *[Standing on the Shoulders of Giants](/2026/03/05/05-standing-on-the-shoulders-of-giants.html) — the foundations*
7. *[The Strangler Fig](/2026/03/05/06-the-strangler-fig.html) — the migration*

*For the F# perspective on Tagless Final, see the [six-part Tagless Final series](/2025/12/12/tagless-final-01-froggy-tree-house.html). For the Free Monad story that started it all, see [Bouncing around with Recursion](/2020/12/07/bouncing-around-with-recursion.html) and [The Trampoline is a Monad](/2026/03/04/the-trampoline-is-a-monad.html).*
