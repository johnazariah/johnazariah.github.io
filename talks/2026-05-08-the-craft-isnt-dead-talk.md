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

This is the real payoff. The same verification infrastructure is reusable across domains. The code remains high-level. The guarantees are structural. What used to be domain-specific ceremony becomes a standard algebraic pipeline.

This is the moment you can tell the audience: the craft isn't dead. It's just becoming more formal.

---

## Section 5: What AI Actually Needs From You (7 min)

### Slide: "The AI Bottleneck"

The AI isn't missing capability. It's missing **ground truth**.

It can generate code. It can rewrite code. It can refactor code. But it cannot know which invariants matter unless you encode them.

A well-structured program gives it:

- a vocabulary of concepts
- a small set of operations
- a domain contract
- a place to attach invariants
- an interpreter model to preserve behavior

### Speaker Notes

This matters because the large models are not 