# Social Media: What Mitchell Wand Taught Me About Intent

**Post URL:** https://johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

---

## Email to Dr. Wand (ready to send)

**To:** Mitchell Wand
**Subject:** Re: Expressing Intent (and apologies for the delay!)

Dear Mitch,

Apologies for the slow reply — work has been a slog and this deserved more attention than I could give it until now.

Your April message identified exactly the right tension, and I wanted to give it a proper response rather than a hasty one.

**On host-language sequencing vs. nested algebra calls:**

The `=>` is doing real work. `price.Total` doesn't exist at program-construction time — it's the result of running an earlier step. The lambda captures a data dependency that can only be resolved at interpretation time. Nested algebra calls like `alg.ChargePayment(method, alg.CalculatePrice(items, coupon).Total)` don't work because `TResult` is abstract — you can't call `.Total` on it.

The honest answer is that `Then` is monadic bind — Kleisli composition — and the host language's `=>` is doing that plumbing. I don't think I can get away with less than monads: the continuations are genuinely data-dependent.

**What I actually need from the theory:**

1. A clean denotational account of the *algebra fragment* — the domain operations and their composition laws, treating `OrderStep<T>` as a functor on C# types-as-sets ($\mathcal{V}(\texttt{t})$, as you correctly reformulated). I believe your theory handles this cleanly, and I'd love to see the write-up.

2. An honest acknowledgement that `Then` is monadic bind and lives at the host-language level. The theory doesn't need to explain that part — I just need to stop pretending it's part of the algebra.

3. On `Guard`: boolean predicate + failure reason is all I need. Everything more complex belongs in the interpreter.

So the honest scope is: your theory covers the functor/algebra fragment precisely; `Then` lives above the algebra and requires monadic structure. That separation is actually a *feature* — the algebra captures the vocabulary of intent, and the host language provides the control flow plumbing.

I've written up a follow-up post exploring this distinction in more detail, connecting it to Kapil Viren Ahuja's three-layer schematic and Jonathan Bell's point about ADRs. It's here if you're interested:

https://johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

I'm genuinely excited to see your simpler-than-Yoneda explanation. Please do share it when you're ready — no rush at all.

Very sincerely,
John

---

## LinkedIn Options

### Option 1: The Humble Hook ✅ RECOMMENDED

A programming languages theorist read my blog series on intent-driven architecture and asked a question I couldn't answer:

"This uses the sequencing of the host language. One could argue that this formulation mixes 'intent' with 'implementation'."

He was right.

What I'd been calling "the algebra of intent" was actually two things jammed together: the domain vocabulary (what your system can do) and the monadic plumbing (how results flow between steps). They look the same in the code. They are not the same thing.

The vocabulary is algebraic — composable, analyzable, transformable. The sequencing is monadic — expressive but opaque. Collapsing them into one interface is pragmatically convenient and theoretically dishonest.

This matters beyond theory. When you're building intent-driven architectures — or instructing AI agents — the separation between "what operations exist" and "how results connect" determines what you can analyze, optimize, and trust.

New post: What Mitchell Wand Taught Me About Intent.

https://johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

#SoftwareArchitecture #FunctionalProgramming #CategoryTheory #ProgrammingLanguages

---

### Option 2: The Sharp One-Liner

I wrote a blog series about separating intent from process in software architecture. A PL theorist who literally wrote the book on continuations told me I'd mixed them back together.

He was right. Here's what I learned:

https://johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

#SoftwareArchitecture #FunctionalProgramming

---

### Option 3: The Industry Connection

Everyone's talking about "intent-driven development" — from Spec-Driven Development to AI agents. But there's a structural distinction nobody's making:

The *vocabulary* of intent (what operations exist) is algebraic — composable and analyzable.

The *sequencing* of intent (how results flow) is monadic — expressive but opaque.

Collapse them, and your "intent layer" becomes a leaky abstraction that looks clean but can't be analyzed or optimized.

A conversation with Mitchell Wand taught me this the hard way. New post:

https://johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

#SoftwareArchitecture #FunctionalProgramming #AI #VibeCoding

---

## Bluesky Options

### Option A: Conversational

A PL theorist read my blog series and pointed out that what I called "the algebra of intent" was actually two things: the domain vocabulary (algebraic, analyzable) and the sequencing plumbing (monadic, opaque). He was right.

johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

### Option B: The Punchline

@mitchwand.bsky.social read my blog series on intent-driven architecture and said "your `=>` is monadic bind, not domain vocabulary."

He was right. New post on what I learned.

johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

### Option C: The Thread (2 posts)

**Post 1:**
"This uses the sequencing of the host language. One could argue that this formulation mixes intent with implementation." — Mitchell Wand, reading my blog

When a PL theorist who shaped how we think about continuations says you mixed up your layers, you listen. 🧵

**Post 2:**
What I learned: every intent-driven architecture has two sub-layers. The algebra (what your system can do) and the monad (how results flow). They look identical in C#. They are mathematically different. And collapsing them is why "clean architecture" stays leaky.

johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html

---

## Tagging Strategy

**LinkedIn:**
- Tag Mitchell Wand if he has a profile (check first — academics often don't)
- Tag Kapil Viren Ahuja (referenced his Medium article)
- Don't tag Jonathan Bell unless you've confirmed he's comfortable being public
- Hashtags: #SoftwareArchitecture #FunctionalProgramming #CategoryTheory #ProgrammingLanguages

**Bluesky:**
- @mitchwand.bsky.social — tag him, he's on there
- @shriram.bsky.social may engage — this is exactly his domain
