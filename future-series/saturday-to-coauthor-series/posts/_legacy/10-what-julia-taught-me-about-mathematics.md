# What Julia Taught Me About Mathematics

_Part 10 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — looking back at ten algorithmic innovations over eight weeks, and the case that programming languages aren't just implementation tools. They're thinking tools. And the one you think in shapes what you find._

---

This is the last post in the series. I want to step back from the code and the timings and the comparison tables, and talk about what I actually learned.

Not about QAOA. Not about Julia. About the relationship between the two — and what it says about how computational science should work.

---

## The Ten Innovations

Here they are, in the order they emerged:

| # | Innovation | What changed | Came from |
|---|-----------|-------------|-----------|
| 1 | WHT factorisation | $O(4^{3p}) \to O(p^2 \cdot 4^p)$ | Refactoring the fold |
| 2 | Manual Basso adjoint | Gradient in $1.6\times$ forward | Deriving the backward pass |
| 3 | Cost algebra / tagless final | MaxCut + XORSAT in one engine | Separating what from how |
| 4 | Plateau detection | 2 hr $\to$ 40 min at $p = 12$ | Watching the optimizer waste time |
| 5 | Threshold normalisation | Overflow-free at $(7, 8)$ | Debugging impossible values |
| 6 | Swarm/memetic optimizer | All 15 pairs valid | Remembering Helmut's BRKGA lessons |
| 7 | Multi-machine orchestration | $p = 13$ on cluster | Needing more RAM than one machine has |
| 8 | Double64 precision | Cancellation-free at $k \geq 6$ | One type parameter, one line |
| 9 | Charge decomposition | $O(p^2 \cdot 4^p) \to O(p \cdot 4^p)$ | Studying the QOKit team's code |
| 10 | Charge manual adjoint | Gradient in $\sim 5\times$ forward | Translating C++ to Julia |

Ten innovations in eight weeks. Each one built on the infrastructure the previous ones created. The cost algebra (3) enabled the cross-validation that caught the normalisation bugs (5). The type parameter that enabled ForwardDiff (2) also enabled Double64 (8). The test harness that validated the Basso adjoint (2) was the same harness that validated the charge adjoint (10).

This isn't accidental. This is what happens when your abstractions are right.

## What Came From Code Clarity

Innovations 1, 3, and 5 came directly from having code that was clean enough to read.

The WHT factorisation (1) emerged from separating the constraint kernel from the fold. The cost algebra (3) emerged from wanting to avoid copy-pasting the evaluator for MaxCut vs XORSAT. The threshold normalisation (5) emerged from staring at the forward pass and seeing where the magnitudes exploded.

None of these were planned. None of them were in the Basso paper. They were consequences of writing code in a language where abstractions are cheap and readable, and then _reading what you wrote_.

Julia didn't discover the WHT factorisation. I did. But Julia made the kernel a standalone function, made the fold a clean iteration, and made the convolution structure visible. In a language where the fold was buried in a nested loop with inlined array operations, I doubt I would have seen it.

## What Came From Julia's Type System

Innovations 2, 8, and 10 came from Julia's parametric types and multiple dispatch.

ForwardDiff (2) worked because `QAOAAngles{T}` accepts `Dual{Float64}` without code changes. Double64 (8) worked because the same type parameter accepts `Double64` without code changes. The charge adjoint (10) works through the same API as the Basso adjoint because multiple dispatch routes to the right implementation.

These aren't features you appreciate until you need them at 2 AM with a deadline. The moment you realise "I can test this with a completely different numerical substrate by changing one type parameter" is the moment Julia's design philosophy pays off.

In Python, each of these would have required a framework (JAX, PyTorch) or a rewrite. In C++, each would have required templates with explicit specialisation. In Julia, each was a one-line change that composed with everything else.

## What Came From Other People

Innovations 6, 9, and 10 came from other people's work.

The swarm optimizer (6) came from Helmut Katzgraber's teaching, five years earlier, about population-based search in rugged landscapes. The charge decomposition (9) came from the QOKit team's published code. The charge adjoint (10) came from their C++ implementation.

I want to be clear about this because the narrative of the lone genius — the programmer who sits down and derives everything from first principles — is a myth. Every project stands on other people's work. The question is whether you acknowledge it.

## What Came From Engineering

Innovations 4, 5, 7, and the diagnostics module came from pure engineering — not mathematical insight, not language features, just the craft of making software work in production.

Plateau detection (4): four iterations to get convergence right. Multi-machine orchestration (7): git branches and CSV files. The diagnostics module: born from an overnight crash that could have been caught if we'd built monitoring into the code instead of the script.

These aren't the innovations that make the paper. They're the innovations that make the paper _possible_. Without plateau detection, $p = 12$ takes 2 hours instead of 40 minutes. Without multi-machine orchestration, $p = 13$ doesn't happen. Without diagnostics, the $p = 14$ overnight run dies silently and you don't know why.

Engineering is the connective tissue of computational science. It doesn't get published, it doesn't get cited, but without it, nothing works.

## The Case for Methods-as-Publication

This series exists because methods papers are hard to publish.

Journals want novel results — new theorems, new algorithms, new bounds. They don't want "we implemented the Basso recurrence in Julia and here are ten engineering decisions we made." The implementation craft that makes computational science reproducible is systematically undervalued by the publication system.

But it's exactly what future researchers need. When someone wants to implement the charge decomposition for a new problem, they need to know about C-order vs F-order reshape bugs. When someone wants to push to $p = 15$, they need to know about threshold normalisation and plateau detection. When someone wants to reproduce our results, they need to know that 1,875 tests validate the pipeline against four independent gradient methods.

This blog series is my attempt to fill that gap. It's not peer-reviewed in the traditional sense. But the code is public, the tests are comprehensive, and the DOI is permanent. The reproducibility infrastructure _is_ the review.

## What Julia Taught Me

Here's what I actually learned, distilled to three sentences:

**The language you think in shapes the theorems you find.** Julia's composable abstractions made the WHT factorisation visible. A less expressive language would have hidden it.

**The language you write in shapes the experiments you run.** Julia's type system made the three-gradient comparison, the Double64 fix, and the ForwardDiff experiment trivially cheap. When experiments are cheap, you run them. When you run them, you learn things you wouldn't have predicted.

**Engineering is research.** The ten innovations in this series aren't cleanly divided into "science" and "engineering." The WHT factorisation is mathematics. The threshold normalisation is engineering. The cost algebra is software architecture. The charge adjoint is all three at once. The boundaries don't exist. They never did.

---

## The Numbers

Since this is the last post, here's the table that tells the whole story — the computation time for MaxCut ($k = 2, D = 3$) optimisation at each depth, across the three generations of the evaluator:

| $p$ | Basso + FD | Charge + FD | Charge + Adjoint | Speedup (total) |
|-----|-----------|------------|-----------------|----------------|
| 5 | 260 ms | — | — | — |
| 8 | 88 s | 18.7s | **3.1s** | 28× |
| 9 | — | 111.9s | **15.6s** | — |
| 10 | 11 min | 772s | **74.8s** | 9× |
| 11 | — | 4,331s (72 min) | **373s (6 min)** | 12× |
| 12 | 40 min | ~5.5 hr | **1,646s (27 min)** | 12× |
| 13 | 84 hr (cluster) | ~25 hr (est) | **6,588s (110 min)** | 14× |
| 14 | not feasible | ~100 hr (est) | **$\tilde{c} \geq 0.8896$, running on Xeon** | — |

Look at that rightmost column. p=11 in six minutes. p=12 in twenty-seven minutes. p=13 in under two hours. On a Mac Studio. No cluster. No GPU. Just the charge evaluator, the manual adjoint, and a fold engine that doesn't know what problem it's solving.

Each row in this table is a depth that was once a wall. $p = 8$ was the limit of the naive evaluator. $p = 11$ was where ForwardDiff hit its cost ceiling. $p = 13$ needed Stephen's 50-node cluster with the old code. Now it's a lunch break.

That's not one speedup. That's three speedups stacked — WHT factorisation ($10^{11}\times$ at $p = 11$), charge decomposition ($12\times$ at $p = 12$), and the manual adjoint ($12\times$ again) — each one unlocking the depth range that the next one optimised. The fold is the same fold. The mathematics just got faster around it.

And somewhere, right now, the fold engine is grinding through $p = 14$. It doesn't know how it got here. It just folds.

---

_This is the final post in the "From Saturday to Co-Author" series. The full series:_

> _1. [The Fold That Changed Everything](/tags/from-saturday-to-coauthor/)_
> _2. [Three Gradients and a Type Parameter](/tags/from-saturday-to-coauthor/)_
> _3. [The Walls](/tags/from-saturday-to-coauthor/)_
> _4. [1,875 Reasons to Sleep at Night](/tags/from-saturday-to-coauthor/)_
> _5. [The Algebra That Runs Itself](/tags/from-saturday-to-coauthor/)_
> _6. [Learning From the Masters](/tags/from-saturday-to-coauthor/)_
> _7. [The Manual Adjoint, Manually](/tags/from-saturday-to-coauthor/)_
> _8. [p=14 (and What It Means)](/tags/from-saturday-to-coauthor/)_
> _9. [The Pair Programmer That Never Sleeps](/tags/from-saturday-to-coauthor/)_
> _10. What Julia Taught Me About Mathematics_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat) (DOI: [10.5281/zenodo.19211958](https://doi.org/10.5281/zenodo.19211958)). 1,875 tests passing. Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._

_Thank you to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao) for the problem and the collaboration. Thank you to the QOKit team for the charge decomposition. Thank you to [Helmut Katzgraber](https://scholar.google.com/citations?user=s1PfsM8AAAAJ&hl=en&oi=ao) for teaching me about swarms five years before I needed them. And thank you to Julia, for being a language clear enough that the mathematics had nowhere to hide._

_Bon appétit._
