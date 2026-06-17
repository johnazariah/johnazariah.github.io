---
    layout: post
    title: "Learning from the masters"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, automatic-differentiation, software-engineering]
    author: johnazariah
    summary: Part 7 of the project report. Studying QOKit, the JPM team's published implementation, finding a deeper mathematical structure than we had derived on our own, and reimplementing it from first principles in Julia.
---

_Part 7 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/). [Part 6 covered the test architecture](/2026/06/15/saturday-to-coauthor-06-eighteen-hundred-reasons.html). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

The Phase 1 paper was submitted to arXiv on Monday 27 April. The XORSAT campaign was done. Fifteen $(k, D)$ instances of Max-$k$-XORSAT had been swept; thirteen of them beat the strongest classical baseline in the comparison; the methodology held under cross-evaluator review.

This post is about what happened next, and about a piece of mathematics I did not derive.

The natural follow-on question after Phase 1 was MaxCut at depth: how far could a single workstation push exact finite-depth QAOA on regular MaxCut, the canonical benchmark? The answer (Part 8) was depth fourteen on a Mac, but only after a piece of structural mathematics that I learned by reading someone else's code.

---

## The implementation next door

[QOKit](https://github.com/jpmorganchase/QOKit) is an open-source QAOA toolkit from JPMorganChase's Global Technology Applied Research group. It implements exact finite-depth QAOA evaluation for several problem classes, including Max-$k$-XORSAT. The JPM team had pushed depth further than we had during Phase 1, using cluster compute and GPU acceleration; three members of that team, Abid Khan, Ruslan Shaydulin and Sami Boulebnane, were among the eventual co-authors on the joint paper.

When the dust from Phase 1 settled, the natural next thing was to read their code. The version of the engine we had been running ([Part 2](/2026/06/01/saturday-to-coauthor-02-the-fold-under-the-tree.html)) was $O(p^2 \cdot 4^p)$. They were running at the same arities at depths we had not reached. Either their constant factor was much better than ours, which would have been surprising at the scale of the gap, or they had a structurally different algorithm.

They had a structurally different algorithm.

## The insight we had not found

The Basso branch-tensor representation our evaluator uses lives on a vector of $2^{2p+1}$ amplitudes that gets folded $p$ times. Each fold uses a Walsh-Hadamard transform over the full vector and costs $O(p \cdot 4^p)$ per level; $p$ levels gives $O(p^2 \cdot 4^p)$. That was the engine the Phase 1 paper ran on.

The JPM team's evaluator decomposes the doubled density matrix into four independent *charge channels* via the $\mathbb{Z}_2 \times \mathbb{Z}_2$ character table. Each channel is a $4^p$-element tensor on which the fold reduces to a sequence of *mode products*: $4 \times 4$ matrix multiplications applied along successive axes. One pass through all $p$ axes of the tensor costs $O(p \cdot 4^p)$. There is no outer loop over $p$ rounds at this cost; the per-channel pass *is* the round structure, telescoped. The whole evaluator runs at $O(p \cdot 4^p)$.

One factor of $p$ stripped from the forward cost. The kind of speedup that does not change the asymptotic class of the algorithm but does change which depths are reachable on which hardware. At the arities and depths Phase 2 wanted to reach, it was the difference between feasible and not.

This is exactly the kind of mathematical structure I would not have found on my own. The factorisation in [Part 2](/2026/06/01/saturday-to-coauthor-02-the-fold-under-the-tree.html) was visible because the Basso recurrence, written as a fold, exposes its XOR convolution structure naturally to anyone who has spent time around Walsh-Hadamard transforms. The charge decomposition is one level deeper: it is a representation-theory observation about how the doubled circuit factors under a discrete symmetry, and it is the kind of observation that comes from people who have lived inside QAOA for years. The JPM team had lived inside QAOA for years; I had been inside it for a month.

## What we owed and what we added

The JPM team derived the charge decomposition. They published the mathematics. They open-sourced clean, readable code in Python and JAX. When the dust settled, what we owed them was the entire structural speedup that made Phase 2 thinkable on commodity hardware.

What I could add, on my side, was a second, independent implementation. Our charge evaluator and our original Basso evaluator share no code, and they agree to ten digits. That is the Layer 3 argument from [Part 6](/2026/06/15/saturday-to-coauthor-06-eighteen-hundred-reasons.html) in action: two implementations of the same quantity, sharing nothing but the mathematics they both encode, agreeing. The cross-validation is not a courtesy; it is the structural reason the MaxCut-at-depth numbers I went on to compute deserve to be trusted. The Phase 1 XORSAT paper had already shipped, on the Basso engine alone; this reimplementation was for what came next.

The reimplementation also gave us the algebra integration. The charge evaluator plugs into the same `CostAlgebra` as the Basso evaluator. The same optimiser, the same test harness, the same diagnostics. Adding it to the codebase was adding a new gradient backend; nothing else changed.

## The translation, and three bugs that hide below $p = 2$

The translation is ~450 lines of Python with JAX to ~500 lines of Julia. The mathematics is the same. The conventions are not. The translation took four days. Three of the bugs found in those four days were invisible at $p = 1$, and the harness from [Part 6](/2026/06/15/saturday-to-coauthor-06-eighteen-hundred-reasons.html) is what surfaced them.

### Bug 1: C-order vs F-order reshape

Python's NumPy reshapes arrays in C-order (row-major). Julia's `reshape` is F-order (column-major). For a one-dimensional vector reshaped to a $1 \times 4$ matrix, the two are identical and the $p = 1$ tests all pass. At $p = 2$ the reshape becomes $4 \times 4$ and the two orderings disagree.

The fix is a small helper:

```julia
function _reshape_c(v, dims...)
    permutedims(reshape(v, reverse(dims)...), length(dims):-1:1)
end
```

It exists because two languages disagree about which axis is "first." There is no deeper meaning to it. It is a paper cut that becomes load-bearing the moment any test sweep includes $p \geq 2$.

### Bug 2: transpose versus adjoint

Python's `.T` is a plain transpose. Julia's `'` is the conjugate transpose. For real matrices these are identical; for the complex-valued matrices that show up throughout the charge pipeline they are not. A single character, one operator, wrong answer.

This was found by the same cross-evaluator congruence test at $p \geq 2$.

### Bug 3: the $\gamma / 2$ convention

The QOKit code's charge weight matrix takes the full angle $\gamma$. Our API convention, inherited from Basso et al., is $\gamma / 2$. Getting the convention wrong produces values that are plausible, self-consistent, and entirely wrong: $\cos(\gamma)$ and $\cos(\gamma / 2)$ are both smooth functions of the angle with values in the same range. There is no internal signal that tells you which one is right.

The only signal is the cross-evaluator congruence test against the Basso evaluator at $p \geq 2$ with non-trivial angles. The Basso evaluator uses the Basso convention; the charge evaluator was using the QOKit convention; the values disagreed in the third decimal place at $p = 2$. That disagreement is what the harness was there to surface.

## The adjoint, and one bug from complex analysis

Once the forward was right, the adjoint was the next translation: ~550 lines of C++ from `adjoint_branch.cpp`, `adjoint_primitives.cpp` and `adjoint_root.cpp`, into a single Julia file. The harness for this lived in `test/test_charge_adjoint.jl`, 76 assertions written *before* the adjoint code existed; the test set was based on the existing finite-difference and ForwardDiff references.

The translation produced a working forward. The first run of the adjoint failed 24 of the 76 tests. The diagnostic from the failing tests was specific:

```
fb[2]: code = 0.01971 - 0.03598i
       FD   = -0.01971 - 0.03598i    # real part sign-flipped
```

Real-valued elements were correct. Imaginary parts of complex elements were correct. Real parts of complex elements were sign-flipped. The pattern named the bug.

The derivative of a real-valued loss with respect to a complex variable is governed by Wirtinger calculus. For a real loss $L$ and a complex variable $z$:

$$
\frac{\partial L}{\partial z} \;=\; \overline{\left(\frac{\partial L}{\partial \bar z}\right)}
$$

In the root backward we were accumulating `fb += dz * tv` where the chain rule wanted `fb += conj(dz) * conj(tv)`. Two `conj()` calls. The bug was invisible at $p = 1$ because the branch tensor at $p = 1$ has only real entries, and `conj(x) = x` when `x` is real.

This is the kind of bug that exists because the harness exists. Without 76 assertions checking against four independent gradient references at multiple arities and depths, the wrong adjoint would have shipped, the gradients would have been wrong in a way the optimiser would have been happy to follow, and the optimisation would have converged to subtly wrong angles. The whole production result would have been off by a fraction of a percent that nobody could have located by reading the code. The test caught it because the test was specific enough to localise the failure as "real parts of complex elements."

Two more bugs followed in the same vein. A missing coefficient adjoint at intermediate rounds that left the gamma gradient zero in a regime where it should have been small but nonzero. A C-order indexing into the flattened $V$ matrix that worked at $n_\mathrm{ch} = 1$ (which is to say $p \leq 2$) and broke at $n_\mathrm{ch} = 4$ ($p = 3$). The first was found by a "gamma gradient at $p \geq 2$" test that returned zero where ForwardDiff returned non-zero. The second was found by a $p = 3$ adjoint test that failed where the $p = 2$ adjoint tests had passed.

All three: invisible at the degenerate case, surfaced by the harness at the first non-degenerate one, diagnosed by the specific failure pattern, fixed structurally.

## The structural fix and the headline ratio

The first version of the adjoint reconstructed the forward intermediates by replaying the forward pass for every backward level. The forward already computed those intermediates; the replay threw them away and recomputed them. The replacement was an instrumented forward (`_charge_branch_instrumented`) that retains the per-level $V$, $F$ and coefficient tensors on the way up, so the backward can read them on the way down.

Measured cost of the charge manual adjoint after this fix: **about 4.5 times a single forward evaluation, independent of $p$**. The JPM team's C++ adjoint in QOKit sits at around three times forward; the gap is allocation overhead in the recursive Phase 2 backward, where their C++ preallocates a pool and our Julia allocates per level. Closing that gap is doable and has diminishing returns. Four-and-a-half-times-forward was the number we shipped, and it is the number the Phase 2 $p = 14$ runs in [Part 8](/2026/06/22/saturday-to-coauthor-08-fourteen.html) depend on.

## A note about how this is supposed to work

This is the post that most embodies the dedication at the top of the series.

The mathematics of the charge decomposition is the JPM team's. They derived it; they implemented it; they published it; they made the code clean enough that someone outside their group could read it. What I built was an independent Julia implementation, a cross-evaluator congruence proof against our own Basso engine, and the algebra integration that let the rest of the pipeline absorb it. My MaxCut results are stronger because two implementations agree, not because either of them is the canonical one. The Phase 1 XORSAT paper, by contrast, had already shipped on the Basso engine; this work was for the MaxCut-at-depth campaign that follows.

Studying a colleague's implementation is a research skill in its own right. It takes humility: they solved something you did not. It takes patience: their conventions are not your conventions, and every difference is a potential bug. It takes a test harness, because the differences will not all be visible at $p = 1$. The payoff is that you get to stand on someone's shoulders rather than dig your own foundation.

Thank you to the JPM team for QOKit, code clear enough that the mathematics had nowhere to hide.

---

The charge evaluator and its manual adjoint were what made depth fourteen on a Mac thinkable. Thinkable is not the same as runnable. The next post is what the rest of the way cost.

---

_Next: **Fourteen**, on what it took to compute $p = 14$ MaxCut at $D = 3, 4, 5, 6, 7, 8, 9$ on a sixty-four-gigabyte Mac Studio, the five memory fixes that brought a hundred-and-twenty-gigabyte projected working set down inside the machine, and the diagnostics module that was born from an earlier silent failure._

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). The charge evaluator is in `src/charge.jl`; the manual charge adjoint is in `src/charge_manual_adjoint.jl`; the 76-assertion adjoint test set is in `test/test_charge_adjoint.jl`. The QOKit codebase, whose mathematics this post is built on, is at [github.com/jpmorganchase/QOKit](https://github.com/jpmorganchase/QOKit)._
