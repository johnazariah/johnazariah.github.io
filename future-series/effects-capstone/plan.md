# Capstone Post: Free Monads, Tagless Final, and the Algebra of Effects

## Metadata

- **Working title**: "Free Monads, Tagless Final, and the Algebra of Effects"
- **Subtitle/summary**: Three ways to separate *what* from *how* — and how they're all the same idea in disguise.
- **Target date**: 2026-03-XX
- **Tags**: `[functional-programming, monads, free-monad, tagless-final, algebraic-effects, F#, C#, effect-systems]`
- **Format**: Single capstone post (~4000-5000 words)
- **Running example**: The Frog DSL from the Tagless Final series
- **Languages**: F# primarily, C# for LINQ parallels, pseudocode for algebraic effects
- **Category theory depth**: Moderate — include initial/final encoding duality, F-algebras at intuition level

## Predecessor Posts (reader assumed to know these)

| Post | Year | Concept introduced |
|------|------|--------------------|
| **Bouncing around with Recursion** | 2020 | Trampolines: `Return`, `Suspend`, `Flatten`, `execute` |
| **This is not a Monad Tutorial** | 2022 | Monad = composing functions in context; `return`, `bind`, effect |
| **Tagless Final in F# (6 parts)** | 2025 | DSLs, interpreters-as-records, computation expressions, algebra as contract |
| **The Trampoline is a Monad** | 2026 | Trampoline = Free Monad over thunks; the three-part anatomy (return/bind/effect) |

## Thesis

Every post on this blog about "separating description from execution" has been a different angle on the same fundamental idea. This post makes that explicit by showing three formulations — Free Monads (data), Tagless Final (functions), and Algebraic Effects (language-level) — applied to the same example.

---

## Detailed Outline

### 1. Introduction: "We've been circling the same idea" (~400 words)

Open with the observation that across 6 years and ~15 posts, we keep arriving at the same place:

- 2020: "Build a data structure describing the computation, then interpret it" (Trampoline)
- 2022: "Wrap values in a context, compose functions that work in that context" (Monad)
- 2025: "Write your program against an abstract algebra, swap interpreters" (Tagless Final)
- 2026: "The Trampoline was a Free Monad all along, and the effect is what makes it interesting"

Thesis: These are three answers to one question: **"How do I write a program that describes _what_ to do without committing to _how_ to do it?"**

Preview the three encodings:

| Encoding | Program is... | Effects are... | Interpretation is... |
|----------|---------------|----------------|---------------------|
| Free Monad | Data (an AST) | Constructors of a DU | Pattern matching (fold) |
| Tagless Final | A function | Methods of an interface | Method dispatch |
| Algebraic Effects | First-class | Declared operations | Effect handlers |

### 2. The Frog DSL, Three Ways (~1200 words)

#### 2a. Recall: Tagless Final (what readers already know)

Brief recap of the Frog algebra from Part 1:

```fsharp
type FrogInterpreter<'a> = {
    Jump   : unit -> 'a
    Croak  : unit -> 'a
    EatFly : unit -> 'a
    Bind   : 'a -> (unit -> 'a) -> 'a
    Return : unit -> 'a
}
```

And a sample program:

```fsharp
let adventure = frog {
    jump
    croak
    eat_fly
}
```

The program is **a function** `FrogInterpreter<'a> -> 'a`. No data structure. The meaning is determined by which interpreter you pass in. Two interpreters shown: Storyteller (string) and Simulator (state → state).

**Key property**: the program never becomes a value you can inspect. It's opaque — you can run it, but you can't look inside.

#### 2b. The same DSL as a Free Monad

Now define the Frog DSL as a functor:

```fsharp
type FrogF<'next> =
    | Jump   of 'next
    | Croak  of 'next
    | EatFly of 'next
```

And the Free Monad over it:

```fsharp
type FrogProgram<'a> =
    | Done of 'a
    | Step of FrogF<FrogProgram<'a>>
```

Smart constructors:

```fsharp
let jump   = Step (Jump   (Done ()))
let croak  = Step (Croak  (Done ()))
let eatFly = Step (EatFly (Done ()))
```

With a `bind` that threads through the structure:

```fsharp
let rec bind (m: FrogProgram<'a>) (f: 'a -> FrogProgram<'b>) : FrogProgram<'b> =
    match m with
    | Done a -> f a
    | Step instr -> Step (mapF (fun rest -> bind rest f) instr)
```

The same adventure becomes:

```fsharp
let adventure =
    jump >>= fun () ->
    croak >>= fun () ->
    eatFly
```

Or with a CE builder:

```fsharp
let adventure = frogFree {
    do! jump
    do! croak
    do! eatFly
}
```

Now write an interpreter by folding over the AST:

```fsharp
let rec runStory = function
    | Done _ -> "The End."
    | Step (Jump k)   -> "Froggy jumps! " + runStory k
    | Step (Croak k)  -> "Ribbit! " + runStory k
    | Step (EatFly k) -> "*munch* " + runStory k
```

**Key property**: the program IS a data structure. `adventure` is a value: `Step(Jump(Step(Croak(Step(EatFly(Done ()))))))`. You can inspect it, transform it, count the steps, optimize it.

#### 2c. The same DSL with Algebraic Effects (conceptual / pseudocode)

Show what it would look like in a language with algebraic effects:

```
effect Frog =
    operation jump   : unit -> unit
    operation croak  : unit -> unit
    operation eatFly : unit -> unit

let adventure () =
    jump ()
    croak ()
    eatFly ()
```

Handler:

```
handle adventure () with
    | jump k   -> print "Froggy jumps!"; k ()
    | croak k  -> print "Ribbit!"; k ()
    | eatFly k -> print "*munch*"; k ()
    | return _ -> print "The End."
```

**Key property**: the program looks like normal code with function calls. Effects are declared, not encoded. The handler gets access to the continuation `k` — it can call it, ignore it, call it twice (nondeterminism!), etc.

Emphasize: this is very close to the Frog interpreter record from Tagless Final (same operations!), but with the added power of continuations (like Free).

#### 2d. Side-by-side comparison table

| | Tagless Final | Free Monad | Algebraic Effects |
|---|---|---|---|
| `jump` is... | A method call | A constructor | An effect operation |
| Program is... | `Interpreter -> 'a` | `Step(Jump(...))` | Plain function |
| Interpreter is... | A record of functions | A recursive function | A handler |
| Multiple interpreters? | Pass different records | Write different folds | Install different handlers |
| Inspect the program? | No | Yes | Via continuation |
| Performance | Direct dispatch | Allocates AST nodes | Implementation-dependent |

### 3. The Duality: Initial vs Final Encoding (~800 words)

Now formalize what we just saw.

#### The Expression Problem connection

- **Initial encoding** (Free): defined by constructors. Adding a new **interpreter** is easy (write a new fold). Adding a new **operation** is hard (change the DU, update all interpreters).
- **Final encoding** (Tagless Final): defined by observations. Adding a new **operation** is easy (add a method). Adding a new **interpreter** is also easy (write a new implementation). But: you can't inspect the program.

#### F-algebras and F-coalgebras (at intuition level)

The Free Monad is an **initial F-algebra**: the "simplest" structure that has the shape of `F` and supports folding. It's the AST — pure syntax, no interpretation baked in.

Tagless Final is a **final F-coalgebra** (loosely): the "richest" structure that supports the observations defined by `F`. It IS the interpretation — there's no syntax, only behaviors.

Draw the analogy:
- A **number** can be represented as `Zero | Succ of Nat` (initial — it's data, you fold over it) or as "something that supports addition, multiplication, comparison" (final — it's defined by what you can do with it).
- The Free Monad is the `Zero | Succ` version of a DSL.
- Tagless Final is the "defined by operations" version.

Both represent the same abstract thing — the DSL — but from opposite ends.

#### When to pick which

Give the concrete decision: the Trampoline NEEDS to be Free (the reassociation trick requires inspecting `Flatten(Flatten(...))` — a data operation). The Frog DSL is better as Tagless Final (many interpreters, no need to inspect). Connect to the blog's own history: Free appeared when we needed stack safety (data matters), Tagless Final appeared when we needed verification (interpretation flexibility matters).

### 4. Formalizing the Effect (~800 words)

#### The effect is the functor F

Return to the "three-part anatomy" from the Trampoline post (return, bind, effect) and generalize:

| Monad | `return` | `bind` | Effect (functor F) |
|-------|----------|--------|--------------------|
| Trampoline | `Return v` | `Flatten` | `Suspend` — pause/resume (thunk functor) |
| ErrorChecked | `Value v` | `CallWithValue` | `Error e` — short-circuit (const functor) |
| FrogProgram | `Done v` | the `>>=` we defined | `Step(Jump\|Croak\|EatFly)` — frog instructions |
| State | `return v` | `bind` | `get`/`put` — thread mutable state |

In every case:
- `return` and `bind` are the **universal plumbing** (they make it a monad)
- The effect is the **domain-specific behavior** (what makes THIS monad worth using)
- The functor's **shape** determines what effects are available

#### The effect as type parameter

Show how Free<F, A> literally type-parameterizes the effect:

```fsharp
// F = ThunkF        → Trampoline (suspend/resume)
// F = FrogF         → Frog DSL (jump/croak/eatFly)  
// F = ConsoleF      → Console IO (readLine/writeLine)
// F = Const<E>      → Error monad (short-circuit)
```

The functor F IS the formalized effect. This is the answer to "how do we type-parameterize the effect" from our earlier conversation.

In Tagless Final, the same role is played by the **interface/algebra** — the set of abstract operations. Different interfaces = different effects.

In Algebraic Effects, it's the **effect declaration** — `effect Frog = ...`. Same concept, three representations.

### 5. The Modern Synthesis: Algebraic Effects (~600 words)

**Keep this conceptual — no specific language, F#/C# framing.**

Algebraic Effects combine the best of both:
- Effects are **declared** like interface methods (Tagless Final style)
- Programs look like **normal functions** with direct-style calls (not wrapped in monadic plumbing)
- Handlers get access to **continuations** — they can inspect, transform, resume, or discard (Free style)
- Handlers are **composable** — you can stack them, nest them, have different parts of the program handled by different handlers

The key insight: algebraic effects factor the problem into:
1. **Effect signature** (= the functor F = the algebra = the interface)
2. **Program** (= the Free AST = the tagless function = just... code)
3. **Handler** (= the fold/interpreter = the implementation = the effect handler)

All three approaches have these three parts. Algebraic effects just make them all first-class with clean syntax.

#### Where .NET is / could be heading

Brief mention:
- F# computation expressions already give us syntax for monadic effects (we use them!)
- C# LINQ is a limited version of the same thing
- Neither has native algebraic effects (yet)
- Libraries like `LanguageExt` in C# approximate this
- OCaml 5, Koka, Eff, Unison have native support
- The conceptual framework is already here — the blog has been building it!

### 6. When to Use What (~400 words)

Decision guide:

**Use Free Monads when:**
- You need to **inspect, optimize, or serialize** the program before running it
- Stack safety via reassociation (Trampoline)
- Building a compiler or optimizer
- Example from blog: the Trampoline (`execute` needs to pattern-match on `Flatten`)

**Use Tagless Final when:**
- You want **zero-cost abstraction** (no intermediate data structure)
- You need **many interpreters** for the same program
- Your team is comfortable with generic/polymorphic programming
- Example from blog: the Frog/Elevator DSLs (simulators, verifiers, pretty-printers)

**Use Algebraic Effects when:**
- Your language supports them (Koka, OCaml 5, Eff)
- You want the **cleanest syntax** with the most power
- You need reification AND direct-style programming
- Not yet practical in F#/C# — but the mental model is valuable

**The pragmatic default**: In F#, Tagless Final with computation expressions covers most use cases beautifully. Reach for Free when you need data-level access to the program structure.

### 7. Conclusion: "The same idea, wearing different clothes" (~300 words)

Bring it full circle:

> In 2020, we built a data structure to describe recursive computations. In 2022, we discovered that wrapping values in a context and composing them was a pattern called "monad." In 2025, we wrote DSLs as abstract functions and swapped interpreters. In 2026, we closed one loop (the Trampoline is a Free Monad) and now we've closed the other (Free Monads and Tagless Final are dual encodings of the same separation).
>
> The thread running through all of it: **describe what you want, defer how it happens.**
>
> Free Monads do it with data. Tagless Final does it with functions. Algebraic Effects do it with language support. But the algebra — the set of operations, the effects, the domain-specific behaviors — is the same in all three.

Link back to all predecessors. Tease potential future exploration (algebraic effects in F# via delimited continuations, effect libraries, etc.).

---

## Open Questions for Writing

1. **Title**: "Free Monads, Tagless Final, and the Algebra of Effects" or something catchier?
2. **The formal duality section**: how deep on F-algebras? Current plan: analogy-level (Nat = Zero|Succ vs "supports arithmetic"), not categorical definitions.
3. **Code**: should the Free Monad Frog DSL be working F# that readers can run? Or is pseudocode sufficient since the Tagless Final version already has runnable code?
4. **The `choose`/nondeterminism angle**: Tagless Final Part 2 adds `choose` for nondeterminism. This maps beautifully to algebraic effects (a handler that calls the continuation multiple times). Worth including, or does it bloat the post?
5. **Length check**: estimated ~4500 words. Is that okay, or trim to ~3500?

---

## Cross-References to Include

- [Bouncing around with Recursion (2020)](/2020/12/07/bouncing-around-with-recursion.html)
- [This is not a Monad Tutorial (2022)](/2022/12/06/this-is-not-a-monad-tutorial.html)
- [A Tale of Two Languages (2018)](/2018/12/04/tale-of-two-languages.html)
- [Tagless Final Part 1: Froggy Tree House (2025)](/2025/12/12/tagless-final-01-froggy-tree-house.html)
- [Tagless Final Part 6: Code as Model (2025)](/2025/12/12/tagless-final-06-model-verification.html)
- [The Trampoline is a Monad (2026)](/2026/03/04/the-trampoline-is-a-monad.html)
- [The Parseltongue Chronicles: Trampolines (2026)](/2026/03/04/the-parseltongue-chronicles-trampolines.html)
- Bjarnason, "Stackless Scala With Free Monads" (external)
