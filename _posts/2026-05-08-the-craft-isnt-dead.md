---
    layout: post
    title: "The Craft Isn't Dead. You Just Never Learned It."
    tags: [software-architecture, functional-programming, AI, tagless-final]
    author: johnazariah
    summary: "Jensen Huang says don't learn to code. Karpathy says English is the programming language. Uncle Bob is having the time of his life with Claude. They're all wrong — and right — in ways none of them are saying."
---


# The Craft Isn't Dead. You Just Never Learned It.

Let me tell you about the worst take in technology right now.

It's not that AI can write code. It can. Spectacularly well, sometimes. It's not that vibe coding works for prototypes. It does. Karpathy was right about that.

The worst take is the logical leap that follows: _"Therefore, learning the craft of software engineering is a waste of time."_

Jensen Huang, the CEO of the most valuable company on Earth, stood on a stage in Dubai and told a room full of world leaders: **"Nobody has to program. The programming language is human."** Andrej Karpathy, one of the most respected AI researchers alive, coined the term **"vibe coding"** — write code by feel, let the AI fill in the gaps, don't sweat the details. And on LinkedIn, Lee Mager celebrated Uncle Bob Martin's excited adoption of Claude Code as proof that the discourse has shifted: _from "AI is fancy autocomplete" to "holy crap this is doing weeks of high quality work in a single session."_

And the crowd goes wild. The craft is dead! Long live the vibe!

Here's my problem with all of this: **these people have never told you what "code properly" actually means.** They're knocking down a version of craftsmanship that was never the real thing.

---

## The Strawman Craftsman

When Huang says "nobody has to program," what does he mean by "program"? He means syntax. Semicolons. Curly braces. The mechanical act of translating intent into a language a computer understands.

And he's right! Nobody should have to memorize that `std::vector<int>::const_iterator` is how you iterate a list in C++. That's obviously something a machine should handle.

But that was never what "learning to code properly" meant. That's not craftsmanship. That's _typing_.

When Karpathy describes vibe coding, he describes writing code without fully understanding it, leaning on AI to fill the gaps. And for exploration and prototyping, this is genuinely powerful. I do it myself.

But he's describing a **mode of work**, not a **philosophy of engineering**. Vibe coding a prototype is smart. Vibe coding a payment system is negligence.

The craft was never about syntax. It was about **structure**.

And here's the uncomfortable truth: **most developers never learned the structure either.**

The industry has spent decades teaching people to cargo-cult _patterns_. Factory pattern. Strategy pattern. Repository pattern. Decorator pattern. Developers memorize these like recipes — "when you see X, apply Y" — without ever understanding _why_ the pattern exists, what mathematical structure it encodes, or when it actually applies versus when it's just ceremony that makes the code look "enterprise-grade."

This is the real problem. Not that the craft is dead. That most people were never practising it. They were practising _rituals_. Finger-in-the-air design. "I saw this in a blog post once." "Uncle Bob said to do it this way." "The Gang of Four book says..."

Nobody ever said: **"Find the structure in your problem first."**

Nobody said: "Your domain has a _shape_. It has operations that compose in specific ways. It has invariants that can be expressed as types. There is a mathematical structure here, and if you find it, everything else falls out naturally — the tests, the architecture, the extensibility, the verification."

Instead, they learned to paste patterns on top of structureless code and call it architecture.

---

## What the Craft Actually Is (And Why Patterns Aren't It)

Let me show you a piece of code that would pass any code review at any company on Earth:

```csharp
public async Task<OrderResult> PlaceOrder(OrderRequest request)
{
    var stock = await _inventory.CheckStock(request.Items);
    if (!stock.IsAvailable) return OrderResult.Failed("Out of stock");

    var price = _pricing.Calculate(request.Items, request.Coupon);

    var charge = await _payment.Charge(request.PaymentMethod, price.Total);
    if (!charge.Succeeded) return OrderResult.Failed("Payment failed");

    await _inventory.Reserve(request.Items);
    await _email.SendConfirmation(request.Customer, price);

    return OrderResult.Success(charge.TransactionId);
}
```

SOLID. Dependency injection. Interface segregation. Clean Architecture textbook material. This is _the best code the patterns community knows how to write_.

**This is also the code AI generates.** Not because AI is bad — because this is the ceiling of what pattern-thinking produces. Every tutorial. Every StackOverflow answer. Every "Clean Code" repository. This is the apex. The pinnacle. The industry's best.

And this code is broken. Not syntactically. _Structurally_.

It knows too much. It fuses _what_ it wants to do (validate, price, charge, reserve, notify) with _how_ it does it (async I/O, sequential execution, early returns, no retry, no compensation). Change the infrastructure and you rewrite the business logic. Test the intent and you need four mocks. Add logging and it drowns the domain.

Why? Because the developer who wrote this never asked: **"What is the structure of my problem?"**

They asked: "Which _pattern_ should I use?" They reached for the Strategy pattern (interfaces for each service), the Repository pattern (data access abstracted), the Service Layer pattern (business logic in a service class). They applied patterns like stickers on a laptop. And the result is code that _looks_ clean but has no structural integrity.

Here's the question nobody asked: _"What are the operations in my domain, and how do they compose?"_ Not which pattern to use. What the _shape_ of the problem is.

Because this problem has a shape. It's a sequence of domain operations with branching on failure. It's a monad. And if you find that structure — if you _see_ it — everything changes.

When Jensen Huang says "nobody needs to program," he's looking at _this_ code — the best that patterns can do — and concluding that since AI generates it fine, humans are redundant. And if this were all there was to programming, he'd be right.

But it's not.

---

## Finding the Structure

What if, instead of reaching for a pattern, you looked at your problem and asked: _"What is the structure here?"_

The `PlaceOrder` method has five domain operations. They compose sequentially. Some of them can fail, and failure short-circuits the rest. The operations are independent of _how_ they execute — they could be synchronous, asynchronous, batched, retried, logged, or dry-run.

That's a structure. Specifically, it's a **sequential process with effects and failure**. Mathematicians have been studying this structure since the 1960s. It's called a _monad_. It has laws. It has properties. And once you see it, you can encode it.

What if you could write this?

```fsharp
let adventure = frog {
    jump
    croak
    jump
    eat_fly
}
```

This doesn't _do_ anything. It describes _intent_. To make it do something, you give it an interpreter — a record of functions that decides what "jump" and "croak" _mean_:

```fsharp
// Interpreter 1: Tell a story
adventure storyTeller   // → "Froggy jumps!\nRibbit!\nFroggy jumps!\nYum!"

// Interpreter 2: Simulate physics
adventure simulator     // → { Height = 2; Hunger = 0 }

// Interpreter 3: Check for danger
adventure safetyInspector  // → false (no deaths found)

// Interpreter 4: Draw a decision graph
adventure graphBuilder  // → Graph { nodes = [...]; edges = [...] }
```

One program. Four completely different meanings. No rewrites. No adapters. Just swap the interpreter.

This is called **Tagless Final**, and it's the real craft — the one nobody teaches and the one AI can't replace.

Or rather: it's the one AI **amplifies**.

But I'm getting ahead of myself. Let me show you _why_ this works — and why it's not just a clever trick, but a mathematical inevitability.

---

## The Expression Problem: What Patterns Are Secretly Fighting

Here's the thing nobody tells you in a patterns book: there's a famous problem in computer science — named by [Shriram Krishnamurthi](https://bsky.app/profile/shriram.bsky.social) and popularized by Philip Wadler — called the **Expression Problem**, and every design pattern you've ever memorized is a workaround for it.

It goes like this:

You have some operations (check stock, charge payment, etc.) and some interpretations (run in production, test, audit). You want to be able to add new operations _and_ new interpretations without breaking existing code.

In standard OOP, you pick one axis and suffer on the other:

- **Interfaces** (the Strategy/Repository/Service patterns): adding a new interpretation is easy — just implement the interface. Adding a new operation? You change the interface, and _every_ implementation breaks.

- **Data types** (switch/case, discriminated unions): adding a new interpretation is easy — just write a new fold. Adding a new operation? You change the data type, and _every_ fold breaks.

Pattern-thinkers never see this. They just feel the pain and apply more patterns. Visitor pattern to work around closed data types. Abstract Factory to work around closed interfaces. Adapter, Bridge, Decorator — layers of indirection papering over the fundamental constraint.

But this isn't a design problem. It's a _mathematical_ constraint. And it has two precise, well-studied solutions — not patterns, not heuristics, but _constructions_ with proofs.

---

## Two Sides of the Same Coin

### Side 1: Tagless Final — Programs as Functions

The Froggy example above is **Tagless Final**. The program is a function waiting for an interpreter. You write your logic once, generically, and swap implementations:

```fsharp
type FrogInterpreter<'a> = {
    Jump   : unit -> 'a
    Croak  : unit -> 'a
    EatFly : unit -> 'a
}

// The program is a FUNCTION: give me an interpreter, I'll give you a result
type FrogProgram<'a> = FrogInterpreter<'a> -> 'a
```

The key property: the program is **opaque**. You can run it, but you can't look inside it. You can't inspect it, optimize it, or transform it before execution. And the algebra captures only the domain operations — composition is handled separately by the [computation expression builder](/2025/12/12/tagless-final-01-froggy-tree-house.html).

### Side 2: Free Monad — Programs as Data

What if the program _was_ data? What if instead of calling methods, you built a tree of instructions?

```csharp
// The program is DATA: a tree you can walk, inspect, transform
public abstract record OrderProgram<T>;
public record Done<T>(T Value) : OrderProgram<T>;
public record Then<T, TNext>(OrderStep<T> Step, Func<T, OrderProgram<TNext>> Continue)
    : OrderProgram<TNext>;
```

Now `PlaceOrder` doesn't _do_ anything — it builds a data structure:

```csharp
static OrderProgram<OrderResult> PlaceOrder(OrderRequest request) =>
    from stock   in Lift(new CheckStock(request.Items))
    where stock.IsAvailable
    from price   in Lift(new CalculatePrice(request.Items, request.Coupon))
    from charge  in Lift(new ChargePayment(request.PaymentMethod, price.Total))
    where charge.Succeeded
    from _       in Lift(new ReserveInventory(request.Items))
    from __      in Lift(new SendConfirmation(request.Customer, price))
    select OrderResult.Success(charge.TransactionId);
```

That reads almost identically to the original `OrderService` — but it produces a _value_. A tree. You can walk it, batch the payment calls, deduplicate inventory checks, generate an `EXPLAIN` plan, estimate cost — _before you execute anything_.

### The Duality

Here's the punchline: **these two approaches are mathematically dual**. They contain the same information, expressed differently. One builds up (constructors), the other tears down (observations).

| | Tagless Final | Free Monad |
|---|---|---|
| Program is... | A function | A data structure |
| Operations are... | Interface methods | Data constructors |
| Interpretation is... | Method dispatch | Pattern matching |
| Add a new interpreter | Easy | Easy |
| Add a new operation | Easy (add a method) | Hard (update all folds) |
| Inspect the program? | ❌ No | ✅ Yes |
| Optimize before execution? | ❌ No | ✅ Yes |

And here's what the textbooks don't tell you: **you can have both**. Write your programs against a Tagless Final algebra (clean, extensible, ergonomic). When you need optimization, write _one_ interpreter that produces a Free Monad AST. Then optimize the AST and execute it.

```csharp
// Day-to-day: Tagless Final — clean, extensible, familiar
var result = PlaceOrder(new ProductionInterpreter(), request);

// When the boss wants batching: produce an AST, optimize, then execute
var program = PlaceOrder(new ToFreeMonad(), request);
var optimized = BatchPayments(Parallelize(program));
var result = await Execute(optimized);
```

The math guarantees this works. The initial algebra and final coalgebra are _isomorphic_ — Lambek's Lemma tells you that converting between representations preserves meaning. This isn't a hack. It's a theorem.

---

## Why This Is Mathematical, Not a "Pattern"

I need to say this clearly because it's the part that matters most — both for the AI argument and for the industry's self-image.

**This is not a pattern.** A pattern is a heuristic — "when you see X, try Y." Patterns are remembered and applied. They require no understanding of _why_ they work. You can cargo-cult a pattern and get lucky.

What we've built here is a _mathematical construction_. It has:

- **A definition** (the algebra: a set of operations and composition rules)
- **Laws** (left identity, right identity, associativity)
- **A theorem** (Lambek's Lemma: initial algebra ≅ final coalgebra — the two encodings are isomorphic)
- **Proof-carrying properties** (interpreters compose because of naturality, verification is sound because of exhaustiveness)

The three-part anatomy of every principled program is:

| Part | What it does | Example |
|---|---|---|
| **Return** | Wrap a value | `Done(value)` / `alg.Done(result)` |
| **Bind** | Sequence operations | `Then(step, continue)` / `alg.Then(first, next)` |
| **Effect** | Domain-specific operations | `CheckStock`, `ChargePayment`, `Jump`, `Croak` |

Return and Bind are universal plumbing — they make it a monad. The Effect is the domain-specific behaviour — it's what makes _this_ monad worth using.

These aren't arbitrary choices. They satisfy three laws:

- **Left Identity**: wrapping a value and immediately continuing is the same as continuing directly
- **Right Identity**: wrapping and unwrapping are inverses
- **Associativity**: you can break a workflow into sub-workflows and compose them, and the result is identical to writing the whole thing flat

These laws are _why_ your interpreters compose. They're _why_ you can swap them safely. They're _why_ the `safetyInspector` gives you trustworthy results. The math guarantees it.

And here's the kicker for the AI debate: **AI can verify that code satisfies these laws. AI cannot discover that these laws are the right ones to impose.** That's the difference between a mathematician and an apprentice. The apprentice can check the proof. The mathematician knows what to prove.

Most developers are apprentices. Not because they're stupid — because the industry trained them to be. And the training was worse than "follow the rules, don't ask why." It was: **"Follow the rules. Trust us — we're experts."**

Except the experts weren't. Uncle Bob didn't derive SOLID from first principles. He _intuited_ it — from decades of experience, sure, but intuition isn't foundation. He codified his gut feelings into dogma and called it "Clean Code." The Gang of Four catalogued recurring solutions and called them "Design Patterns." None of them asked: _why_ does the Strategy pattern work? _What_ mathematical structure does Dependency Injection encode? _When_ does the Visitor pattern break, and is there a construction that doesn't?

They were practising folk medicine — effective sometimes, harmful sometimes, and with no theory to tell you which was which. And they built an entire industry of apprentices who memorize the prescriptions without understanding the pharmacology.

The answers _existed_. In 2012, [Erik Meijer](https://en.wikipedia.org/wiki/Erik_Meijer_(computer_scientist)) gave a keynote at YOW! Melbourne called ["I Eat Co-Monads for Breakfast"](https://shinesolutions.com/2012/12/20/yow-2012-melbourne/) where he systematically reduced the entire Gang of Four pattern index to just two mathematical constructions: monads and comonads. Every pattern — Strategy, Visitor, Iterator, Observer, Decorator — was a workaround for a missing language abstraction. The patterns weren't discoveries. They were _symptoms_ of languages that couldn't express the underlying structure directly. I was in the audience, and I've never looked at a design pattern the same way since.

But the industry didn't listen. It kept teaching the patterns. It kept selling the books. It kept training apprentices.

Twenty years of that, and you have a profession that can't tell the difference between ceremony and structure. And now that AI can generate the ceremony faster than any human, they think the craft is dead.

The craft was never the ceremony.

---

## Now Let's Talk About AI

With this structure in place, the division of labour becomes razor-sharp:

### What the human does (the irreducible contribution)

**Find the structure in your problem. Then define your domain with it.**

Not "which pattern should I use?" Not "what does Uncle Bob say?" Not "what did the last codebase I worked on look like?"

_What is the shape of this problem?_

Is it a sequential process with failure? That's a monad. Is it a set of independent operations that can run in parallel? That's an applicative. Is it a state machine with transitions and invariants? That's an algebra with laws. Is it a tree of decisions with branching? That's a nondeterministic computation.

These structures exist in your problem _before_ you write any code. They're not patterns you apply — they're structures you _discover_. And once you discover them, the code writes itself. Or rather — once you discover them, you can _tell AI what to write_, and the types will ensure it writes it correctly.

This requires _understanding_. Not syntax — understanding. You need to know that an elevator must never move with doors open. You need to know that a payment should be charged _after_ inventory is validated. You need to know that `CheckStock` and `CalculatePrice` are independent and can run in parallel, but `ChargePayment` depends on both.

No prompt gives you this. You learn it by understanding your domain and understanding how to express invariants in structure. This is what "learning to code properly" actually means.

### What AI does (the amplified contribution)

**Implement interpreters.** Given a well-defined algebra — a record of typed functions — generating an interpreter is highly constrained. The type system tells you what each method must accept and return. The compiler tells you if you missed a case.

Add `EmergencyStop` to the algebra. The compiler _immediately_ tells you which interpreters need updating. Hand each one to AI. The AI generates the implementation. The compiler verifies it. No vibe required.

**Write optimization passes.** Given a Free Monad AST, AI can write tree transformations: batch payment calls that happen within a window, deduplicate identical `CheckStock` queries, parallelize independent steps. Each transformation is a function from `OrderProgram<T>` to `OrderProgram<T>` — the types constrain the solution space.

**Verify properties.** AI can generate a `safetyInspector` interpreter that exhaustively checks every path through your program. Not random testing. Not sampling. [Exhaustive model checking — I built this with a frog game and used it to verify elevator safety.](/2025/12/12/tagless-final-05-verifying-elevators.html)

### What AI does that _nobody is talking about_

**Refactor old code into good structure.**

This is the real opportunity. Take that `OrderService` from the top of this post — the code everyone approves, the code AI already generates — and use AI to _refactor_ it into principled structure:

1. **Extract the algebra.** "Read this method. List the domain operations it performs. Define an interface with a generic `TResult` parameter."

2. **Write the legacy interpreter.** "Wrap the existing services behind the algebra. Each method delegates to the same service call the original code made."

3. **Rewrite the program against the algebra.** "Express the same business logic — validate, price, charge, reserve, notify — as calls to the algebra, with no infrastructure concerns."

4. **Write a test interpreter.** "No mocks. No Moq. Just a pure, deterministic implementation of the algebra with configurable responses."

Each of these steps is _mechanical_. AI is excellent at them. And each step is _verifiable_ — you can run the old code and the new code against the same inputs and assert identical results.

This is the [Strangler Fig pattern](/2026/03/05/06-the-strangler-fig.html): grow the new structure around the old code, strangle one service at a time, and measure progress by counting how many interpreter methods still delegate to legacy code.

**The irony completes itself.** AI _generated_ the bad code. AI can _refactor_ it into good code. But only if a human knows what "good" looks like. Only if someone understands the algebra.

---

## The Irony

Jensen Huang is right that nobody needs to memorize syntax. Karpathy is right that vibe coding works for exploration. Uncle Bob is right that Claude is fun to work with.

But none of them are saying the thing that matters.

The developers who invest in principled structure — algebras, type safety, separation of intent from process — are the ones who get the _most_ out of AI. They write the 15-line algebra that shapes everything downstream. The contract that constrains a thousand implementations. The structure that makes verification free and testing trivial.

The developers who "just vibe it" are the ones _fighting_ AI. Regenerating. Debugging. Hoping. Prompting harder. Because without structure, AI has nothing to be precise about. It's generating the same fused-intent-and-process code from its training data, over and over, and each generation is independent of the last.

**The craft isn't about syntax. It's about structure. And structure is what makes AI actually work.**

---

## What "Learning to Code Properly" Means in 2026

It doesn't mean memorizing syntax. It doesn't mean typing fast. It doesn't mean knowing which Gang of Four pattern to apply. It means _thinking_.

1. **Find the structure in your problem** — before you write a line of code, before you pick a framework, before you ask AI to generate anything. What are the operations? How do they compose? What are the invariants?
2. **Define your domain as an algebra** — capture the vocabulary as a typed contract, not as a bag of interfaces
3. **Understand the Expression Problem** — and know that it has two mathematical solutions, not twenty design patterns
4. **Know the duality** — Tagless Final for ergonomics, Free Monad for inspection, both when you need power
5. **Understand why it works** — the monad laws aren't academic trivia; they're the reason your interpreters compose and your verification is trustworthy
6. **Design for verification** — build systems where correctness is structural, not incidental

These are not apprentice skills. They're not things you learn by watching tutorials or memorizing recipes. They're the skills of someone who _thinks_ about their domain, who sees the mathematical structure in the problem before reaching for a tool.

And they're the skills AI can't replace — because they're the skills that make AI _work_.

The craft isn't dead. It was never about the things they're saying are dead. It was always about something deeper — something most of the industry was never taught. Not because developers are stupid. Because the industry taught them to be _apprentices_ when it should have taught them to be _thinkers_.

If you want to see what I mean, start with [the series where I built a frog game and ended up with verified elevator controllers](/2025/12/12/tagless-final-01-froggy-tree-house.html). Then read [the one where I took Clean Architecture apart and rebuilt it with algebras — in C#, so you can't claim it's just an FP party trick](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html).

Then tell me the craft is dead.

🐸
