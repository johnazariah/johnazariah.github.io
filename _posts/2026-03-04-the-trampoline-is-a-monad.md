---
    layout: post
    title: "The Trampoline is a Monad (and that's a good thing)"
    tags: [functional-programming, monads, free-monad, recursion, trampolines, F#, C#]
    author: johnazariah
    summary: A follow-up to "Bouncing around with Recursion" — where we discover that the Trampoline we built was a monad all along, and that recognizing this gives us composability for free.
---

_This post is dedicated to [Rúnar Bjarnason](https://twitter.com/runarorama), whose paper [Stackless Scala With Free Monads](http://days2012.scala-lang.org/sites/days2012/files/bjarnason_trampolines.pdf) planted the seed that grew into this post — and to everyone who, upon encountering a monad tutorial, has asked "but why should I care?"_

- [1. A confession](#1-a-confession)
- [2. Two old friends meet at a bar](#2-two-old-friends-meet-at-a-bar)
  - [The Trampoline (from 2020)](#the-trampoline-from-2020)
  - [The ErrorChecked Monad (from 2022)](#the-errorchecked-monad-from-2022)
- [3. The Anatomy of a Monad: Return, Bind, and Effect](#3-the-anatomy-of-a-monad-return-bind-and-effect)
  - [The effect is what makes each monad unique](#the-effect-is-what-makes-each-monad-unique)
  - [Now look at the Trampoline](#now-look-at-the-trampoline)
- [4. The Monad laws — or, why the execute function works](#4-the-monad-laws--or-why-the-execute-function-works)
  - [Left identity](#left-identity)
  - [Right identity](#right-identity)
  - [Associativity](#associativity)
- [5. The reassociation trick demystified](#5-the-reassociation-trick-demystified)
- [6. From Trampoline to Free Monad](#6-from-trampoline-to-free-monad)
  - [The "free" construction](#the-free-construction)
  - [Why "Free"?](#why-free)
- [7. So what? — Why this matters in practice](#7-so-what--why-this-matters-in-practice)
  - [Composability](#composability)
  - [Syntactic sugar — Computation Expressions for the Trampoline](#syntactic-sugar--computation-expressions-for-the-trampoline)
    - [Factorial with the CE](#factorial-with-the-ce)
    - [Tree traversal with the CE](#tree-traversal-with-the-ce)
  - [Meanwhile, in C#... — LINQ as a Computation Expression](#meanwhile-in-c--linq-as-a-computation-expression)
  - [Understanding the machinery](#understanding-the-machinery)
- [8. Conclusion](#8-conclusion)

## 1. A confession

A few years ago, I wrote a [post about recursion, trampolines, and continuations](/2020/12/07/bouncing-around-with-recursion.html). In it, I presented the Trampoline as a kind of clever trick — a piece of machinery for writing stack-safe recursive functions. Near the end, I mentioned in passing that the `Flatten` case was used to "chain computed values to their continuations _monadically_" and then hand-waved vaguely, telling readers to either read Bjarnason's paper or just "accept this as a piece of opaque machinery."

I've been carrying that guilt ever since.

Because here's the thing: the Trampoline isn't just _coincidentally_ monadic. It's a monad _by design_, and understanding _why_ it's a monad is the key to understanding why the whole contraption works — and how to compose trampolined computations with confidence.

I also wrote a [post about monads](/2022/12/06/this-is-not-a-monad-tutorial.html) where I derived the monad pattern from the very practical problem of composing error-checked functions. The punchline of that post was that a monad is really just "composing functions in context."

With those two posts as a backdrop, I owe it to you — and to myself — to close the loop. Let's go.

## 2. Two old friends meet at a bar

Let's put the two constructs side by side and see what happens.

### The Trampoline (from 2020)

```fsharp
type Trampoline<'a> =
    | Return of 'a
    | Suspend of (unit -> 'a Trampoline)
    | Flatten of {| m : 'a Trampoline; f : 'a -> 'a Trampoline |}
```

And we used it like this:

```fsharp
// composing trampolined tree traversals
Suspend (fun () -> foldTrampoline n.Left accum)
>>= (fun left -> Return (consume left n.Value))
>>= (fun curr -> Suspend (fun () -> foldTrampoline n.Right curr))
```

### The ErrorChecked Monad (from 2022)

```fsharp
type ErrorChecked<'v, 'e> =
    | Value of 'v
    | Error of 'e
with
    member this.CallWithValue (op: 'v -> ErrorChecked<'r, 'e>) =
        match this with
        | Error e -> Error e
        | Value v -> op v
```

And we used it like this:

```fsharp
error_checked {
    let! jbConfig = config(host.Jumpbox.AuthConfig)
    let! jbConn = dial(jbConfig)
    return jbConn
}
```

Now, stare at these two for a moment. They look quite different. One has `Return`, `Suspend`, and `Flatten`. The other has `Value` and `Error`. One deals with stack safety; the other deals with error propagation.

And yet, if you squint...

Both have a way to **wrap a plain value**: `Return` for Trampoline, `Value` for ErrorChecked.

Both have a way to **chain a computation that depends on a previous result**: the `>>=` operator for Trampoline (which builds a `Flatten`), and `CallWithValue` for ErrorChecked.

That's `return` and `bind`. That's a monad.

But that's only two ingredients. And both these types have a _third_ thing — the thing that makes them _different_ from each other. Let me pull that thread.

## 3. The Anatomy of a Monad: Return, Bind, and Effect

Most monad tutorials will tell you that a monad is a type `M<'a>` with two operations:

```fsharp
val return : 'a -> M<'a>
val bind   : M<'a> -> ('a -> M<'b>) -> M<'b>
```

And that's _technically_ correct. But I think it misses something important. Let me explain with a thought experiment.

Imagine a monad whose `return` wraps a value and whose `bind` unwraps it, applies a function, and wraps it again. That's all it does: wrap, unwrap, wrap. This monad exists — it's called the **Identity monad** — and it's as interesting as a blank piece of paper. It's the monad equivalent of a `;` in C#: sequencing instructions with no additional behaviour whatsoever.

What makes a monad _interesting_ — what makes it _useful_ — is when something _else_ happens during `bind`. Something beyond just unwrapping and rewrapping. That something else is the **effect**.

### The effect is what makes each monad unique

Let's look at our old friend `ErrorChecked`:

```fsharp
type ErrorChecked<'v, 'e> =
    | Value of 'v     // ← return: a finished value
    | Error of 'e     // ← THE EFFECT: something went wrong
```

The `Value` case is `return` — it wraps a successful result. The `Error` case is something entirely different. It represents the possibility of _failure_. It's the whole reason `ErrorChecked` exists. Without `Error`, we'd just have a value — no point in wrapping it.

And look at what happens in `bind` (which we called `CallWithValue`):

```fsharp
member this.CallWithValue (op: 'v -> ErrorChecked<'r, 'e>) =
    match this with
    | Error e -> Error e   // ← the effect: short-circuit on failure
    | Value v -> op v      // ← the plumbing: pass value to next step
```

The `Value` branch is just plumbing — unwrap, call the function, carry on. The `Error` branch is where the _effect_ lives. It says: "something went wrong upstream, so we're not going to call `op` at all — we're going to propagate the error." That's the whole point. That's what we wanted when we set out to solve the `if err != nil` problem.

So a monad really has _three_ parts:

| Part | Role | ErrorChecked example |
|------|------|---------------------|
| **`return`** | Wrap a plain value | `Value v` |
| **`bind`** | Unwrap, then pass to the next computation | `CallWithValue` |
| **The effect** | The _extra thing_ that happens during bind | `Error` — short-circuit on failure |

`return` and `bind` are the _universal plumbing_ that all monads share. The effect is what makes _this particular monad_ worth using.

### Now look at the Trampoline

Armed with this lens, let's map the Trampoline onto the same three-part anatomy:

```fsharp
type Trampoline<'a> =
    | Return of 'a                                              // ← return: a finished value
    | Suspend of (unit -> 'a Trampoline)                        // ← THE EFFECT: pause and resume
    | Flatten of {| m : 'a Trampoline; f : 'a -> 'a Trampoline |}  // ← bind: chain computations
```

| Part | Role | Trampoline example |
|------|------|-------------------|
| **`return`** | Wrap a plain value | `Return v` |
| **`bind`** | Chain a next computation | `Flatten {\| m; f \|}` |
| **The effect** | The _extra thing_ that happens | `Suspend` — pause execution, resume later |

`Suspend` is the effect. It says: "I haven't finished computing yet, but instead of diving deeper into the call stack right now, here's a _thunk_ — a function you can call whenever you're ready to take the next step." It's the deferred-computation equivalent of `Error`'s short-circuiting.

And just as `Error` was the entire _raison d'être_ of `ErrorChecked` — the thing that made it worth wrapping values in the first place — `Suspend` is the entire _raison d'être_ of the Trampoline. Without `Suspend`, we'd just have `Return` and `Flatten`, which is the Identity monad again. No stack safety. No point.

This is why I think the effect deserves to be called out explicitly. When someone says "the Trampoline is a monad," the natural question is "what _kind_ of monad?" The answer is: **a monad whose effect is suspension** — the ability to pause a computation and hand control back to a driver loop.

Let's be concrete. Here's `Factorial` again from the 2020 post:

```fsharp
let fact n =
    let rec fact' n accum =
        if n = 0 then
            Return accum                                         // return: we're done
        else
            Suspend (fun () -> fact' (n-1) (accum * (bigint n))) // effect: pause, then continue

    fact' n 1I |> execute
```

And here's the doubly-recursive In-Order tree traversal:

```fsharp
let rec foldTrampoline node accum =
    match node with
    | None -> Return accum                                       // return: nothing to do
    | Some n ->
        Suspend (fun () -> foldTrampoline n.Left accum)          // effect: traverse left
        >>= (fun left -> Return (consume left n.Value))          // bind: process current node
        >>= (fun curr -> Suspend (fun () -> foldTrampoline n.Right curr))  // bind + effect: traverse right
```

The tree traversal uses both `Suspend` (the effect) and `>>=` (bind/Flatten). The single recursion of Factorial only needs `Suspend`, because there's only one recursive call in tail position — no chaining required. But the moment you have _two_ recursive calls, you need to compose them, and that's where `bind` earns its keep.

This is exactly parallel to how the ErrorChecked monad works: if you only have one function call, you don't need `CallWithValue`. It's when you need to _chain_ calls — passing the result of one into the next — that the monadic structure becomes essential.

**The monad is the mechanism of composition.** The effect — `Suspend`, `Error`, whatever — is the context in which the composition happens.

## 4. The Monad laws — or, why the execute function works

Here's where things get interesting. Monads must satisfy three laws. These aren't bureaucratic formalities — they are the _reason_ the `execute` function is correct. Let me show you what I mean.

### Left identity
```fsharp
Return a >>= f  ≡  f a
```

_"If you wrap a value and immediately chain a function, it's the same as just calling the function."_

Look at the `execute` function:

```fsharp
| Flatten b ->
    match b.m with
    | Return v ->
        b.f v           // <--- Left identity! We just call f with the value.
        |> execute'
```

The first case of `Flatten` handling is _literally_ the left identity law being applied as a reduction rule. `execute` sees `Return v >>= f` and replaces it with `f v`. If this law didn't hold, this case would be incorrect.

### Right identity
```fsharp
m >>= Return  ≡  m
```

_"If you chain 'just wrap it', you get back what you started with."_

This law ensures that binding with `Return` doesn't introduce any extra computation. It means we can always add `>>= Return` at the end of a chain without changing the result — a useful property when composing a variable number of steps.

### Associativity
```fsharp
(m >>= g) >>= f  ≡  m >>= (fun a -> g a >>= f)
```

_"It doesn't matter whether you group the chain from the left or the right."_

This is the law that makes the `execute` function _terminate correctly_ for deeply nested chains. And it's the law behind the trickiest case in `execute`:

```fsharp
| Flatten f ->
    let fm = f.m
    let ff a = Flatten {| m = f.f a ; f = b.f |}
    Flatten {| m = fm;  f = ff |}
    |> execute'
```

This is _exactly_ the associativity law! We have `Flatten(Flatten(m, g), f)` — which is `(m >>= g) >>= f` — and we rewrite it to `Flatten(m, fun a -> Flatten(g(a), f))` — which is `m >>= (fun a -> g a >>= f)`.

## 5. The reassociation trick demystified

Let me dwell on this, because in the 2020 post I presented this case as opaque machinery. It isn't.

Consider what happens when we traverse a large tree. Each step adds a `>>=` to the chain. The natural structure — traversing left, then processing, then traversing right — builds up chains like this:

```fsharp
((step1 >>= step2) >>= step3) >>= step4
```

This is a **left-leaning** chain. If `execute` tried to evaluate it as-is, it would have to dive into the innermost `Flatten`, peel it open, find _another_ `Flatten` inside, peel _that_ open, and so on — each level adding a frame to the call stack. We'd be right back to our stack overflow problem!

The associativity law lets us rewrite this as:

```fsharp
step1 >>= (fun a -> step2 a >>= (fun b -> step3 b >>= step4))
```

This is a **right-leaning** chain, which `execute` can consume one step at a time: pop off `step1`, evaluate it, pass the result to the continuation, pop off the next step, and so on. Constant stack space.

The loop in `execute` is performing this reassociation _on the fly_, as it encounters left-leaning chains. It's a beautiful piece of engineering, but it's not magic — it's the monad associativity law being applied as a mechanical rewrite rule.

Here is the whole thing, annotated:

```fsharp
let execute (head : 'a Trampoline) : 'a =
    let rec execute' = function
        // A finished computation — return the value
        | Return v -> v

        // A suspended computation — run the thunk and keep going
        | Suspend f ->
            f () |> execute'

        // A composed computation — apply the monad laws to make progress
        | Flatten b ->
            match b.m with
            // Left Identity: Return v >>= f  →  f v
            | Return v ->
                b.f v |> execute'

            // Suspended inner: run the thunk, then re-chain with f
            | Suspend f ->
                Flatten {| m = f (); f = b.f |} |> execute'

            // Associativity: (m >>= g) >>= f  →  m >>= (fun a -> g a >>= f)
            | Flatten inner ->
                let m' = inner.m
                let f' a = Flatten {| m = inner.f a; f = b.f |}
                Flatten {| m = m'; f = f' |} |> execute'

    execute' head
```

Every case of the `Flatten` handler is a monad law being applied to make the computation one step simpler, one step closer to a `Return`. The loop keeps applying these laws until it reaches a terminal value. The monad laws _guarantee_ that this process terminates and produces the correct result.

Let me say that again, because I think it's worth emphasizing: **the monad laws are not abstract niceties. They are the correctness proof of the `execute` loop.**

## 6. From Trampoline to Free Monad

Now I want to step back and ask a deeper question: _where did the Trampoline come from?_

We needed a way to make recursive computations stack-safe. We ended up with a data structure with three cases: `Return`, `Suspend`, and `Flatten`. We then built a loop that interpreted this structure. And we discovered that the structure was a monad.

But did we _discover_ it, or did we _derive_ it?

It turns out there's a construction in category theory called the **Free Monad**. I'm not going to drown you in category theory — instead, let me describe it by analogy.

### The "free" construction

You know how in algebra, you can take a set of generators and build the "free group" over them? You get a group where the _only_ relationships between elements are the ones forced by the group axioms — nothing more. It's the most general group you can build from those generators.

The Free Monad does the same thing, but for monads. You take _any_ type constructor `F` (technically, a functor) and build the most general monad you can from it. The result is:

```fsharp
type Free<'f, 'a> =
    | Pure of 'a
    | Impure of 'f (Free<'f, 'a>)
```

(I'm being deliberately hand-wavy about the F# syntax for higher-kinded types here — F# doesn't natively support them. Bear with me for the concept.)

`Pure` wraps a finished value. `Impure` wraps one layer of the functor `F`, containing a `Free` that represents the rest of the computation.

Now, what happens when `F` is the "thunk" functor — that is, `F<'a> = unit -> 'a`?

```fsharp
type Free<(unit -> _), 'a> =
    | Pure of 'a
    | Impure of (unit -> Free<(unit -> _), 'a>)
```

Rename `Pure` to `Return`, rename `Impure` to `Suspend`, and you get:

```fsharp
type Trampoline<'a> =
    | Return of 'a
    | Suspend of (unit -> Trampoline<'a>)
```

_The Trampoline is the Free Monad over thunks._

And the `Flatten` case? That's actually the `bind` operation — the "free" monad `bind` that any free monad construction gives you. In many presentations, `Flatten` (or `FlatMap`, or `Bind`) is added as an explicit case for _performance_ — it lets the `execute` loop apply bind lazily rather than building up deep closures.

This is exactly what Bjarnason described in his paper, and it's why he titled it "Stackless Scala with **Free Monads**." He didn't _invent_ the Trampoline and then _notice_ it was a monad. He started from the Free Monad, specialized it to thunks, and got the Trampoline as a consequence.

### Why "Free"?

The word "free" means that the structure doesn't commit to any particular interpretation. The Trampoline _describes_ a computation — "do this step, then do that step, then finish" — without _executing_ it. The `execute` function is the **interpreter** that gives meaning to the description.

This separation of description from execution is enormously powerful. In principle, you could write _different_ interpreters for the same trampolined computation:

- One that runs it step-by-step with logging
- One that counts the number of steps
- One that serializes the computation for later resumption
- One that runs it... which is our `execute`

This is the same pattern that powers effect systems, coroutines, and IO monads in various functional languages. Our humble Trampoline is the simplest instance of a much bigger idea.

## 7. So what? — Why this matters in practice

I can hear the pragmatic engineer at the back of the room — the one I used to be — asking: "That's all very elegant, but does it change how I write code?"

Fair question. Here's my answer.

### Composability

When you know the Trampoline is a monad, you know that you can compose trampolined computations _freely_ (pun thoroughly intended). You can write:

```fsharp
let combined =
    trampolinedFunctionA input
    >>= (fun a -> trampolinedFunctionB a)
    >>= (fun b -> trampolinedFunctionC b)

combined |> execute
```

And you know — by the monad laws — that this composition is correct. You don't have to think about whether the intermediate results are being passed correctly, whether the stack is growing, or whether the order of evaluation is being preserved. The laws guarantee it.

Contrast this with rolling your own ad-hoc mechanism for composing recursive computations, where every composition point is a potential bug.

### Syntactic sugar — Computation Expressions for the Trampoline

In F#, because the Trampoline is a monad, we can build a **Computation Expression** (CE) for it. If you've read the [monad tutorial post](/2022/12/06/this-is-not-a-monad-tutorial.html), this is exactly the same trick we used to turn the deeply nested `CallWithValue` chain into the clean `error_checked { ... }` syntax. The same principle, applied to a different effect.

The builder is remarkably small:

```fsharp
type TrampolineBuilder() =
    member _.Return(x)     = Return x
    member _.Bind(m, f)    = Flatten {| m = m; f = f |}
    member _.ReturnFrom(m) = m

let trampoline = TrampolineBuilder()
```

Three members — that's all F# needs to give us the `trampoline { ... }` syntax. Let me explain each one:

| Builder member | Triggered by | What it does |
|---|---|---|
| `Return(x)` | `return x` | Wraps `x` in `Return` — the monad's `return` |
| `Bind(m, f)` | `let! x = m` | Builds `Flatten {\| m; f \|}` — the monad's `bind` |
| `ReturnFrom(m)` | `return! m` | Passes through a `Trampoline` value unchanged |

The key insight is what `let!` desugars to. When the F# compiler sees:

```fsharp
trampoline {
    let! x = someComputation
    // ... rest of the expression using x ...
}
```

It transforms it into:

```fsharp
builder.Bind(someComputation, fun x ->
    // ... rest of the expression using x ...
)
```

Which in our case becomes:

```fsharp
Flatten {| m = someComputation; f = fun x -> ... |}
```

That's the `>>=` chain we've been writing by hand — the compiler writes it for us!

#### Factorial with the CE

Let's start simple. Here's the trampolined factorial _without_ the CE:

```fsharp
let fact n =
    let rec fact' n accum =
        if n = 0 then
            Return accum
        else
            Suspend (fun () -> fact' (n-1) (accum * (bigint n)))
    fact' n 1I |> execute
```

And _with_ the CE:

```fsharp
let fact n =
    let rec fact' n accum =
        if n = 0 then
            trampoline { return accum }
        else
            trampoline {
                return! Suspend (fun () -> fact' (n-1) (accum * (bigint n)))
            }
    fact' n 1I |> execute
```

For this singly-recursive case, the improvement is modest — the `return!` is just passing through the `Suspend` value. The CE really shines when we have _composition_.

#### Tree traversal with the CE

Here's where the payoff arrives. The trampolined in-order traversal _without_ the CE:

```fsharp
let rec foldTrampoline node accum =
    match node with
    | None -> Return accum
    | Some n ->
        Suspend (fun () -> foldTrampoline n.Left accum)
        >>= (fun left -> Return (consume left n.Value))
        >>= (fun curr -> Suspend (fun () -> foldTrampoline n.Right curr))
```

And _with_ the CE:

```fsharp
let rec foldTrampoline node accum =
    match node with
    | None -> trampoline { return accum }
    | Some n ->
        trampoline {
            let! left = Suspend (fun () -> foldTrampoline n.Left accum)
            let  curr = consume left n.Value
            let! result = Suspend (fun () -> foldTrampoline n.Right curr)
            return result
        }
```

Look at that. It reads like a normal recursive function. The `let!` lines mark the points where we suspend and resume — they're the _effect boundaries_ — but the flow of data is immediately clear: traverse left, process current, traverse right, done.

Let me put the unsugared version and the CE version side by side to drive home what the desugaring does:

```fsharp
// What we write:
trampoline {
    let! left   = Suspend (fun () -> foldTrampoline n.Left accum)
    let  curr   = consume left n.Value
    let! result = Suspend (fun () -> foldTrampoline n.Right curr)
    return result
}

// What the compiler generates:
Flatten {|
    m = Suspend (fun () -> foldTrampoline n.Left accum);
    f = fun left ->
        let curr = consume left n.Value
        Flatten {|
            m = Suspend (fun () -> foldTrampoline n.Right curr);
            f = fun result -> Return result
        |}
|}
```

It's the _same_ `>>=` chain — `Flatten` wrapping `Flatten` wrapping `Return` — but the CE syntax lets us write it in a sequential, top-to-bottom style. The monadic plumbing and the effect boundaries are there, but they don't clamour for attention.

### Meanwhile, in C#... — LINQ as a Computation Expression

In the 2020 post, we showed the Trampoline in C#, complete with `Bind` and `Map` methods. But we composed things with chains of `.Bind(left => ...)` calls — functional, but not exactly _idiomatic_ C#.

Now that we know the Trampoline is a monad, we can do better. Remember how the [monad tutorial post](/2022/12/06/this-is-not-a-monad-tutorial.html) showed that LINQ's `from ... in ... select` syntax is C#'s equivalent of F#'s computation expressions? The same trick works here.

All we need is a single `SelectMany` extension method — LINQ's `Bind`:

```csharp
public static class TrampolineLinqExtensions
{
    public static Trampoline<C> SelectMany<A, B, C>(
        this Trampoline<A> ma,
        Func<A, Trampoline<B>> f,
        Func<A, B, C> select) =>
            ma.Bind(a =>
                f(a).Map(b =>
                    select(a, b)));
}
```

This is _identical_ in structure to the `SelectMany` we wrote for `ErrorChecked` in 2022. The pattern is always the same: `Bind` the first computation, `Map` the projection. If you've written it once for one monad, it's mechanical for any other.

Now look what happens to the tree traversal. Here's the `.Bind()` chain version from 2020:

```csharp
Trampoline<R> V(Tree<T> node, R accum) =>
    (node is null)
        ? Trampoline<R>.Return(accum)
        : Trampoline<R>.Suspend(() => V(node.Left, accum))
            .Bind(left => Trampoline<R>.Return(consume(left, node.Value)))
            .Bind(curr => Trampoline<R>.Suspend(() => V(node.Right, curr)));
```

And here's the LINQ version:

```csharp
Trampoline<R> V(Tree<T> node, R accum) =>
    (node is null)
        ? Trampoline<R>.Return(accum)
        : from left   in Trampoline<R>.Suspend(() => V(node.Left, accum))
          let  curr    = consume(left, node.Value)
          from result  in Trampoline<R>.Suspend(() => V(node.Right, curr))
          select result;
```

Read it aloud: "_from_ the left traversal, _let_ the current value be the fold of left with this node, _from_ the right traversal, _select_ the result." That's practically English.

Put the F# CE and the C# LINQ side by side, and the parallel is unmistakable:

```fsharp
// F# Computation Expression
trampoline {
    let! left   = Suspend (fun () -> foldTrampoline n.Left accum)
    let  curr   = consume left n.Value
    let! result = Suspend (fun () -> foldTrampoline n.Right curr)
    return result
}
```

```csharp
// C# LINQ
from left   in Trampoline<R>.Suspend(() => V(node.Left, accum))
let  curr    = consume(left, node.Value)
from result  in Trampoline<R>.Suspend(() => V(node.Right, curr))
select result
```

`let!` in F# maps to `from ... in` in C#. `let` maps to `let`. `return` maps to `select`. The syntactic sugar is different, but the monadic plumbing underneath is _exactly the same_ — `Bind`, `Bind`, `Return`.

The fact that we can take the same Trampoline monad, add one extension method, and get clean LINQ syntax is a testament to Erik Meijer's foresight in designing LINQ as a general-purpose monad comprehension syntax — one that works for _any_ type with `SelectMany`, not just `IEnumerable`. Most C# developers don't realize they've been writing monadic code every time they write a LINQ query. Now you do.

### Understanding the machinery

Perhaps most importantly, recognizing the Trampoline as a monad turns it from "opaque machinery" into something with known, well-studied properties. You don't have to take the `execute` function on faith. You can verify each case against the monad laws. You can prove it terminates. You can extend it with confidence.

The alternative — treating it as a black box — works fine until you need to debug it, extend it, or compose it in ways the original author didn't anticipate. Then you're stuck reverse-engineering something that has a perfectly good mathematical explanation.

## 8. Conclusion

Let me tie the threads together.

In 2020, we built a Trampoline and used it to write stack-safe recursive functions. I presented the `Flatten` case as mysterious machinery and told you to just trust it.

In 2022, we derived the monad pattern from the practical need to compose error-checked functions. The core insight was: a monad is "composing functions in context."

Today, we've closed the loop. The Trampoline is a monad where:

- The **context** is "this computation can pause and resume"
- `Return` is the monad's `return` — wrapping a finished value
- `Flatten` is the monad's `bind` — composing computations that depend on previous results
- `Suspend` is the **effect** — a single step of deferred computation
- The `execute` loop is the **interpreter** — it applies the monad laws as rewrite rules to drive the computation to completion with constant stack space
- And the whole thing is an instance of the **Free Monad** over thunks — the simplest possible monad for suspending and resuming computation

The Trampoline was never "opaque machinery." It was a monad all along. And _that_ — the monadic structure — is what makes it composable, correct, and extensible.

I promised in 2020 that "a full explanation of this may take a post in itself." It's taken me over five years, but here we are.

Happy typing!

---

_If you want to go deeper, I recommend:_
- _[Stackless Scala With Free Monads](http://days2012.scala-lang.org/sites/days2012/files/bjarnason_trampolines.pdf) by Rúnar Bjarnason — the foundational paper_
- _[This is not a Monad Tutorial](/2022/12/06/this-is-not-a-monad-tutorial.html) — where we derived the monad pattern from scratch_
- _[Bouncing around with Recursion](/2020/12/07/bouncing-around-with-recursion.html) — the original trampoline post_
- _[The Parseltongue Chronicles: Taming Recursion with Trampolines](/2026/03/04/the-parseltongue-chronicles-trampolines.html) — the Python version of the trampoline post_
