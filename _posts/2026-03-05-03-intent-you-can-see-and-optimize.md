---
    layout: post
    title: "Intent vs Process - Part 3: Intent You Can See (and Optimize)"
    tags: [C#, software-architecture, free-monad, LINQ, functional-programming, optimization]
    author: johnazariah
    summary: "What if your program was data you could walk, analyze, and transform — like SQL EXPLAIN for your business logic? Enter the Free Monad: programs as inspectable, optimizable values."
---

_This series is dedicated to [Christian Smith](https://www.linkedin.com/in/christian-smith-9562658/), with gratitude for all the insightful conversations that shaped the ideas in these posts._

> **Series: Your Clean Architecture Has a Dirty Secret**
>
> This is Part 3 of a 7-part series on separating intent from process in real-world C#.
>
> 1. [Your Clean Architecture Has a Dirty Secret](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html)
> 2. [The Algebra of Intent](/2026/03/05/02-the-algebra-of-intent.html)
> 3. **Intent You Can See (and Optimize)** ← you are here
> 4. [Two Sides of the Same Coin](/2026/03/05/04-two-sides-of-the-same-coin.html)
> 5. [Choosing Both](/2026/03/19/intent-vs-process-choosing-both.html)
> 6. [Standing on the Shoulders of Giants](/2026/03/05/05-standing-on-the-shoulders-of-giants.html)
> 7. [The Strangler Fig](/2026/03/05/06-the-strangler-fig.html)

---

# Intent You Can See (and Optimize)

In [Post 2](/2026/03/05/02-the-algebra-of-intent.html), we built a clean separation of intent from process using an algebra and interpreters. The business logic became a generic function that describes *what* to do, and interpreters decide *how* to do it.

It was a big upgrade. But there's something we can't do with it.

- [Intent You Can See (and Optimize)](#intent-you-can-see-and-optimize)
  - [The Problem with Opaque Programs](#the-problem-with-opaque-programs)
  - [Programs as Data](#programs-as-data)
    - [LINQ Extensions — the Monadic Plumbing](#linq-extensions--the-monadic-plumbing)
  - [LINQ — Making It Readable](#linq--making-it-readable)
  - [The Optimization Passes](#the-optimization-passes)
    - [Optimizer 1: Batching](#optimizer-1-batching)
    - [Optimizer 2: Deduplication](#optimizer-2-deduplication)
    - [Optimizer 3: Execution plan / cost estimation](#optimizer-3-execution-plan--cost-estimation)
    - [Optimizer 4: Reordering for parallelism](#optimizer-4-reordering-for-parallelism)
    - [Optimizer 5: AI/LLM call minimization](#optimizer-5-aillm-call-minimization)
    - [The Pipeline](#the-pipeline)
  - [Testing is Even Better — Structural Tests](#testing-is-even-better--structural-tests)
  - [Compensation as an Effect — the Saga for Free](#compensation-as-an-effect--the-saga-for-free)
- [An Honest Note on Analyzability](#an-honest-note-on-analyzability)

---

## The Problem with Opaque Programs

The boss walks in.

> "We're spending too much on payment gateway calls. Can we batch orders that come in within a 100ms window and do a single bulk charge?"

Or:

> "Before we hit the AI pricing engine, can we deduplicate items that appear in multiple concurrent orders?"

Or:

> "Show me the execution plan for this order *before* we run it — like SQL `EXPLAIN`."

With the Tagless Final approach from Post 2, you can't. The program is a generic function. By the time you can observe it, it's already running. You'd need to write a special "batching interpreter" that somehow accumulates calls — but the sequencing is baked into the function's control flow. You can't reorder steps you can't see.

What if the program was *data*?

---

## Programs as Data

The same five operations from the algebra — but now as **descriptions** rather than method calls:

```csharp
public abstract record OrderStep<T>;

public record CheckStock(List<Item> Items) : OrderStep<StockResult>;
public record CalculatePrice(List<Item> Items, Coupon? Coupon) : OrderStep<PriceResult>;
public record ChargePayment(PaymentMethod Method, decimal Amount) : OrderStep<ChargeResult>;
public record ReserveInventory(List<Item> Items) : OrderStep<ReservationResult>;
public record SendConfirmation(Customer Customer, PriceResult Price) : OrderStep<Unit>;
```

These are *values*, not actions. `ChargePayment(Visa, 99.50m)` doesn't charge anyone — it's a note that says "a charge should happen." A description of intent, captured as a C# record. You can hold it, inspect it, compare it, serialize it, put it in a list.

Now chain them into a program:

```csharp
public abstract record OrderProgram<T>;
public record Done<T>(T Value) : OrderProgram<T>;
public record Failed<T>(string Reason) : OrderProgram<T>;
public record Then<T, TNext>(OrderStep<T> Step, Func<T, OrderProgram<TNext>> Continue)
    : OrderProgram<TNext>;
```

Three cases:
- `Done(value)` — the program has finished successfully with this value
- `Failed(reason)` — the program has failed with this reason
- `Then(step, continue)` — do this step, then feed the result to a continuation that produces the rest of the program

`Then` is the crucial one. It says: "here's a step, and here's what to do after it." The continuation is a function from the step's result to the *rest of the program*. This means the program is a chain — a linked list of steps where each step's result feeds into the next.

### LINQ Extensions — the Monadic Plumbing

To enable LINQ syntax, we need `Select` and `SelectMany` on `OrderProgram<T>`:

```csharp
public static class OrderProgramExtensions
{
    public static OrderProgram<T> Lift<T>(OrderStep<T> step) =>
        new Then<T, T>(step, value => new Done<T>(value));

    public static OrderProgram<TResult> Select<T, TResult>(
        this OrderProgram<T> source, Func<T, TResult> selector) =>
        source.SelectMany(value => new Done<TResult>(selector(value)));

    public static OrderProgram<TResult> SelectMany<T, TResult>(
        this OrderProgram<T> source, Func<T, OrderProgram<TResult>> selector) =>
        source switch
        {
            Done<T>(var value) => selector(value),
            Failed<T>(var reason) => new Failed<TResult>(reason),
            Then<T, var TInner>(var step, var cont) inner =>
                new Then<TInner, TResult>(step, x => cont(x).SelectMany(selector)),
            _ => throw new InvalidOperationException()
        };

    public static OrderProgram<TResult> SelectMany<T, TIntermediate, TResult>(
        this OrderProgram<T> source,
        Func<T, OrderProgram<TIntermediate>> collectionSelector,
        Func<T, TIntermediate, TResult> resultSelector) =>
        source.SelectMany(x =>
            collectionSelector(x).Select(y => resultSelector(x, y)));

    public static OrderProgram<T> Where<T>(
        this OrderProgram<T> source, Func<T, bool> predicate) =>
        source.SelectMany(value =>
            predicate(value) ? new Done<T>(value) : new Failed<T>("Guard failed"));
}
```

The `Where` extension is worth highlighting. LINQ's `where` clause becomes a *guard* — a conditional that short-circuits the program with `Failed` if the predicate doesn't hold. In the business logic, `where stock.IsAvailable` reads like a *requirement*, not a control flow decision.

---

## LINQ — Making It Readable

With those extensions, the program becomes a LINQ query:

```csharp
static OrderProgram<OrderResult> PlaceOrder(OrderRequest request) =>
    from stock   in Lift(new CheckStock(request.Items))
    where stock.IsAvailable                                        // ← guard!
    from price   in Lift(new CalculatePrice(request.Items, request.Coupon))
    from charge  in Lift(new ChargePayment(request.PaymentMethod, price.Total))
    where charge.Succeeded                                         // ← guard!
    from _       in Lift(new ReserveInventory(request.Items))
    from __      in Lift(new SendConfirmation(request.Customer, price))
    select OrderResult.Success(charge.TransactionId);
```

Read it aloud: "From the stock check, where stock is available, from the price calculation, from the charge... select success." It reads like a *specification* of the business logic — practically the same list from Post 1 where we asked "what does this code *want* to do?"

The `where` clauses express business rules as guards. If stock isn't available, the program short-circuits to `Failed`. If the charge doesn't succeed, same thing. No `if` statements. No early returns. Just declarative requirements.

Same intent. Same readability. But now `PlaceOrder(request)` returns a **value**:

```
Then(CheckStock([...]),
  stock => Then(CalculatePrice([...]),
    price => Then(ChargePayment(Visa, 99.50),
      charge => Then(ReserveInventory([...]),
        _ => Then(SendConfirmation(...),
          __ => Done(Success("txn-123")))))))
```

That's a data structure. A tree. You can walk it. You can analyze it. You can **transform it**.

---

## The Optimization Passes

This is the section that earns programs-as-data their keep.

### Optimizer 1: Batching

Multiple orders come in concurrently. Each produces an `OrderProgram`. Before interpreting, coalesce them:

```csharp
// Walk the ASTs of 5 concurrent orders
// Find: all have a ChargePayment step
// Rewrite: replace 5 individual ChargePayments with 1 BulkCharge
// Result: 1 API call instead of 5

static List<OrderProgram<OrderResult>> BatchPayments(
    List<OrderProgram<OrderResult>> programs) => ...
```

The rewriter walks each AST, extracts the `ChargePayment` nodes, groups them by payment provider, creates a single `BulkCharge` instruction, and rewrites the individual ASTs to share the result. Five API calls become one. The business logic — each individual `PlaceOrder` — didn't change. The optimization lives *between* the description and the execution.

### Optimizer 2: Deduplication

Two concurrent workflows both call `CheckStock` for the same items:

```csharp
// Before: CheckStock(itemA) + CheckStock(itemA) = 2 DB calls
// After:  CheckStock(itemA) shared across both flows = 1 DB call
```

Walk both ASTs. Find matching `CheckStock` nodes (same items). Replace the duplicate with a shared reference. One database call instead of two.

### Optimizer 3: Execution plan / cost estimation

Walk the AST and produce a report *without executing anything*:

```csharp
static ExecutionPlan Analyze(OrderProgram<OrderResult> program)
{
    // Count: 2 DB calls, 1 payment API call, 1 email
    // Estimated latency: 350ms (DB) + 800ms (payment) + 50ms (email) = 1.2s
    // Estimated cost: $0.03 (payment API) + $0.001 (email API) = $0.031
}
```

This is SQL `EXPLAIN` for your business logic. Show this to your boss. Show this to your compliance team. "Before we process this order, here's exactly what will happen, how long it'll take, and how much it'll cost." Try doing that with opaque function calls.

### Optimizer 4: Reordering for parallelism

Walk the AST. Find steps with no data dependencies. Run them in parallel:

```csharp
// CheckStock and CalculatePrice don't depend on each other (in some flows)
// Before: sequential (1.2s total)
// After:  parallel where possible (0.85s total)

static OrderProgram<T> Parallelize<T>(OrderProgram<T> program) => ...
```

The optimizer inspects data flow through the continuations. If the result of step A isn't used in step B, they can run concurrently. The business logic didn't ask for parallelism — the optimizer discovered it.

### Optimizer 5: AI/LLM call minimization

The pricing engine is an LLM that costs $0.01 per call. Ten orders in a batch each call it:

```csharp
// Before: 10 orders × 1 LLM pricing call = 10 calls ($0.10)
// After:  1 batched prompt with 10 items = 1 call ($0.01)
```

Walk the ASTs, extract all `CalculatePrice` requests, combine them into one context-rich prompt, fan out the results. The business logic says "calculate the price." The optimizer decides *how* to minimize the cost of doing so.

### The Pipeline

With programs as data, interpretation gets a new step:

```
Program (data)  →  Optimize  →  Interpret  →  Result
```

The optimization step sits *between* description and execution. **This is impossible with the function-based approach from Post 2** — you need the program to be data you can walk.

The interpreters themselves are the same idea as Post 2 — production, test, dry-run, narrative. But now they interpret data structures instead of receiving method calls:

```csharp
static async Task<T> Run<T>(OrderProgram<T> program) =>
    program switch
    {
        Done<T>(var value) => value,
        Failed<T>(var reason) => throw new OrderFailedException(reason),
        Then<T, var TStep>(var step, var cont) =>
            Run(cont(await Execute(step)))
    };

static async Task<T> Execute<T>(OrderStep<T> step) =>
    step switch
    {
        CheckStock s => (T)(object)await _inventory.CheckStockAsync(s.Items),
        ChargePayment s => (T)(object)await _payment.ChargeAsync(s.Method, s.Amount),
        // ... pattern match on each step type
    };
```

**The reveal**: This data structure — `Done` plus `Then` — with its LINQ `SelectMany` — is called a **Free Monad**.

"Free" because it's the simplest possible monad you can build over a set of operations. It adds *no interpretation of its own* — it's pure syntax, pure intent, waiting for an interpreter to give it meaning.

The correspondence:
- `Done` = `Return` / `Pure` — inject a value into the program
- `Then` = `Bind` / `FlatMap` — sequence an operation with a continuation
- `OrderStep<T>` = the **effect functor** — the vocabulary of domain operations
- The interpreter = a **fold** over the AST — the same kind of recursive traversal as the Trampoline's `execute` from the [2020 post](/2020/12/07/bouncing-around-with-recursion.html)

If you've read the [Trampoline post](/2026/03/04/the-trampoline-is-a-monad.html), this should feel familiar — because the Trampoline *was* a Free Monad all along. `Suspend` was the effect, `execute` was the interpreter. We just didn't call it that.

---

## Testing is Even Better — Structural Tests

With Tagless Final (Post 2), testing was already great — swap the interpreter, assert the result. But with programs as data, you can test things that were *impossible* before: the **structure** of the program itself.

```csharp
[Test]
public void PlaceOrder_ChecksStockBeforeCharging()
{
    var program = PlaceOrder(request);

    // Walk the AST — no interpreter, no execution, no I/O
    var steps = Flatten(program).Select(s => s.GetType().Name).ToList();

    var stockIndex  = steps.IndexOf("CheckStock");
    var chargeIndex = steps.IndexOf("ChargePayment");

    Assert.That(stockIndex, Is.LessThan(chargeIndex),
        "Must validate stock before charging");
}
```

This test asserts **business sequencing** without running anything. No database. No HTTP calls. Not even an in-memory fake. You're testing the *intent* directly.

More structural tests:

```csharp
[Test]
public void PlaceOrder_NeverChargesWithoutStockCheck()
{
    var program = PlaceOrder(request);
    Assert.That(NeverBefore<ChargePayment, CheckStock>(program));
}

[Test]
public void PlaceOrder_HasExactly5Steps()
{
    var program = PlaceOrder(request);
    Assert.AreEqual(5, CountSteps(program));
}

[Test]
public void PlaceOrder_AlwaysSendsConfirmation()
{
    var program = PlaceOrder(request);
    Assert.That(AllPathsContain<SendConfirmation>(program));
}
```

This is the testing equivalent of SQL `EXPLAIN` — verify the plan without executing it. You can also do **property-based testing** on the structure: generate random `OrderRequest` values, assert that `PlaceOrder` always produces a program where compensation covers every step that has side effects.

With Tagless Final you test *behavior* — does the right result come out? With programs as data you can also test *structure* — is the program shaped correctly? Both are massive upgrades from mock hell.

---

## Compensation as an Effect — the Saga for Free

Remember the compensation nightmare from Post 1? Payment succeeded but inventory reservation failed, and the `PlaceOrder` method doubled in size with try/catch/refund logic?

With programs as data, compensation is *another transformation*. Add a `WithCompensation` step to the instruction set:

```csharp
public record WithCompensation<T>(
    OrderStep<T> Forward,
    Func<T, OrderStep<Unit>> Rollback) : OrderStep<T>;
```

Now express compensation *declaratively* in the LINQ program:

```csharp
static OrderProgram<OrderResult> PlaceOrder(OrderRequest request) =>
    from stock   in Lift(new CheckStock(request.Items))
    where stock.IsAvailable
    from price   in Lift(new CalculatePrice(request.Items, request.Coupon))
    from charge  in Lift(new WithCompensation<ChargeResult>(
                        new ChargePayment(request.PaymentMethod, price.Total),
                        result => new RefundPayment(result.TransactionId)))
    where charge.Succeeded
    from _       in Lift(new WithCompensation<ReservationResult>(
                        new ReserveInventory(request.Items),
                        result => new ReleaseInventory(result.ReservationId)))
    from __      in Lift(new SendConfirmation(request.Customer, price))
    select OrderResult.Success(charge.TransactionId);
```

Read it: "Charge — and if something fails *later*, here's how to undo it. Reserve — and if something fails *later*, here's how to undo that too."

The business logic says *what* can be compensated and *how*. It doesn't implement the compensation strategy — that's the interpreter's job.

The **saga interpreter** walks the AST, executes forward steps, accumulates a compensation stack, and on failure unwinds it:

```csharp
static async Task<T> RunWithSaga<T>(OrderProgram<T> program)
{
    var compensations = new Stack<Func<Task>>();

    try
    {
        return await RunForward(program, compensations);
    }
    catch
    {
        // Unwind: execute accumulated compensations in reverse order
        await Rollback(compensations);
        throw;
    }
}

static async Task<T> RunForward<T>(OrderProgram<T> program, Stack<Func<Task>> compensations)
{
    return program switch
    {
        Done<T>(var value) => value,
        Failed<T>(var reason) => throw new OrderFailedException(reason),
        Then<T, var TStep>(var step, var cont) when step is WithCompensation<TStep> wc =>
            var result = await Execute(wc.Forward),
            compensations.Push(() => Execute(wc.Rollback(result))),
            await RunForward(cont(result), compensations),
        Then<T, var TStep>(var step, var cont) =>
            await RunForward(cont(await Execute(step)), compensations)
    };
}
```

The business logic didn't grow. The compensation logic lives in the interpreter. And because the program is data, you can *also* write interpreters that:

- **Visualize the saga**: draw the forward and rollback paths as a graph
- **Test the compensation**: simulate failure at each step, verify rollback happens in the right order
- **Audit the plan**: "If step 3 fails, we'd refund $99.50 and release 3 items"

This is the saga pattern without the saga framework — without event buses, without state machines, without correlation IDs. Pure intent, composable compensation. The infrastructure that *doubled* the method in Post 1 is now *zero lines in the business logic* and a reusable interpreter.

> **Sidebar (F#):** The computation expression version is, predictably, much cleaner:
>
> ```fsharp
> let placeOrder request = saga {
>     let! stock = checkStock request.Items
>     do! guard stock.IsAvailable "Out of stock"
>     let! price = calculatePrice request.Items request.Coupon
>     let! charge = withCompensation
>                     (chargePayment request.PaymentMethod price.Total)
>                     (fun result -> refundPayment result.TransactionId)
>     do! guard charge.Succeeded "Payment failed"
>     let! _ = withCompensation
>                (reserveInventory request.Items)
>                (fun result -> releaseInventory result.ReservationId)
>     do! sendConfirmation request.Customer price
>     return OrderResult.Success charge.TransactionId
> }
> ```
>
> Also worth noting: the Trampoline from the [2020 post](/2020/12/07/bouncing-around-with-recursion.html) was a Free Monad all along — `Suspend` was the effect, `execute` was the interpreter. We just didn't call it that.

---

## An Honest Note on Analyzability

*Thanks to [George Pollard](https://www.linkedin.com/in/george-pollard-0547a511/) for the keen-eyed observation that prompted this section.*

Everything above works — the structural tests pass, the execution plan is useful, the saga interpreter is sound. But there's a subtlety worth naming: **monadic bind hides the next effect behind an opaque function.**

In `Then(step, continuation)`, the continuation is a `Func<T, OrderProgram<TNext>>` — a function we can't look inside. What happens after `CheckStock` depends on the *value* of `StockResult`. Our `Flatten` helper works by feeding **dummy values** through the continuations to reveal the happy-path spine. That's practical — it's how our `ExecutionPlan` and structural tests work — but it's approximate. We can't enumerate all branches or discover effects that only appear on failure paths.

This is a fundamental tradeoff, not a bug. There's a hierarchy of abstractions — Functor → Applicative Functor → Arrow → Monad — and as you move right, you gain expressiveness but lose analyzability:

| Abstraction | What it adds | What you can see |
|---|---|---|
| **Applicative** | Combine independent effects | All effects visible statically — trivially parallelizable |
| **Arrow** | Forward data between effects | Data flow explicit — analyzable control flow |
| **Monad** | Data-dependent sequencing | Next effect hidden behind `f` — approximate analysis only |

Our order flow is genuinely monadic: `chargePayment` needs `price.Total`, which comes from `calculatePrice`. That's a real data dependency — you can't know the charge amount without first computing the price. An Applicative encoding would lose that, and an Arrow encoding (which *would* give you analyzable data flow) would be impractical in C# without language support for arrow notation.

So the Free Monad is the right tool for this domain in this language. The optimization story is real — happy-path analysis, execution planning, structural testing, and saga compensation all work correctly. Just know that you're analyzing the *linear spine*, not the full branching tree. For domains where you need complete static analysis of all possible effects, look to Applicative Functors or Arrows — Chris Penner's [Monads are too powerful: The Expressiveness Spectrum](https://chrispenner.ca/posts/expressiveness-spectrum) and [Exploring Arrows for sequencing effects](https://chrispenner.ca/posts/arrow-effects) are excellent starting points.

---

## What We Built

Now we have two ways to separate intent from process.

In **Post 2**, programs are *functions* — clean, composable, but opaque. You can run them with different interpreters, but you can't see inside. This is powerful for day-to-day development: easy to extend, zero overhead, familiar to every C# developer.

In **this post**, programs are *data* — inspectable, transformable, optimizable. You can batch, deduplicate, parallelize, explain, *and* compensate — all without touching the business logic. The optimization pipeline sits between description and execution, impossible with the function-based approach.

They look different. They feel different. One uses interfaces and method dispatch; the other uses records and pattern matching. One is opaque; the other is transparent.

But they describe the *same* order flow using the *same* five operations. Both separate intent from process. Both eliminate mock hell. Both give you pluggable interpreters.

In the next post, we'll see that this isn't a coincidence.

> **Sidebar (Haskell):** In Haskell, the Free Monad over a GADT functor gives you the typed instruction set naturally — no boxing, no `OrderStepBase`, no casting:
>
> ```haskell
> data OrderStepF next where
>   CheckStockF       :: [Item] -> (StockResult -> next) -> OrderStepF next
>   CalculatePriceF   :: [Item] -> Maybe Coupon -> (PriceResult -> next) -> OrderStepF next
>   ChargePaymentF    :: PaymentMethod -> Double -> (ChargeResult -> next) -> OrderStepF next
>   ReserveInventoryF :: [Item] -> (ReservationResult -> next) -> OrderStepF next
>   SendConfirmationF :: Customer -> PriceResult -> next -> OrderStepF next
>
> type OrderProgram = Free OrderStepF
>
> placeOrder :: OrderRequest -> OrderProgram OrderResult
> placeOrder req = do
>   stock <- checkStock (orderItems req)
>   guard' (stockIsAvailable stock) "Out of stock"
>   price <- calculatePrice (orderItems req) (orderCoupon req)
>   charge <- chargePayment (orderPaymentMethod req) (priceTotal price)
>   guard' (chargeSucceeded charge) "Payment failed"
>   _ <- reserveInventory (orderItems req)
>   sendConfirmation (orderCustomer req) price
>   pure $ OrderSuccess (chargeTransactionId charge)
> ```
>
> The same do-notation. The same five steps. But now it's a *data structure* — and the GADT keeps everything typed. No `object`, no casts. See the [Haskell companion code](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/haskell/) for structural analysis, execution plan optimizer, and saga interpreter — all working with this typed AST.

---

> **Companion code**: The full working implementation — including LINQ programs, structural helpers, execution plan analyzer, and saga interpreter — is in the [companion repository](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/). Available in [C#](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/csharp/), [F#](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/fsharp/), and [Haskell](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/haskell/).

---

> **Next**: [Two Sides of the Same Coin](/2026/03/05/04-two-sides-of-the-same-coin.html) — where we discover that interfaces-as-programs and data-as-programs are mathematically dual, and learn when to choose which.

---

*This is Part 3 of the series **"Your Clean Architecture Has a Dirty Secret."** The [full series](/tags/software-architecture/) explores separating intent from process using techniques from functional programming — Tagless Final, Free Monads, and the mathematical foundations that make them trustworthy.*
