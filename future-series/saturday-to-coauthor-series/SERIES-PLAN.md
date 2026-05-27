# From Saturday to Co-Author — Blog Series Plan

## Series Concept

A multi-part series telling the full story of how a functional programmer
went from "what is QAOA?" on a Saturday morning to co-author on a Google
Quantum AI paper eight weeks later — by writing clean Julia code that
kept revealing hidden mathematical structure.

Methods papers are notoriously hard to publish: journals want novelty in
results, not implementation craft. But the engineering that makes computational
science reproducible — the debugging stories, the wrong turns, the role of
language design in enabling mathematical discovery — is exactly what future
researchers need most. This series is that record. Every algorithmic decision,
every wall we hit, every insight we borrowed and adapted — documented in enough
detail that someone could reconstruct the entire pipeline from these posts alone.

## Target Audience

Programmers who care about mathematical structure in code. Functional
programmers curious about scientific computing. Physicists who want to see
how software engineering can drive research. Anyone who's been told "just
use Python."

## Posts

### 01 — The Fold That Changed Everything
**Source material**: Current draft Acts 1-2 (refactored from monolith)
**Story**: Saturday morning: no idea what QAOA is. Wednesday: p=11 results.
How separating the constraint kernel from the fold revealed the WHT
factorisation. The thesis: clean code reveals hidden structure.
**Key content**: catamorphism / cost algebra pattern, XOR convolution
insight, O(p²·4^p) from O(4^{3p}), 65,000× speedup at k=3 p=8.

### 02 — Three Gradients and a Type Parameter
**Source material**: Current draft Act 3 (AD comparison)
**Story**: Why you need gradients, and how Julia's type system made it
trivial to test three completely different gradient strategies — finite
differences, ForwardDiff, and hand-derived adjoint — in one codebase.
**Key content**: parametric types + dual numbers, why FD fails at p≥4,
manual adjoint derivation (WHT is self-adjoint, log-derivative trick),
the 1.6× cost result, head-to-head comparison table.

### 03 — The Walls
**Source material**: Current draft "What Happened Next" + Entries 20-26
**Story**: Four walls that almost stopped us — overflow, landscape
flatness, scale, and the GPU dead end. How each wall was diagnosed,
and how knowing when to abandon an approach (Metal.jl) is as important
as knowing when to push through.
**Key content**: threshold normalisation (not always-normalise — and
why always-normalise was worse than no normalisation), memetic/swarm
optimizer (Helmut Katzgraber connection), Metal.jl spike (Float64
hard-rejected, Float32 precision degrades, knowing when to stop),
plateau detection (four iterations to get convergence right: iteration
chunks → depth-dependent → wall-time → circular buffer with per-
iteration check), multi-machine orchestration across Mac/Azure/P710/
Google, Double64 for catastrophic cancellation at k≥6.

### 04 — 1,875 Reasons to Sleep at Night
**Source material**: NEW — the testing and validation architecture
**Story**: How do you know your exact evaluator is exact? When you're
computing numbers nobody has computed before, there's no reference to
check against — except the ones you build yourself. This post walks
through the multi-layered testing architecture that gave us confidence
to publish numbers in a Google Quantum AI paper.
**Key content**:
- **Layer 1: Structural tests** — tree construction, tensor dimensions,
  hyperindex bit positions. The boring stuff that prevents the
  catastrophic stuff.
- **Layer 2: Known-value tests** — MaxCut (k=2, D=3) at p=1 reproduces
  Farhi 2014's c̃ ≈ 0.6924 to 10 digits. This is our anchor.
- **Layer 3: Cross-evaluator congruence** — Basso evaluator, charge
  evaluator, reduced-basis evaluator, GPU evaluator: four independent
  implementations that must agree to 1e-10 at every (k, D, p). If they
  don't, at least one is wrong.
- **Layer 4: Gradient congruence** — ForwardDiff (exact, but slow),
  central FD (approximate), Basso adjoint (hand-derived), charge adjoint
  (hand-derived from different mathematics): four gradient methods that
  must agree. The charge adjoint was validated against all three others.
- **Layer 5: Optimizer congruence** — L-BFGS with different autodiff
  backends must converge to the same c̃. If :adjoint and :charge_adjoint
  give different optima, the gradients have a bug.
- **Layer 6: Physical bounds** — c̃ ∈ [0,1] always. Gradient finite
  always. Value at zero angles = 0.5 always. These catch overflow,
  cancellation, and NaN propagation.
- **Layer 7: Monotonicity** — c̃(p) ≥ c̃(p-1) for MaxCut. If depth
  p+1 is worse, the optimizer found a bad basin, not a bug — but it
  still needs investigation.
22 test files. 1,875 individual tests. Every evaluator, every gradient
method, every autodiff mode, every (k,D) family cross-checked against
every other. This is what "engineering excellence" looks like in
computational science.
**Why this matters**: The paper's results were trusted because every
number was independently verified through multiple code paths. No
single bug could produce a plausible-looking wrong answer without
being caught by at least two other test layers.

### 05 — The Algebra That Runs Itself
**Source material**: Current draft Act 1 (cost algebra) + tagless final
connection
**Story**: The CostAlgebra pattern — MaxCut and Max-k-XORSAT as
different instantiations of the same fold — is a tagless final
encoding in disguise. This post connects the QAOA evaluator's
architecture to the programming languages concept of tagless final
interpreters, and shows how the same pattern enables four capabilities
that would otherwise require four separate codebases:
1. **Evaluation** — compute c̃ for any (k, D, p)
2. **Differentiation** — gradients via the same fold, different algebra
3. **Validation** — cross-check by interpreting the same program with
   a different evaluator
4. **Extension** — add a new problem (MaxCut → XORSAT) by swapping
   the algebra, not the engine
**Key content**: the CostAlgebra trait (constraint_kernel,
root_observable_kernel, expectation_from_parity), how multiple
dispatch makes this zero-cost in Julia, comparison with the tagless
final pattern from the [earlier blog series](/2025/12/12/tagless-final-01-froggy-tree-house.html),
why this matters for reproducibility (the engine is problem-agnostic,
so validating it on MaxCut validates it for XORSAT too).
**Connection to tagless final**: In the tagless final encoding, a
"program" is a function parameterised by an interpreter. In our
evaluator, the "program" is the light-cone tree structure, and the
"interpreter" is the CostAlgebra. Same idea, different language,
real-world scientific impact.

### 06 — Learning From the Masters
**Source material**: NEW — the charge decomposition work (May 5-12)
**Story**: How studying the QOKit team's open-source
implementation taught us a deeper mathematical structure — the Z₂×Z₂
charge decomposition — that we'd missed in our own derivation. A case
study in why reading expert implementations is a research skill, not
just a coding skill. Their insight — decomposing the doubled density
matrix into four independent charge channels — is elegant and
non-obvious. We learned it by reading their code, understanding the
mathematics behind it, and reimplementing it from scratch in Julia.
The translation was far from mechanical: every bug was invisible at
p=1, caught only by systematic testing at p≥2.
**Attribution rule**: see FACTS.md "Joint-paper collaborators". The
QOKit toolkit is from JPMorganChase Applied Research; the JPMC
co-authors on John's joint paper are Abid Khan, Ruslan Shaydulin, and
Sami Boulebnane. Do NOT use "Marwaha, Wurtz, Lykov" — that was a
planning-doc mistake corrected on 27 May 2026.
**Key content**: charge channels, mode products, the Z₂×Z₂ character
table, C-order vs F-order, γ/2 convention, transpose vs adjoint.
O(p·4^p) from O(p²·4^p). Why "reading someone else's code" is an
underrated research method. Credit to the QOKit team for the
mathematical insight that made this possible.
**Tone**: Collegial, grateful. These are seasoned experts whose
published, open-source work enabled ours. We learned from them the way
you learn from a masterclass — by studying technique, understanding
the principles, and then practising until you can do it yourself.

### 07 — The Manual Adjoint, Manually
**Source material**: NEW — the charge adjoint work (May 10-12)
**Story**: The QOKit team also published a C++ adjoint for the charge
evaluator. Again, we studied it, understood the mathematics, and built
our own Julia implementation from scratch. Three bugs, all invisible
at p=1: Wirtinger conjugation, coefficient chain, C-order V_flat.
How writing tests first (76 tests!) found every bug. The instrumented
forward that eliminated redundant replay.
**Key content**: backward through mode products, WHT butterfly adjoint,
Phase 2 recursive trace backward, Wirtinger calculus for real-loss-of-
complex-variables, the 57×→4.5× speedup, comparison with the QOKit
team's 3×. What the remaining performance gap teaches us about
allocation patterns in Julia vs C++.

### 08 — p=14 (and What It Means)
**Source material**: NEW — the p=14 run (May 12-13)
**Story**: The first exact p=14 MaxCut result on commodity hardware.
The three attempts: FD (would take 100 hours), adjoint v1 (421s for
p=11), adjoint v2 with instrumented forward (373s for p=11, 6653s
for p=13). The overnight crash and the diagnostics module born from it.
**Key content**: timing comparison table (FD vs adjoint v1 vs v2),
the Diagnostics module (env-controlled, zero-cost when off, atexit
hook for crash diagnosis), the final c̃ value and what it says about
QAOA vs classical algorithms.

### 09 — The Pair Programmer That Never Sleeps
**Source material**: NEW — honest reflection on agentic coding throughout
**Story**: Every algorithmic innovation in this series involved an AI
coding agent. Not as autocomplete — as a genuine collaborator that reads
papers, translates between languages, writes tests, debugs at 2 AM, and
asks "should I run the full suite?" This post is an honest accounting of
what the agent did, what it couldn't do, and what the collaboration
actually looks like in practice.
**Key content**:
- **What the agent did well**: translate 550 lines of C++ to Julia in
  one pass (charge adjoint), write 76 tests that caught three bugs the
  human missed, diagnose the Wirtinger conjugation error from element-
  wise sign patterns in a 16-element vector, build a diagnostics module
  from requirements to working code in 20 minutes, keep the project
  context across a multi-day session (this conversation is >1M tokens)
- **What the agent couldn't do**: decide which algorithm to implement
  (the human chose charge decomposition over other options), notice that
  `_replay_branch` was the performance bottleneck (needed wall-clock
  evidence), know when to stop debugging and ship (kept wanting to
  add more tests), judge whether a 4.5× adjoint ratio was "good enough"
  vs JPM's 3× (a research judgement, not an engineering one)
- **What surprised us**: the agent's ability to hold mathematical
  context (Wirtinger calculus, C-order vs F-order, charge channels)
  across hours of debugging. The speed of the test-first workflow —
  76 tests written before the code was wired in. The overnight crash
  that the agent's diagnostics module would have caught if we'd built
  it first instead of after.
- **The uncomfortable question**: if an AI agent can translate a C++
  adjoint into Julia, debug it, test it, and benchmark it — what is
  the human's role? Answer: direction, judgement, and taste. The human
  says "let's do the charge adjoint" and "yes, 4.5× is good enough,
  ship it." The agent executes. Neither could do this alone.
- **The productivity multiplier**: eight weeks of algorithmic innovation
  that would normally take a year. Not because the agent is smarter —
  because it never gets tired, never loses context, and can write a
  test suite in the time it takes a human to write a test plan.
**Tone**: Radically honest. Not AI hype, not AI skepticism. Just:
here's what happened, here's what worked, here's what didn't. Let the
reader draw their own conclusions about what this means for
computational science.

### 10 — What Julia Taught Me About Mathematics
**Source material**: NEW — retrospective
**Story**: Looking back at ten algorithmic innovations over eight weeks.
Which ones came from mathematical insight? Which came from code clarity?
Which came from studying other people's work? The case that programming
languages are research tools, not just implementation tools — and that
methods work, though hard to publish in traditional venues, is the
connective tissue that makes computational science reproducible.
**Key content**: the 10-innovation arc (WHT → adjoint → cost algebra →
normalisation → swarm → multi-machine → Double64 → charge decomp →
charge adjoint → diagnostics), Julia-specific features that enabled
each one, why blog-as-publication is legitimate for methods work, how
reproducibility (1875+ tests, public repo, DOI) serves the same
purpose as peer review of the implementation.
**The collaboration angle**: How a conversation between friends became
a co-authorship on a Google Quantum AI paper. Stephen running code on
Google's cluster, both debugging overflow at midnight, git branches as
coordination protocol across institutions. The human side of
computational science — five compute environments, three continents,
$50 in Azure spend, and a shared spreadsheet of best angles. Science
is a social activity, and the tools that make it work (git, tests,
warm-start packages) are as important as the algorithms.

## Series Navigation

Each post will include previous/next links. The first post includes
a series overview linking all parts.

## Tags

All posts: `Julia`, `quantum-computing`, `QAOA`, `performance`
Post-specific: `functional-programming`, `catamorphism` (01),
`automatic-differentiation` (02, 07), `optimization` (03, 08),
`testing`, `software-engineering` (04, 05, 10),
`tagless-final` (05),
`ai-assisted-development`, `agentic-coding` (09)

## Status

- 01: Draft exists (needs extraction from monolith + rewrite)
- 02: Draft exists (needs extraction + expansion)
- 03: Draft exists (needs extraction + expansion)
- 04: Need to be written (testing architecture)
- 05: Need to be written (cost algebra + tagless final connection)
- 06-10: Need to be written from scratch
