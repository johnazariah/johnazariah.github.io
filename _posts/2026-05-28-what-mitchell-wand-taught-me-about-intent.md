---
    layout: post
    title: "What Mitchell Wand Taught Me About Intent"
    tags: [software-architecture, tagless-final, free-monad, functional-programming, category-theory]
    author: johnazariah
    summary: "A PL theorist read my blog series and asked a question I couldn't answer. What followed was a lesson in the difference between the algebra of your domain and the plumbing that connects it."
---

_This post is a follow-up to the [Intent vs Process](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html) series. You don't need to have read all six parts, but familiarity with [Part 2: The Algebra of Intent](/2026/03/05/02-the-algebra-of-intent.html) will help._

---

# What Mitchell Wand Taught Me About Intent

In March, I published a [six-part series](/tags/software-architecture/) about separating intent from process in C#. The thesis: if you make the vocabulary of your domain into an algebra and write programs against it, you can swap interpreters — production, test, narrative, dry-run — without changing the business logic. I called it "DI done properly" and connected it to the programming-languages concept of Tagless Final.

Then [Mitchell Wand](https://www.khoury.northeastern.edu/people/mitchell-wand/) started reading.

For those who don't know the name: Mitch is a professor at Northeastern University whose work on continuations, type systems, and denotational semantics has shaped how we think about programming languages. He's the kind of reader who doesn't just spot the typo in your category-theory section — he spots the *conceptual* imprecision that the typo was hiding.

He left a detailed Disqus comment, which became an email thread, which became a months-long conversation that changed how I understand my own code.

This post is about what I got wrong, what I learned, and why it matters for anyone building intent-driven architectures.

---

## The Question I Couldn't Answer

In [Post 2](/2026/03/05/02-the-algebra-of-intent.html), the `PlaceOrder` function chains operations together using `Then`:

```csharp
price => alg.Then<ChargeResult>(
    alg.ChargePayment(request.PaymentMethod, price.Total),
    charge => ...
)
```

Mitch's question was deceptively simple:

> *"This uses the sequencing of the host language, as evidenced by the `=>`, rather than having an abstract sequencing operator. One could argue that this formulation mixes 'intent' with 'implementation'."*

He then offered an alternative: nest the algebra calls directly.

> *"You could instead use nested algebra calls, like `alg.ChargeResult(...the algebra term that produces the price...)`. Admittedly, this mixes intent with implementation even more intimately. On the other hand, the theorems become much more straightforward."*

And then the sharp part:

> *"The theory I have (worked out with the help of Claude) handles nested algebra calls perfectly. As near as I can tell, the theory (including the Yoneda lemma story) no longer applies in a straightforward manner to the host-language sequencing that you've used."*

I sat with this for weeks. And he's right.

---

## The Two Fragments

What Mitch had identified — with the precision of someone who's been doing this for forty years — is that my code is actually mixing two fundamentally different things:

**The algebra fragment.** The domain operations — `CheckStock`, `CalculatePrice`, `ChargePayment` — are pure vocabulary. They describe *what* your system can do. They form a clean algebraic structure: a functor on the category of C# types (objects as value-sets $\mathcal{V}(\texttt{t})$, arrows as C#-definable functions). Mitch's theory handles this fragment cleanly.

**The plumbing fragment.** The `Then` operation — and the `=>` lambda that accompanies it — is not part of the domain algebra. It's *monadic bind*. It's Kleisli composition. It's the host language's way of saying "take the result of this step and feed it into the next one." And it's there because the steps are *data-dependent*: `price.Total` doesn't exist until the interpreter actually runs `CalculatePrice`.

This distinction matters because it's not just a theoretical nicety — it's the same distinction that determines what you can and can't optimize.

---

## Why the Lambda Is Doing Real Work

Consider the alternative Mitch suggested — nested algebra calls:

```csharp
alg.ChargePayment(method, alg.CalculatePrice(items, coupon).Total)
```

This doesn't work. `alg.CalculatePrice(items, coupon)` returns a `TResult`, not a `PriceResult`. The whole point of the algebra is that `TResult` is abstract — it might be `Task<PriceResult>`, it might be `string`, it might be `List<AuditEntry>`. You can't call `.Total` on it because you don't know what it is.

The lambda exists precisely because the program is a *description*, not an *execution*. The continuation `price => ...` says: "when the interpreter eventually produces a `PriceResult` from this step, here's what to do with it." That's a data dependency that can only be resolved at interpretation time. That's monadic bind.

In Haskell, this is transparent — `do`-notation is syntactic sugar for `>>=` (bind). In C#, LINQ's `from ... select ...` is sugar for `SelectMany` (bind). In my algebra, `Then` is bind wearing a business-casual name.

---

## What This Means for Intent-Driven Architecture

Here's the punchline, and it connects directly to a debate happening in the industry right now.

In May, Mitch forwarded a [Medium article by Kapil Viren Ahuja](https://medium.com/activated-thinker/spec-driven-development-isnt-broken-it-will-collapse-c00609f72496) arguing that both spec-driven development and vibe coding fail for the same reason: they jam three layers — intent, specification, and implementation — into one document. Ahuja's "three-layer schematic" separates them:

- **Intent** — what the user wants, with constraints and failure conditions
- **Spec** — the evaluable contract (can you write a test for it?)
- **Implementation** — architectural decisions that belong to the system

This is exactly the separation my series advocates at the code level. But Mitch's critique reveals something Ahuja's framework doesn't address: **even within the intent layer, there are two sub-layers.** The *vocabulary* of intent (what operations exist) and the *sequencing* of intent (how results flow between them) are structurally different things, governed by different mathematics, with different implications for what you can analyze and optimize.

The vocabulary is algebraic — it's your domain's effect functor. You can reason about it, compose it, transform it. Mitch's theory handles it.

The sequencing is monadic — it's Kleisli composition. It gives you expressiveness (data-dependent steps) at the cost of analyzability (you can't see the full program structure until you run it). This is the fundamental trade-off explored in [Post 3](/2026/03/05/03-intent-you-can-see-and-optimize.html), and it's the reason the Free Monad representation exists: it makes the sequencing *visible as data* so you can optimize the happy path even though you can't see the full branching tree.

Jonathan Bell, a colleague of Mitch's at Northeastern, added another thread to this: ADRs (Architecture Decision Records) as a way to capture intent that survives across time. Ahuja's article identifies the same gap — SDD solves for structure at the moment of creation but has no answer for continuity. ADRs are the durable record of *why* decisions were made, which is intent in its purest form.

---

## The Honest Acknowledgement

So here's what I should have said in [Post 2](/2026/03/05/02-the-algebra-of-intent.html), and what I'm saying now:

The `IOrderAlgebra<TResult>` interface conflates two things. The domain operations (`CheckStock`, `ChargePayment`, etc.) are the algebra of intent — the vocabulary of your domain. `Then` and `Guard` are not part of that algebra. `Then` is monadic bind — the sequencing plumbing that the host language provides. `Guard` is a conditional effect — a boolean predicate with a failure reason.

The algebra captures what your system *can do*. The monad captures how results *flow between* operations. Both are necessary. But they're different things, governed by different laws, and collapsing them into one interface — while pragmatically convenient in C# — obscures the separation that makes the whole approach trustworthy.

I updated [Post 4](/2026/03/05/04-two-sides-of-the-same-coin.html) with a more careful treatment of the category-theoretic foundations after Mitch's initial feedback. This post is the acknowledgement that the deeper issue — applicative vs monadic, algebra vs plumbing — deserves to be said out loud.

---

## What's Coming

Mitch has a simpler-than-Yoneda explanation for the algebra fragment that he's writing up. When it arrives, I'll update the series with proper attribution. In the meantime, if you're building intent-driven architectures — whether in code, in methodology, or in the way you instruct AI agents — the separation to keep in your head is:

1. **What can the system do?** — the algebra (domain operations, composable, analyzable)
2. **How do results flow?** — the monad (sequencing, data-dependent, expressive but opaque)
3. **What are the failure conditions?** — the effect layer (guards, compensations, handled by the interpreter)

Get those three right, and the intent layer stops being a buzzword and starts being an engineering discipline.

---

*Thanks to [Mitchell Wand](https://www.khoury.northeastern.edu/people/mitchell-wand/) for the detailed feedback that prompted this post, and for the ongoing correspondence that continues to sharpen these ideas. Thanks also to Jonathan Bell for the ADR connection, and to Kapil Viren Ahuja whose [article](https://medium.com/activated-thinker/spec-driven-development-isnt-broken-it-will-collapse-c00609f72496) on the three-layer schematic arrived at the same separation from a completely different direction.*

---

> **Series context**: This is a companion to the [Intent vs Process](/tags/software-architecture/) series. Start with [Part 1: Your Clean Architecture Has a Dirty Secret](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html) if you're new here.
