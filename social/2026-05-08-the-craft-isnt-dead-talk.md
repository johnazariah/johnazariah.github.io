# Why Learning to Code Properly Still Matters in the Age of AI

**Speaker**: John Azariah
**Duration**: 35–40 minutes + Q&A
**Audience**: Developers, architects, tech leads — anyone being told "just vibe-code it"

---

## Abstract

Everyone says the same thing: *"Don't worry about the quality of AI-generated code. Focus on outcomes."* Ship fast. Iterate. Let the AI handle the messy details.

This talk argues the opposite.

Not because AI is bad at code. Because it's *good* at code — and principled structure makes it dramatically better. Drawing on two real blog series (a Tagless Final DSL series in F# and a Clean Architecture critique in C#), I'll show how investing in algebraic foundations — separating *what* your code does from *how* it does it — doesn't slow you down. It gives you verification for free, testing without mocks, optimization without rewrites, and a codebase where AI can generate entire interpreters from a single contract.

The developers who learn to code properly aren't competing with AI. They're the ones AI amplifies most.

---

## Talk Structure

| # | Section | Time | Purpose |
|---|---------|------|---------|
| 1 | The Prevailing Wisdom | 5 min | Set up the thesis everyone disagrees with |
| 2 | The Code Everyone Approves | 5 min | The "dirty secret" — intent fused with process |
| 3 | The Frog That Changed Everything | 8 min | Tagless Final: one program, many meanings |
| 4 | From Frogs to Elevators to Orders | 7 min | The algebra is the contract — across domains |
| 5 | What AI Actually Needs From You | 7 min | Why structure amplifies AI |
| 6 | The Punchline | 3 min | Learning to code properly IS the AI strategy |

---

## Section 1: The Prevailing Wisdom (5 min)

### Slide: "The Luminaries Have Spoken"

> **Jensen Huang**, CEO of NVIDIA (World Government Summit, 2024):
> *"It is our job to create computing technology such that nobody has to program... the programming language is human. Everybody in the world is now a programmer."*

> **Andrej Karpathy**, AI researcher, coiner of "vibe coding":
> *"English is the new programming language."*

> **Lee Mager**, LinkedIn (2025), on Uncle Bob Martin's AI conversion:
> *"The shift in discourse from 'AI can at best do autocomplete' to 'holy crap this is doing weeks of high quality work in a single session' has been huge..."*

### Speaker Notes

Open with the names. These aren't anonymous Twitter trolls — these are the CEO of the most valuable company on Earth, one of the most respected AI researchers alive, and a LinkedIn post about the author of *Clean Code* himself falling in love with Claude.

The message is clear: **don't learn to code. Learn to prompt. The craft is dead. Long live the vibe.**

And there's a version of this that's not wrong. If your code is truly throwaway — a one-off script, a prototype, a data migration — then yes, let the AI rip. Don't polish it. Karpathy's "vibe coding" is a real and useful mode of working for exploration and prototyping.

### Slide: "But Here's What They're Actually Saying"

Strip away the clickbait. The real claim has three parts:

1. **AI generates code fast enough that the cost of writing approaches zero**
2. **Therefore, quality doesn't matter — just regenerate**
3. **Therefore, learning the craft of software is wasted effort**

Premise 1 is true. Conclusion 3 is catastrophically wrong. And premise 2 is where the logic breaks.

### Slide: "The Vibe Coding Loop"

```
Prompt → Generate → Ship → Bug
  → Prompt again → Generate → Ship → Different bug
    → Prompt harder → Generate → Ship → Same bug, different place
      → ...
```

The "iterate fast" loop doesn't converge. Each regeneration is independent. There's no accumulation of structural guarantees. You're rolling dice every time. The AI doesn't *learn* from its mistakes within your codebase. It has no memory of what invariants matter. It doesn't know your domain. It doesn't even know what "correct" means for your system.

**You** are the one who knows that. And if you can't express it in structure, the AI can't preserve it.

**Transition**: "Let me show you a different approach. One where learning to code properly doesn't compete with AI — it gives AI something to be *good at*. But first, let me show you the code that *everyone* writes — and that AI writes *for* everyone — and explain why it's broken."

---

## Section 2: The Code Everyone Approves (5 min)

### Slide: "The OrderService"

```csharp
public class OrderService
{
    private readonly IInventoryRepository _inventory;
    private readonly IPricingService _pricing;
    private readonly IPaymentGateway _payment;
    private readonly IEmailService _email;

    public async Task<OrderResult> PlaceOrder(OrderRequest request)
    {
        var stock = await _inventory.CheckStock(request.Items);
        if (!stock.IsAvailable)
            return OrderResult.Failed("Out of stock");

        var price = _pricing.Calculate(request.Items, request.Coupon);

        var charge = await _payment.Charge(request.PaymentMethod, price.Total);
        if (!charge.Succeeded)
            return OrderResult.Failed("Payment failed");

        await _inventory.Reserve(request.Items);
        await _email.SendConfirmation(request.Customer, price);

        return OrderResult.Success(charge.TransactionId);
    }
}
```

### Speaker Notes

This code passes every code review. SOLID principles? Check. Dependency injection? Check. Interface segregation? Check.

But ask two questions:

**What does it *want* to do?** Five things: validate, price, charge, reserve, notify.

**What does it *actually* decide?** Sync vs. async, error strategy, execution order, protocol, observability, failure semantics. The *what* and the *how* are inseparable.

### Slide: "The Testing Tax"

```csharp
var inventory = new Mock<IInventoryRepository>();
inventory.Setup(i => i.CheckStock(It.IsAny<List<Item>>()))
         .ReturnsAsync(new StockResult(true));

var pricing = new Mock<IPricingService>();
pricing.Setup(p => p.Calculate(It.IsAny<List<Item>>(), null))
       .Returns(new PriceResult(99.50m));

var payment = new Mock<IPaymentGateway>();
// ... 20 lines of ceremony for 2 lines of intent
```

Every test mocks four dependencies. Add a fifth dependency — say `IFraudService` — and every test breaks, whether it cares about fraud or not.

**This is the code AI generates.** Not because AI is bad — because this is the code in the training data. Every tutorial, every StackOverflow answer, every "Clean Architecture" example repo. When Jensen Huang says "English is the new programming language," *this* is what the English gets compiled to. Intent and process fused together, untestable without mocks, unchangeable without rewrites.

Vibe-coding this is easy. Vibe-coding this *correctly* is impossible — because correctness isn't a property of the code, it's a property of the *structure*, and there's no structure here to be correct about.

**Transition**: "What if there was a different way to structure code? One where the *what* and the *how* were genuinely separate? Let me introduce you to a frog."

---

## Section 3: The Frog That Changed Everything (8 min)

### Slide: "Meet Froggy 🐸"

```fsharp
let adventure = frog {
    jump
    croak
    jump
    eat_fly
}
```

### Speaker Notes

This is a program. But it doesn't *do* anything yet. It's a description of intent: jump, croak, jump, eat a fly.

To make it do something, you give it an *interpreter*. The interpreter decides what "jump" and "croak" *mean*.

### Slide: "One Program, Many Meanings"

```fsharp
// Interpreter 1: Tell a story
let storyTeller : FrogInterpreter<string> = {
    Jump = fun () -> "Froggy jumps up!"
    Croak = fun () -> "Ribbit!"
    EatFly = fun () -> "Yum, a fly!"
    Bind = fun prev next -> prev + "\n" + next()
    Return = fun () -> ""
}

// Interpreter 2: Simulate physics
let simulator : FrogInterpreter<FrogState -> FrogState> = {
    Jump = fun () -> fun s -> { s with Height = s.Height + 1 }
    Croak = fun () -> fun s -> s
    EatFly = fun () -> fun s -> { s with Hunger = 0 }
    Bind = fun prev next -> fun s -> (next()) (prev s)
    Return = fun () -> id
}
```

Same `adventure`. Two completely different meanings. The storyteller produces a narrative string. The simulator tracks height and hunger. No rewrites. No adapters. Just swap the interpreter.

**Key moment**: Pause here. Let this land. This is the architectural insight that changes everything.

### Slide: "The Algebra is the Contract"

```fsharp
type FrogInterpreter<'a> = {
    Jump   : unit -> 'a
    Croak  : unit -> 'a
    EatFly : unit -> 'a
    Bind   : 'a -> (unit -> 'a) -> 'a
    Return : unit -> 'a
}
```

This record defines a **contract**. It says: "If you want to be a Frog Interpreter, you must know how to handle Jump, Croak, EatFly, and how to glue them together."

The generic `'a` is the magic. For the storyteller, `'a` is `string`. For the simulator, `'a` is `FrogState -> FrogState`. For a graph builder, `'a` is `Graph -> Graph`. For a safety inspector, `'a` is `bool`.

**Transition**: "Now here's where it gets wild. This frog algebra? It turns out to be the same shape as an elevator controller."

---

## Section 4: From Frogs to Elevators to Orders (7 min)

### Slide: "Structural Similarity"

| Frog World | Elevator World | E-Commerce World | Concept |
|:---|:---|:---|:---|
| `Jump` | `MoveUp` | `CheckStock` | State Transition |
| `Croak` | `OpenDoors` | `CalculatePrice` | Action |
| `EatFly` | `CloseDoors` | `ChargePayment` | Action |
| `Choose` | `Choose` | `Guard` | Branching |
| `Die` | `Crash` | `Failed` | Failure State |

### Speaker Notes

Three completely different domains. Same algebraic structure: sequential actions, branching, failure. The algebra IS the contract.

If you've built verification tools for frogs — a safety inspector that checks if any path leads to death — you can **reuse them for elevators** by building a translator.

### Slide: "The Safety Inspector"

```fsharp
let safetyInspector : FrogInterpreter<bool> = {
    Jump = fun () -> false
    Croak = fun () -> false
    EatFly = fun () -> false
    Die = fun _ -> true         // Found a death!
    Bind = fun prev next ->
        if prev then true       // Already dead, short-circuit
        else next()
    Choose = fun options -> options |> List.exists id
}
```

One interpreter. Fifteen lines. And it exhaustively checks **every possible path** through your program for safety violations. Not random testing. Not sampling. Exhaustive model checking.

### Slide: "The Verification Pipeline"

```
Elevator Program
      |
  [safetyModel]  ← translates to Frog, inserts 'die' on violations
      |
  Frog Program
      |
  [safetyInspector]  ← checks if any path leads to 'die'
      |
  bool: SAFE or UNSAFE
```

### Speaker Notes

We've built a verification pipeline by composing interpreters. The elevator program gets translated to a frog program (because they share the same structure), then the frog safety inspector checks it. No new tools needed.

And here's the real-world version. Remember our OrderService?

### Slide: "The Algebra of Intent"

```csharp
public interface IOrderAlgebra<TResult>
{
    TResult CheckStock(List<Item> items);
    TResult CalculatePrice(List<Item> items, Coupon? coupon);
    TResult ChargePayment(PaymentMethod method, decimal amount);
    TResult ReserveInventory(List<Item> items);
    TResult SendConfirmation(Customer customer, PriceResult price);

    TResult Then<T>(TResult first, Func<T, TResult> next);
    TResult Done(OrderResult result);
}
```

Same pattern. Same separation. The *what* — check, price, charge, reserve, notify — is captured in the algebra. The *how* — async I/O, retry policies, error handling, logging — lives in the interpreter.

### Slide: "Interpreters Are Cheap"

| Interpreter | `TResult` | Purpose |
|---|---|---|
| `ProductionOrder` | `Task<OrderResult>` | Real services, async I/O |
| `TestOrder` | `OrderResult` | Pure, deterministic, no mocks |
| `NarrativeOrder` | `string` | Human-readable audit trail |
| `CostEstimator` | `decimal` | Predict cost before executing |
| `SafetyChecker` | `bool` | Verify no invalid states |

**One program. Five meanings. Zero mocks.**

**Transition**: "So what does this have to do with AI?"

---

## Section 5: What AI Actually Needs From You (7 min)

### Slide: "The Division of Labour"

```
┌─────────────────────────────────────────────┐
│  HUMAN: Define the algebra                   │
│                                              │
│  "What operations exist in my domain?"       │
│  "What does 'safe' mean?"                    │
│  "What are the failure modes?"               │
│  "What is the shape of my intent?"           │
│                                              │
│  This requires UNDERSTANDING.                │
│  Domain knowledge. Architectural judgment.   │
│  The thing you learn by learning to code     │
│  properly.                                   │
├─────────────────────────────────────────────┤
│  AI: Implement the interpreters              │
│                                              │
│  "Here's the algebra. Write a production     │
│   interpreter that uses async HTTP."         │
│  "Write a test interpreter that's pure."     │
│  "Write a logging interpreter that wraps     │
│   another interpreter."                      │
│                                              │
│  This is MECHANICAL.                         │
│  The algebra constrains the solution.        │
│  The compiler verifies exhaustiveness.       │
│  The type system catches mistakes.           │
└─────────────────────────────────────────────┘
```

### Speaker Notes

Here's the key insight. When people say "AI makes code quality irrelevant," they're imagining a world where humans and AI do the same job. But with principled structure, they do *different* jobs.

The human's job is **defining the algebra**. What operations exist? What composes with what? What does "safe" mean? This requires domain understanding, architectural judgment, and the kind of thinking you develop by learning to code properly. No amount of prompting gives you this. You have to *understand* the domain.

The AI's job is **implementing interpreters**. Given a well-defined algebra — a record of functions with typed signatures — generating an interpreter is highly constrained. The type system tells you exactly what each method must accept and return. The compiler tells you if you missed a case. This is exactly what AI is good at: generating code that satisfies a well-specified contract.

### Slide: "Karpathy's World vs. The Algebra World"

| | Karpathy's "Vibe Coding" | Algebra-First Coding |
|---|---|---|
| AI generates... | The whole thing | Interpreters from a contract |
| Correctness is... | Hoped for, tested after | Structurally guaranteed |
| Testing requires... | Mocks, mocks, mocks | Swap the interpreter |
| Adding a feature... | Regenerate & pray | Add to algebra, compiler finds all sites |
| Verification... | Manual review | Free from the structure |
| AI mistakes... | Silently ship to prod | Caught by type checker |
| When it works... | Prototypes, scripts, demos | Systems, domains, things that matter |

### Speaker Notes

Vibe-coding treats AI as a replacement for understanding. Algebra-coding treats AI as an *amplifier* of understanding.

When you vibe-code, every regeneration is independent. There's no accumulation of guarantees. When you algebra-code, the algebra IS the guarantee. Every interpreter the AI writes is checked against the contract. Add `EmergencyStop` to the algebra? The compiler won't let you build until every interpreter handles it. The AI can't forget a case even if it tries.

### Slide: "The Compiler as AI Supervisor"

```fsharp
// You add this to the algebra:
type FrogInterpreter<'a> = {
    ...
    EmergencyStop : string -> 'a   // NEW!
}

// The compiler IMMEDIATELY tells you:
//
// Error FS0764: No assignment given for field
//   'EmergencyStop' of type 'FrogInterpreter<string>'
//   (in storyTeller)
//
// Error FS0764: No assignment given for field
//   'EmergencyStop' of type 'FrogInterpreter<bool>'
//   (in safetyInspector)
//
// Error FS0764: No assignment given for field
//   'EmergencyStop' of type 'FrogInterpreter<FrogState -> FrogState>'
//   (in simulator)
```

Three errors. Three interpreters that need updating. The compiler enumerated them for you. Now hand each one to AI: *"Here's the algebra, here's the existing interpreter, add EmergencyStop."* The AI generates the implementation. The compiler verifies it. The human never wrote a line of interpreter code.

But the human *had to know to add EmergencyStop to the algebra*. That's the irreducible human contribution. That's what you learn by learning to code properly.

---

## Section 6: The Punchline (3 min)

### Slide: "The Irony"

> Jensen Huang says nobody needs to program. Karpathy says English is the programming language. Uncle Bob is having the time of his life with Claude.
>
> And the developers who took their advice — who stopped learning, who just vibed — are the ones getting the **worst** AI experience.
>
> The ones who learned to code properly? They're the ones AI amplifies most.

### Speaker Notes

The irony is profound. The developers who invest in principled structure — algebras, type safety, separation of intent from process — are the ones who get the *most* out of AI. Not because they write less code. Because they write the *right* code. The 15-line algebra that shapes everything downstream. The contract that constrains a thousand implementations. The structure that makes verification free.

The developers who "just vibe it" are the ones fighting AI. Regenerating. Debugging. Hoping. Because without structure, AI has nothing to be precise about.

### Slide: "What 'Learning to Code Properly' Means in 2026"

It doesn't mean memorizing syntax. It doesn't mean typing fast. It means:

1. **Knowing how to define an algebra** — the vocabulary of your domain
2. **Separating intent from process** — the *what* from the *how*
3. **Understanding composition** — how pieces fit together with guarantees
4. **Using types as documentation** — contracts the compiler enforces
5. **Designing for verification** — building systems where correctness is structural

These are the skills AI can't replace, because they're the skills that make AI *work*.

### Slide: "The Frog's Lesson 🐸"

> We started with a silly frog game. We ended up with verified elevator controllers,
> mock-free testing, free optimization passes, and a migration strategy for legacy codebases.
>
> All because someone learned to code properly — and defined an algebra.
>
> The AI didn't replace the understanding. It amplified it.
>
> Learn to code properly. Then let AI do the rest.

### Speaker Notes

Close with a callback to the frog. The whole tagless-final series started with a toy: jump, croak, eat a fly. But the *principle* — separate description from execution, define an algebra, let interpreters carry the weight — scales to elevator safety, e-commerce order flows, financial systems, and beyond.

The age of AI doesn't make this principle obsolete. It makes it essential. Because the developer who knows how to define the right algebra doesn't just write better code. They write the *architecture* that makes AI-generated code trustworthy.

That's not a skill AI replaces. That's the skill that makes AI worth using.

---

## Appendix: Key References for Slides

### Blog Series Referenced
- **Tagless Final in F#** (6 parts, FsAdvent 2025) — Frog DSL → Elevator verification → Code as Model
- **Your Clean Architecture Has a Dirty Secret** (6 parts, 2026) — Intent vs Process in C#, from diagnosis to migration

### Recommended Further Reading (for Q&A)
- Free Monads vs Tagless Final: "Two Sides of the Same Coin" (Post 4 of the Clean Architecture series)
- Practical migration: "The Strangler Fig" (Post 6)
- Category-theoretic foundations: "Standing on the Shoulders of Giants" (Post 5)
- Effects capstone (forthcoming): Free Monads, Tagless Final, and Algebraic Effects as three faces of the same idea

### Visual Suggestions
- **Section 2**: Side-by-side diff showing how adding retry policy rewrites the entire method body
- **Section 3**: Live demo or animation of running `adventure` through different interpreters
- **Section 4**: Mermaid diagram of the verification pipeline (elevator → frog → safety check)
- **Section 5**: Two-panel comic: "Vibe coder" regenerating endlessly vs. "Algebra coder" adding one line and getting five interpreters updated by AI
- **Section 6**: The frog wearing a hard hat, standing next to an elevator. Caption: "Same algebra."

---

## Potential Q&A Topics

**Q: "Isn't this overkill for most projects?"**
A: If you're building a CRUD app, maybe. But most systems have at least one domain where correctness matters. Start there. The Strangler Fig pattern (Post 6 of the Clean Architecture series) shows how to adopt this incrementally.

**Q: "Does AI really generate good interpreters from an algebra?"**
A: Remarkably well. The algebra constrains the solution space so tightly that the AI doesn't have room to hallucinate. Each method has a typed input and output. The compiler catches mistakes. It's the ideal AI task: well-specified, verifiable, mechanical.

**Q: "What languages support this?"**
A: F# is ideal (computation expressions make the DSL syntax beautiful). C# works well with interfaces and generics. Haskell and Scala have deep traditions here. But the *principle* — define an algebra, separate intent from process — works in any language with generics/templates and interfaces/traits.

**Q: "How is this different from just using interfaces and DI?"**
A: The key difference is that the *composition itself* is abstract. Standard DI abstracts individual operations but hardcodes how they sequence. An algebra abstracts everything — the operations AND the glue. That's what gives you swappable interpreters for the entire workflow, not just individual services.

**Q: "What about the 'programs as data' / Free Monad approach?"**
A: That's the dual encoding — Post 3 and 4 of the Clean Architecture series. Same separation, but the program becomes an inspectable data structure. You lose opacity but gain the ability to optimize, reorder, batch, and transform programs before executing them. The two approaches are mathematically dual — choose based on whether you need inspection.
