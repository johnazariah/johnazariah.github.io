# Correspondence: Mitchell Wand — Expressing Intent

**Date:** 2026-05-28
**To:** Mitchell Wand
**Re:** Expressing Intent — reply to April 16 message
**Thread context:** Feedback on the [Intent vs Process series](/2026/03/05/01-your-clean-architecture-has-a-dirty-secret.html), specifically Post 2 ([The Algebra of Intent](/2026/03/05/02-the-algebra-of-intent.html))

---

## Background

Mitch left a detailed Disqus comment on the series in mid-March 2026, which opened an email thread. His April 16 message identified a key theoretical gap and asked for clarification on what I actually need from the theory before he writes up his solution (which he says is simpler than Yoneda).

> ⚠️ **Caveat:** The summaries below are my (John's) somewhat amateurish understanding of what Mitch said. I may have missed nuance or stated things imprecisely. The quoted passages are verbatim from his emails; the interpretations are mine.

---

### Thread 1 — March (Disqus → Email)

Mitch's initial detailed comment raised two precise points about Post 2:

**On the "category of C# types":**

> *"Your formulation — objects as the sets `[t]` of values of each C# type `t`, arrows as the set-theoretic functions from `[t₁]` to `[t₂]` definable in C# — is much cleaner, and I should have been more careful here."*

My reading: the informal framing in the post conflates types with their inhabitants, which makes subsequent definitions harder to trust. Mitch's cleaner formulation separates them properly.

**On `OrderStep<T>` as a functor:**

> *"If S is a set, what is `OrderStep<T>(S)`? It must be a set, but what set is it? And if `f:S->S'` is a function from set S to set S', what is `OrderStep<T>(f)`? It must be a function from `OrderStep<T>(S)` to `OrderStep<T>(S')`. But what function is it? And how do we know that the functions `OrderStep<T>(f)` for all the functions f obey the composition law? Oh yes, and what is that pesky `<T>`?"*

My reading: I asserted `OrderStep<T>` is a functor but never showed what it maps sets and functions *to*, or that composition is preserved. The `<T>` question is the sharpest part — is `OrderStep` a functor in `T`, in `S`, or something more complex?

---

### Thread 2 — April 16 (the message this reply addresses)

After working through the theory, Mitch identified the deeper issue around control flow:

> *"You write things like: `price => alg.Then<ChargeResult>(...)`. This uses the sequencing of the host language, as evidenced by the `=>`, rather than having an abstract sequencing operator. One could argue that this formulation mixes 'intent' with 'implementation'."*

> *"You could instead use nested algebra calls, like `alg.ChargeResult(...the algebra term that produces the price...) ?` Admittedly, this mixes intent with implementation even more intimately. On the other hand, the theorems become much more straightforward. Why did you reject this? The theory handles it perfectly."*

> *"Another alternative would be to use some abstract sequencing formalism, like Haskell's arrows. You might not need the full power of arrows or monads. Maybe something simpler will do."*

> *"The theory I have (worked out with the help of Claude) handles nested algebra calls perfectly. As near as I can tell, the theory (including the Yoneda lemma story) no longer applies in a straightforward manner to the host-language sequencing that you've used or to any of the alternatives I've suggested above."*

> *"I'm happy to explore how the theory might be extended to cover your use cases, but I need to hear from you about what directions you would find fruitful."*

My reading: Mitch has a clean theory for the *applicative* fragment (steps that don't depend on earlier results). My code uses the *monadic* fragment (steps that do — `price.Total` comes from the previous step). The host-language `=>` is doing Kleisli composition, which sits above the algebra. His theory doesn't cover that fragment cleanly yet, and he wants to know whether it's worth extending it, or whether I'd be satisfied with the cleaner applicative account.

His two open questions for this reply:

1. Why did I use host-language sequencing (`=>`) rather than nested algebra calls?
2. What exactly do I need from the theory for my proposed application?

---

## Draft Reply

**Subject:** Re: Expressing Intent (and apologies for the delay!)

Dear Mitch,

Apologies for the slow reply — work has been a slog and this deserved more attention than I could give it until now.

---

### On the category of C# types

In your March message you pointed out that my informal framing conflated types with their inhabitants, and offered the cleaner formulation:

> *"Objects as the sets `[t]` of values of each C# type `t`, arrows as the set-theoretic functions from `[t₁]` to `[t₂]` definable in C#."*

You're absolutely right, and I appreciate you pressing on it. The informal framing made the subsequent definitions harder to trust. I'll update the post to use your formulation explicitly.

---

### On `OrderStep<T>` as a functor

You identified the gap precisely:

> *"If S is a set, what is `OrderStep<T>(S)`? It must be a set, but what set is it? And if `f:S->S'` is a function from set S to set S', what is `OrderStep<T>(f)`? It must be a function from `OrderStep<T>(S)` to `OrderStep<T>(S')`. But what function is it? And how do we know that the functions `OrderStep<T>(f)` for all the functions f obey the composition law? Oh yes, and what is that pesky `<T>`?"*

This is a real gap — I asserted the functor claim without showing what it maps sets and functions *to*, or that composition is preserved. I'll address this in the post. The intent is that `OrderStep` is an endofunctor on C# types-as-sets, mapping a type `T` to the set of order step descriptions that produce a `T`, with `Select` mapping over the result type. But you're right that this needs to be made explicit, with the composition law verified.

---

### On host-language sequencing vs. nested algebra calls

This is the heart of your April message, and you've identified exactly the right tension:

> *"You write things like: `price => alg.Then<ChargeResult>(...)`. This uses the sequencing of the host language, as evidenced by the `=>`, rather than having an abstract sequencing operator. One could argue that this formulation mixes 'intent' with 'implementation'."*

> *"You could instead use nested algebra calls, like `alg.ChargeResult(...the algebra term that produces the price...) ?` Admittedly, this mixes intent with implementation even more intimately. On the other hand, the theorems become much more straightforward. Why did you reject this? The theory handles it perfectly."*

The `=>` is doing real work — it's not stylistic. It's there because `price.Total` is only available at runtime, when the interpreter actually executes `CalculatePrice`. The program is a *description* of what to do, and the continuation captures a data dependency that doesn't exist until interpretation time. That's why nested algebra calls like

```csharp
alg.ChargePayment(method, alg.CalculatePrice(items, coupon).Total)
```

don't work — they'd require the argument to be fully resolved before the program is constructed, which conflates description with execution. In the specific example of `PlaceOrder`, `price.Total` isn't a value I have at program-construction time; it's the *result* of running an earlier step.

> *"Another alternative would be to use some abstract sequencing formalism, like Haskell's arrows. You might not need the full power of arrows or monads. Maybe something simpler will do."*

I think the honest answer is that `Then` is monadic bind — Kleisli composition — and the host language's `=>` is doing that plumbing. The question of whether I need the full power of monads or something weaker is a good one, but I don't think I can get away with less: the continuations are genuinely data-dependent (each step's result feeds into the next step's arguments).

---

### What I actually need from the theory

> *"The theory I have (worked out with the help of Claude) handles nested algebra calls perfectly. As near as I can tell, the theory (including the Yoneda lemma story) no longer applies in a straightforward manner to the host-language sequencing that you've used or to any of the alternatives I've suggested above."*

> *"I'm happy to explore how the theory might be extended to cover your use cases, but I need to hear from you about what directions you would find fruitful."*

To answer this directly — what I actually need from the theory is this:

1. A clean denotational account of the *algebra fragment* — the operations (`CheckStock`, `ChargePayment`, etc.) and their composition laws, treating `OrderStep<T>` as a functor (endofunctor on C# types-as-sets, as you correctly reformulated). This is the fragment I believe your theory handles cleanly, and I'd love to see your write-up of it.

2. An honest acknowledgement in the post that `Then` is monadic bind — Kleisli composition — and that the host language's function abstraction (`=>`) is doing the plumbing there. The theory doesn't need to explain that part; I just need to stop *pretending* it's part of the algebra when it isn't.

3. On `Guard`: yes, boolean predicate + failure reason is all I need. Everything more complex (retry logic, compensating transactions) belongs in the interpreter. I don't need anything beyond that in the algebra.

So the honest scope is: your theory covers the functor/algebra fragment precisely; I'll explicitly call out that `Then` lives at the host-language level and requires monadic structure that sits above the algebra. That separation is actually a *feature* of the approach — the algebra captures the vocabulary of intent, and the host language provides the control flow plumbing.

---

I'm genuinely excited to see your simpler-than-Yoneda explanation. Please do share it when you're back from vacation — and no rush at all.

And yes — the QAOA post is coming back, expanded. The preprint is out and the full series is almost ready to publish. I'll make sure you see it when it drops.

Very sincerely,
John

---

## Notes

- The Thread 1 fixes (category definition, `OrderStep<T>` functor, Mitch acknowledgement) were **already applied** in [Post 4: Two Sides of the Same Coin](../_posts/2026-03-05-04-two-sides-of-the-same-coin.md) — using $\mathcal{V}(\texttt{t})$ formulation, explicit functor treatment via continuation pairing, and a thanks line crediting Mitch.
- The Thread 2 material (`Then` as monadic bind, applicative vs monadic distinction, connection to Ahuja/Bell) is addressed in the follow-up draft: [What Mitchell Wand Taught Me About Intent](../_drafts/2026-05-28-what-mitchell-wand-taught-me-about-intent.md).
- A footnote was added to the end of [Post 2](../_posts/2026-03-05-02-the-algebra-of-intent.md) pointing readers to the follow-up and to Post 4.
- The "errors first" thread Mitch raised (senior devs starting with failure modes) is left open intentionally — it's a potential future post direction.
- The QAOA reference points to the `saturday-to-coauthor` series in `future-series/` — pending Stephen's sign-off.
