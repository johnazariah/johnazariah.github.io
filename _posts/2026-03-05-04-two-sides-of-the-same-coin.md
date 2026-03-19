---
    layout: post
    title: "Intent vs Process - Part 4: Two Sides of the Same Coin"
    tags: [C#, F#, software-architecture, tagless-final, free-monad, functional-programming, category-theory]
    author: johnazariah
    summary: "Interfaces vs data structures. Method dispatch vs pattern matching. These aren't competing approaches — they're mathematically dual. Here's what that means and when to choose which."
    update_date: 2026-03-18
---

_This series is dedicated to [Christian Smith](https://www.linkedin.com/in/christian-smith-9562658/), with gratitude for all the insightful conversations that shaped the ideas in these posts._

> **Series: Your Clean Architecture Has a Dirty Secret**
>
> This is Part 4 of a 7-part series on separating intent from process in real-world C#.
>
> 1. [Your Clean Architecture Has a Dirty Secret](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html)
> 2. [The Algebra of Intent](/2026/03/05/02-the-algebra-of-intent.html)
> 3. [Intent You Can See (and Optimize)](/2026/03/05/03-intent-you-can-see-and-optimize.html)
> 4. **Two Sides of the Same Coin** ← you are here
> 5. [Choosing Both](/2026/03/05/04b-choosing-both.html)
> 6. [Standing on the Shoulders of Giants](/2026/03/05/05-standing-on-the-shoulders-of-giants.html)
> 7. [The Strangler Fig](/2026/03/05/06-the-strangler-fig.html)

---

# Two Sides of the Same Coin

Over the last two posts, we built two very different solutions to the same problem.

In [Post 2](/2026/03/05/02-the-algebra-of-intent.html), programs are *functions*. Define an algebra (interface), write a generic program against it, and swap interpreters. Testing, auditing, documentation — all come for free from different interpreters.

In [Post 3](/2026/03/05/03-intent-you-can-see-and-optimize.html), programs are *data*. Define an instruction set (records), build a program as a tree, and walk the tree to interpret, optimize, or transform it. Batching, deduplication, cost estimation, saga compensation — all come from the program being a data structure.

Both describe the same five-step order flow. Both separate intent from process. Both eliminate mock hell. Both give you pluggable interpreters.

How can two such different-looking approaches do the same thing?

- [Side by Side](#side-by-side)
- [The Mathematics of Duality](#the-mathematics-of-duality)
- [The Expression Problem](#the-expression-problem)
- [When to Choose Which](#when-to-choose-which)
- [Choose Both](#choose-both)

---

## Side by Side

| | Post 2 (Tagless Final) | Post 3 (Free Monad) |
|---|---|---|
| Program is... | A generic function `IOrderAlgebra<F> → F<T>` | A data structure `OrderProgram<T>` |
| Operations are... | Interface methods | Record subtypes of `OrderStep<T>` |
| Interpretation is... | Method dispatch | Pattern matching / recursive fold |
| Adding a new interpreter | Easy — implement the interface | Easy — write a new fold |
| Adding a new operation | Easy — add a method (default impl) | Hard — update `OrderStep` + all interpreters |
| Inspecting the program | ❌ Opaque | ✅ Walk the data |
| Optimizing before execution | ❌ Already "running" | ✅ Transform the AST |
| Performance | Direct dispatch, no allocation | Allocates AST nodes |

The left column feels like interfaces and DI — because it is. The right column feels like building an AST and interpreting it — because it is.

Same problem. Same five operations. Same interpreters. Two opposite representations.

---

## The Mathematics of Duality

> **A note before we begin.** Posts 1–3 gave you everything you need to *use* these patterns. The comparison table above and the "when to choose" guide below give you everything you need to *decide between* them. What follows is the *why* — the mathematical structure underneath. If you're not curious about that, **skip ahead to [The Expression Problem](#the-expression-problem)** and you'll lose nothing practical. But if you've ever wondered why these patterns feel so symmetrical, why swapping interpreters just *works*, or why a 1954 theorem gives us confidence that our C# code is correct — read on. This is the payoff.

Everything we've built is an instance of a duality that mathematicians have studied since the 1940s. Understanding it gives you a *tool for thinking* that applies far beyond this series.

> *Thanks to [Mitchell Wand](https://www.khoury.northeastern.edu/people/mitchell-wand/) for detailed feedback on an earlier version of this section, which prompted a more careful treatment of the category-theoretic foundations below.*

### Categories — the Language of Structure

A *category* has three things:

- **Objects** — things
- **Arrows** (morphisms) between objects
- **Composition** — if $f: A \to B$ and $g: B \to C$, then $g \circ f: A \to C$
- Every object has an identity arrow: $\text{id}_A: A \to A$

You already live in one. For each C# type `t`, write $\mathcal{V}(\texttt{t})$ for the set of all values that inhabit it — the "values of" that type. For example, $\mathcal{V}(\texttt{int})$ is the set of all C# integers ($0, 1, -1, 2, \ldots$), and $\mathcal{V}(\texttt{string})$ is the set of all C# strings. These sets are the **objects** of our category.

The **arrows** between objects are functions definable in C#. Some concrete examples:

- `int.ToString()` is an arrow from $\mathcal{V}(\texttt{int})$ to $\mathcal{V}(\texttt{string})$ — it takes any integer and produces a string.
- `int.Parse(s)` is an arrow from $\mathcal{V}(\texttt{string})$ to $\mathcal{V}(\texttt{int})$ — it takes a string and (when valid) produces an integer.
- `s => s.Length` is an arrow from $\mathcal{V}(\texttt{string})$ to $\mathcal{V}(\texttt{int})$.
- `x => x` is the identity arrow on any object — it takes a value and returns it unchanged.

**Composition** works exactly as you'd expect: if `f` goes from $\mathcal{V}(\texttt{int})$ to $\mathcal{V}(\texttt{string})$ and `g` goes from $\mathcal{V}(\texttt{string})$ to $\mathcal{V}(\texttt{int})$, then `g ∘ f` (or in C#, `x => g(f(x))`) goes from $\mathcal{V}(\texttt{int})$ to $\mathcal{V}(\texttt{int})$. For instance, composing `int.ToString()` with `s => s.Length` gives you `n => n.ToString().Length` — an arrow from integers to integers.

(The fact that the set of arrows from $\mathcal{V}(\texttt{t1})$ to $\mathcal{V}(\texttt{t2})$ also happens to be the set of inhabitants of a C# type, `Func<t1, t2>`, is convenient but not essential to the construction.)

That's it. No mystery. A category is just "things with composable arrows." But this simple definition lets us talk precisely about structure — and structure is what this whole series is about.

### Functors — Structure-Preserving Maps

A *functor* maps one category to another while preserving composition and identity. Formally: a functor $F$ maps objects to objects and arrows to arrows, such that $F(\text{id}_A) = \text{id}_{F(A)}$ and $F(g \circ f) = F(g) \circ F(f)$.

In C# terms, an endofunctor on our category is a generic type constructor with a lawful `Select`/`Map` method:

```csharp
// Task<T> is a functor: for any f : A → B, Task<A>.Select(f) : Task<B>
// IEnumerable<T> is a functor: items.Select(f) maps f over every element
```

The `Select` method *is* the functor's action on arrows: given any function $f: \mathcal{V}(A) \to \mathcal{V}(B)$, it produces a function from $F(\mathcal{V}(A))$ to $F(\mathcal{V}(B))$, and it respects composition and identity.

The **effect functor** is the heart of this entire story. But there's a subtlety worth getting right. In the C# code from Post 3, `OrderStep<T>` is a *type-indexed family*: each concrete subtype fixes `T` to a specific result type (`CheckStock` is always `OrderStep<StockResult>`, `ChargePayment` is always `OrderStep<ChargeResult>`). You can't write a `Select` that turns an `OrderStep<StockResult>` into an `OrderStep<string>` — the result type is determined by which operation it is.

The functor structure emerges when — as in the Free Monad construction — you pair each step with a continuation. The `Then(step, continue)` constructor from Post 3 does exactly this: it packages an `OrderStep<T>` together with a `Func<T, OrderProgram<TNext>>`. The Haskell encoding makes this pairing explicit in the functor definition itself:

```haskell
-- Each constructor carries a continuation (result → next)
data OrderStepF next where
  CheckStockF  :: [Item] -> (StockResult -> next) -> OrderStepF next
  ChargePaymentF :: PaymentMethod -> Double -> (ChargeResult -> next) -> OrderStepF next
  ...

-- fmap applies f to the continuation's output
instance Functor OrderStepF where
  fmap f (CheckStockF items k)     = CheckStockF items (f . k)
  fmap f (ChargePaymentF m amt k)  = ChargePaymentF m amt (f . k)
```

Here, `OrderStepF` is an endofunctor on **Hask** (the category of Haskell types and functions). Given any function $f: A \to B$, `fmap f` transforms an `OrderStepF A` into an `OrderStepF B` by post-composing $f$ with the continuation. It preserves identity (`fmap id = id`, since composing with identity doesn't change the continuation) and composition (`fmap (g . f) = fmap g . fmap f`, since post-composition is associative).

In C#, this same structure exists — it's just spread across the `Then` constructor and the `SelectMany` implementation on `OrderProgram<T>`, rather than being a standalone `Select` on `OrderStep<T>`. The functor that the Free Monad *frees* is the signature of operations-with-continuations, whether that signature is packed into a single type (Haskell) or distributed across the program's constructors (C#).

### Natural Transformations — Maps Between Functors

A *natural transformation* $\eta: F \Rightarrow G$ maps one functor to another, transforming $F(A) \to G(A)$ for every type $A$, such that the transformation commutes with mapping:

$$G(f) \circ \eta_A = \eta_B \circ F(f)$$

In English: "it doesn't matter whether you transform then map, or map then transform."

**Our interpreters are natural transformations.** The production interpreter transforms `OrderStep<T> → Task<T>` for every `T`. The test interpreter transforms `OrderStep<T> → Id<T>` for every `T`. The naturality condition says: transforming the result of `CheckStock` and then mapping is the same as mapping then transforming. That's *exactly* the property that makes interpreters composable and interchangeable.

This is why "swap the interpreter" works. It's not a trick — it's naturality. The math guarantees it.

### F-Algebras and F-Coalgebras

Here's where the duality becomes formal.

An **F-algebra** for a functor $F$ is a type $A$ together with a function $\alpha: F(A) \to A$ — a way to *collapse* one layer of structure into a value. Interpreting a Free Monad is folding with an F-algebra: at each `Then` node, evaluate the effect and continue.

An **F-coalgebra** is the dual: a type $A$ together with a function $\beta: A \to F(A)$ — a way to *observe* a value by producing one layer of structure. Tagless Final programs are coalgebraic: given a program state, each algebra method *produces* an effect in the target functor.

| Direction | Construction | Our code | Analogy |
|---|---|---|---|
| $F(A) \to A$ | F-algebra | Interpreter: `OrderStep<Task<T>> → Task<T>` | Folding a tree |
| $A \to F(A)$ | F-coalgebra | Program: `State → OrderStep<State>` | Unfolding a stream |

The **initial F-algebra** is the most general data representation — it's the Free Monad. It represents *all possible* programs as syntax, with nothing added, nothing interpreted.

The **final F-coalgebra** is the most general observation-based representation — it's the Tagless Final encoding. It represents programs by *all possible* ways of observing them, missing nothing.

A deep theorem (Lambek's Lemma + initiality/finality): for well-behaved functors, the initial algebra and final coalgebra are **isomorphic**. They contain the same information, expressed differently. One builds up (constructors), the other tears down (observations). They are dual — and *equal in expressive power*.

**This is why Posts 2 and 3 describe the same programs. It's not a coincidence. It's a theorem.**

### The Simplest Example: Natural Numbers

See the duality in the simplest possible setting:

| | Initial (F-algebra) | Final (F-coalgebra) |
|---|---|---|
| Natural numbers | `Zero \| Succ(n)` — data you build up, then fold | "Anything supporting `+`, `*`, `==`" — defined by what you can *do* with it |
| Your order flow | `Done \| Then(CheckStock, k)` — AST you interpret | `IOrderAlgebra<F>` — defined by its behaviors |
| The insight | "Here's the structure" | "Here's what it means" |

Two views. Same number. Same program. Same information. Constructors vs. observations. Building up vs. tearing down.

### The Connecting Concept: the Effect

Both approaches have the same three parts:

| Part | Free Monad (Post 3) | Tagless Final (Post 2) |
|---|---|---|
| Return | `Done(value)` | `alg.Done(result)` |
| Bind / sequence | `Then(step, continue)` | `alg.Then(first, next)` |
| **The Effect** | `OrderStep<T>` — the DU of operations | `IOrderAlgebra<F>` — the interface methods |

The effect is the *same conceptual thing* — "the domain-specific operations that constitute intent" — represented as data (constructors) in one world and as abstraction (methods) in the other.

Generalizing across this blog's history:

| Domain | Effect (the "what") | As data (Free) | As abstraction (Tagless Final) |
|---|---|---|---|
| Stack-safe recursion | Suspend/resume | `Suspend(thunk)` — [Trampoline](/2020/12/07/bouncing-around-with-recursion.html) | (not used) |
| Error handling | Short-circuit on failure | `Error(e)` — [ErrorChecked](/2026/03/04/the-trampoline-is-a-monad.html) | (not used) |
| Order processing | Business operations | `CheckStock`, `ChargePayment` records | `IOrderAlgebra` methods |
| Game DSL | Game actions | (not used) | `FrogInterpreter` record — [Frog series](/2025/12/12/tagless-final-01-froggy-tree-house.html) |

Every time we've separated intent from process on this blog — whether we called it "the Trampoline," "ErrorChecked," "Tagless Final," or "Free Monad" — we've been working with the same pattern: define a set of effects, then choose an interpretation strategy.

---

## The Expression Problem

The duality has a practical consequence that computer scientists call the **Expression Problem**:

| | Add a new interpreter | Add a new operation |
|---|---|---|
| **Free Monad** | Easy — write a new fold | Hard — change the DU, update *all* folds |
| **Tagless Final** | Easy — implement the interface | Easy — add a method with a default impl |

Tagless Final wins on extensibility. Free wins on inspectability. Neither is strictly "better" — they're *dual*. The right choice depends on what you need.

> **Sidebar (F#):** The duality is visible in about 5 lines. A discriminated union (initial) and a module signature (final) for the same algebra:
>
> ```fsharp
> // Initial encoding — data
> type OrderStep<'t> =
>     | CheckStock of Item list
>     | ChargePayment of PaymentMethod * decimal
>     | ...
>
> // Final encoding — abstraction
> type IOrderAlgebra<'f> =
>     abstract CheckStock : Item list -> 'f
>     abstract ChargePayment : PaymentMethod -> decimal -> 'f
>     ...
> ```
>
> F# makes the symmetry visible because both discriminated unions and abstract types are first-class language constructs. In C#, interfaces and records serve the same role — they're just more verbose.

---

## When to Choose Which

### Choose Tagless Final when:

- You need many interpreters (test, prod, audit, etc.) and easy extension
- You want **zero-cost abstraction** — no intermediate data structure, direct dispatch
- Operations evolve frequently — adding `FraudCheck` shouldn't break 5 interpreters
- You want **interpreter composition** — logging + execution, timing + auditing, etc.
- **Performance matters** — hot paths, tight loops, no GC pressure from AST nodes
- Your team thinks in interfaces and DI (most C# teams)
- You don't need to inspect or optimize the program before running

### Choose Free Monad when:

- You need to **inspect, transform, or optimize** the program before running
- Batching, deduplication, cost estimation, execution planning
- You need to **serialize** the program (send it over a wire, store it, replay it)
- You want **optimization passes** that rewrite the program: coalesce DB calls, parallelize independent steps, minimize LLM invocations
- Stack safety for deep recursion (the [Trampoline](/2020/12/07/bouncing-around-with-recursion.html))
- You want SQL `EXPLAIN` for your business logic
- The set of operations is **stable** — you're not adding new ones every sprint

---

## Choose Both

The most powerful option: write your programs against an algebra (Tagless Final), but have one of your interpreters *produce a Free Monad AST* that you can then optimize and interpret in a second pass.

```csharp
// Day-to-day development: Tagless Final
// Clean, extensible, familiar DI
var result = PlaceOrder(new ProductionOrder(services), request);
var testResult = PlaceOrder(new TestOrder(...), request);

// When you need optimization: one interpreter produces an AST
var program = PlaceOrder(new ToFreeMonad(), request);  // ← Tagless Final → Free Monad

// Now optimize and interpret the AST
var optimized = BatchPayments(Parallelize(program));
var result = await Run(optimized);
```

This is the "have your cake and eat it too" approach:

- **Tagless Final for day-to-day development**: extensibility, ergonomics, zero overhead, interpreter composition
- **Free Monad for the optimization pipeline**: inspectability, transformation, batching, cost estimation

The `ToFreeMonad` interpreter is itself a natural transformation — it maps the algebra's methods to AST constructors. The math guarantees that the resulting AST represents the *same* program as the one you'd get from writing the Free Monad directly. You're not losing information; you're changing representation.

This works because the two encodings are *isomorphic*. The theorem we met earlier — initial algebra ≅ final coalgebra — isn't just theoretical elegance. It's the reason you can freely convert between representations without losing meaning.

---

Two seemingly different techniques — interfaces vs data structures — solving the same problem from opposite ends. That's not a coincidence. It reflects deep mathematical structure that's been studied for decades.

In the [next post](/2026/03/05/04b-choosing-both.html), we'll deliver on the "Choose Both" promise — with an applicative combinator that unlocks parallelism, and the full pipeline from algebra to AST to concurrent execution.

> **Sidebar (Haskell):** The duality is strikingly visible in Haskell. A type class (final) and a GADT (initial) for the same algebra:
>
> ```haskell
> -- Final encoding — the program IS its interpretations
> class Monad m => OrderAlgebra m where
>   checkStock       :: [Item] -> m StockResult
>   chargePayment    :: PaymentMethod -> Double -> m ChargeResult
>   -- ... (type class methods)
>
> -- Initial encoding — the program IS its syntax tree
> data OrderStepF next where
>   CheckStockF   :: [Item] -> (StockResult -> next) -> OrderStepF next
>   ChargePaymentF :: PaymentMethod -> Double -> (ChargeResult -> next) -> OrderStepF next
>   -- ... (GADT constructors)
>
> type OrderProgram = Free OrderStepF
> ```
>
> Both are first-class language constructs. Both produce the same `placeOrder` with do-notation. The symmetry is immediate — no squinting required. See the [Haskell companion code](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/haskell/) where both encodings coexist.

---

> **Companion code**: Both encodings — Tagless Final and Free Monad — are fully implemented with tests in the [companion repository](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/). Available in [C#](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/csharp/) (57 tests), [F#](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/fsharp/) (27 tests), and [Haskell](https://github.com/johnazariah/johnazariah.github.io/tree/main/code/intent-vs-process/haskell/) (29 tests).

---

> **Next**: [Choosing Both](/2026/03/05/04b-choosing-both.html) — where we deliver on the "Choose Both" promise: the applicative combinator that unlocks parallelism, and the full pipeline from algebra to AST to concurrent execution.
>
> *Not interested in parallelism? Skip ahead to [Standing on the Shoulders of Giants](/2026/03/05/05-standing-on-the-shoulders-of-giants.html) for the mathematical foundations, or straight to [The Strangler Fig](/2026/03/05/06-the-strangler-fig.html) for the Monday morning migration plan.*

---

*This is Part 4 of the series **"Your Clean Architecture Has a Dirty Secret."** The [full series](/tags/software-architecture/) explores separating intent from process using techniques from functional programming — Tagless Final, Free Monads, and the mathematical foundations that make them trustworthy.*
