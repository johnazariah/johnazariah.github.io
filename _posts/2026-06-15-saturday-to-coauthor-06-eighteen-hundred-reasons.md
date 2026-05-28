---
    layout: post
    title: "Eighteen hundred reasons"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, testing, software-engineering]
    author: johnazariah
    summary: Part 6 of the project report. The seven-layer test architecture that made the numbers publishable when there was no external reference except the ones the engine produced itself, four different ways, that had to agree.
---

_Part 6 of From Saturday to Co-Author. [Part 5 covered the cost algebra](/2026/06/11/saturday-to-coauthor-05-the-algebra-that-runs-itself.html). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

Here is the uncomfortable shape of the problem this post exists to solve.

You compute a new number. Not a small extension to a known one; a number nobody has computed before, on a problem family with no published exact reference at finite $D$. You cannot check it against a paper. You cannot eyeball it; $\tilde c = 0.8807$ looks no more or less plausible than $\tilde c = 0.8812$, and the difference matters. The instinct to ask "is it right?" has nowhere to go.

This post is what was on the other end of that instinct: a seven-layer test architecture, 1875 assertions across 22 files, and one specific bounds check whose absence cost a day in [Part 4](/2026/06/08/saturday-to-coauthor-04-the-walls.html) and which now exists as a permanent line in Layer 6.

It is the least glamorous post in the series. It is also the one that allows every other number quoted in any other post to mean anything.

---

## The seven layers, briefly

The harness is organised from cheapest and most structural at the bottom to most expensive and most physical at the top. Each layer assumes the ones below it pass.

### Layer 1: structural

Tree construction, tensor dimensions, hyperindex bit positions, branching factors. If `TreeParams(3, 4, 5)` does not produce a tree with the right branching factor and the right node count at each level, nothing else in the pipeline matters and there is no point running it. These tests catch off-by-one errors, typos, and indexing-convention misreadings of the Basso paper. They run in microseconds.

### Layer 2: known-value anchors

MaxCut on 3-regular graphs at $p = 1$ has a known closed-form answer from Farhi, Goldstone and Gutmann (2014): $\tilde c = \tfrac{1}{2} + \tfrac{\sqrt{3}}{9} \approx 0.6924$, attained at $\gamma^* = 0.6156, \beta^* = 0.3927$. The harness checks this to ten digits. The trivial case at zero angles must give $\tilde c = 0.5$. These anchors catch sign errors, normalisation bugs, and convention mismatches at the cheapest possible test point.

This is the layer that the cost algebra from [Part 5](/2026/06/11/saturday-to-coauthor-05-the-algebra-that-runs-itself.html) earns its keep at. The MaxCut anchor and the XORSAT runs go through the *same engine*. Validating the engine on the anchor validates it for the problem family with no anchor of its own, because the only thing that changes between them is the algebra.

### Layer 3: cross-evaluator congruence

This is the layer that catches the subtle bugs.

By the end of the project there were four independent implementations of the same mathematical function: the Basso branch-tensor evaluator from [Part 2](/2026/06/01/saturday-to-coauthor-02-the-fold-under-the-tree.html); the charge decomposition evaluator from [Part 7](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html) that uses a $\mathbb{Z}_2 \times \mathbb{Z}_2$ character-table decomposition; a reduced-basis variant that exploits tensor symmetry; and a Metal `Float32` GPU forward used only for cross-validation at low $p$.

The Basso evaluator and the charge evaluator share no code. They use different data structures, different iteration orders, and different mathematical decompositions of the same underlying quantum-mechanical expectation. The test asserts they agree to $10^{-10}$ at every $(k, D, p)$ the harness sweeps over.

```julia
@testset "k=$k, D=$D, p=$p" for (k, D, p) in test_grid
    basso_val  = basso_parity_expectation(params, angles)
    charge_val = charge_parity_expectation(params, angles)
    @test charge_val ≈ basso_val atol = 1e-10 rtol = 1e-10
end
```

The argument for why this matters is the same shape as the argument in the Tuesday 24 March email from [Part 5](/2026/06/11/saturday-to-coauthor-05-the-algebra-that-runs-itself.html): two implementations sharing no code and agreeing to ten digits are evidence of correctness for both, because the only thing they share is the mathematics they are both trying to compute. This is the computational equivalent of two independent experimental groups reproducing each other's result with different equipment.

### Layer 4: gradient congruence

Four ways of computing the gradient of the same scalar objective:

- ForwardDiff via `Dual{Float64}`, exact through the chain of operations.
- Central finite differences via small perturbations of the forward.
- The manual Basso adjoint, hand-derived in reverse-mode through the WHT pipeline.
- The manual charge adjoint, hand-derived in reverse-mode through the charge pipeline.

The harness asserts all four agree at every sampled point. The argument is identical to Layer 3, one level up: four implementations sharing only the underlying mathematics, all agreeing, is evidence the mathematics has been faithfully encoded in all four. A bug that produces plausible-looking wrong gradients across four implementations would be spectacularly unlucky.

### Layer 5: optimiser congruence

L-BFGS, when given gradients from two different backends, must converge to the same optimum. The harness runs the optimiser with `autodiff = :adjoint` and again with `autodiff = :charge_adjoint`, asserts the final $\tilde c$ values agree to within tolerance, and (separately) asserts the final angles are close in parameter space.

A bug that produces a correct value at sample points but a slightly wrong direction during descent would slip past Layer 4 and be caught here. The optimisation trajectory is more sensitive to gradient errors than any individual evaluation is.

### Layer 6: physical bounds

This is the layer whose absence cost the day in [Part 4](/2026/06/08/saturday-to-coauthor-04-the-walls.html) at $(k = 7, D = 8)$.

The expected satisfaction fraction $\tilde c$ lives in $[0, 1]$ by the definition of the problem. The gradient is finite. The value at zero angles is $1/2$. The gradient at a known optimum is near zero. These are not algorithmic claims; they are properties any correct implementation must have, by virtue of what the quantity *is*.

```julia
@test -1e-9 ≤ v ≤ 1.0 + 1e-9
@test all(isfinite, γg)
@test all(isfinite, βg)
```

In production, the bounds check is not only a test; it is wired into the evaluator and the merge script as an invariant. When the evaluator produces $\tilde c = 21.44$ (it has, exactly once, on Stephen's cluster at high arity), the bounds check halts the run, names the failure, and refuses to let the optimiser act on the corrupted value. This is what should have existed from day one and did not; this is the institutional form the lesson now takes.

The normalisation-and-overflow test file alone has 273 assertions. Each one is the artefact of a specific failure mode in the project's history. Every overflow case that surprised us, every threshold value that turned out to be wrong, every signal-crushing normalisation that looked right but was not: each became a test. The test file is the project's memory of how it has failed before.

### Layer 7: monotonicity

For a fixed $(k, D)$, $\tilde c(p)$ is non-decreasing in $p$, because a depth-$(p+1)$ QAOA circuit contains the depth-$p$ circuit as a special case (set the extra angles to zero). If the harness sees a decrease, the optimiser at the higher depth has found a worse basin than the optimiser at the lower depth, which is a signal that the warm-start angles are misleading, not that the engine is broken. The monotonicity check runs across the whole results table rather than on individual function calls; it flags entries for re-examination rather than failing the build.

This is also the layer the merge script across machines (from [Part 4](/2026/06/08/saturday-to-coauthor-04-the-walls.html)) enforces during multi-machine runs.

## What the layers buy that no single layer buys

Each layer alone is a weak test. Layer 2 catches gross errors; Layer 3 catches algorithmic ones; Layer 6 catches representation failures. The strength is in the combination.

No single bug in the implementation can produce a result that passes all seven layers. A sign error breaks Layer 2. An indexing error breaks Layer 1 or Layer 3. An autodiff bug breaks Layer 4. An optimiser-stability bug breaks Layer 5. A representation failure breaks Layer 6. A bad warm-start breaks Layer 7. The kinds of bugs that survive any one of these are the bugs the next one is designed to catch.

A subtle bug that produces a plausible-looking wrong number, undetected, would have to be invariant under all four independent forward implementations, all four independent gradient implementations, both optimisation backends, the physical bounds, and the monotonicity ladder, simultaneously. That is a strong constraint. It is the constraint that lets the harness do the job the absence of a published reference cannot do.

## The numbers about the numbers

22 test files. 1875 assertions. The breakdown, abbreviated:

| Test file                  | What it tests                                | Approx. count |
|----------------------------|----------------------------------------------|---------------|
| `test_tree.jl`             | Tree structure, branching factors            | ~30 |
| `test_tensors.jl`          | Tensor construction, hyperindex bits         | ~50 |
| `test_basso_finite_d.jl`   | Basso evaluator forward pass                 | ~60 |
| `test_charge.jl`           | Charge evaluator congruence with Basso       | ~30 |
| `test_charge_adjoint.jl`   | Charge gradient against four references      | 76 |
| `test_adjoint.jl`          | Basso gradient against ForwardDiff           | ~40 |
| `test_normalization.jl`    | Overflow safety, bounds, high-$(k,D)$        | 273 |
| `test_optimization.jl`     | Optimiser convergence, mode selection        | ~80 |
| `test_cost_algebra.jl`     | MaxCut vs XORSAT dispatch                    | ~20 |
| `test_checkpointed_adjoint.jl` | Checkpointed gradient vs full            | ~30 |
| (12 more files)            | GPU, WHT, spectral, reduced basis            | ~1186 |

The shape of the table is the shape of the project's failure history. The normalisation file is the largest because that is where the most surprises lived. The `test_charge_adjoint.jl` file has 76 assertions written *before* the adjoint code existed, because the adjoint translation in [Part 7](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html) was test-first by design and three of the bugs caught that way were invisible at $p = 1$.

## What this had to do with publishing

This is the structural form of an argument made in plain English on Saturday 28 March. The mentor's view of the work, in his own words, leaned on the same property:

> *Your results are in some ways more helpful to me because you have shared your code and data and documented what you have done. The sanity checks against Eddie's published numbers are very reassuring.*

The 22 test files and 1875 assertions are the documentary form of that. The sanity checks are not a one-off claim, they are a permanent property of the build. Anyone who pulls the repository runs them. Anyone who modifies a line of the fold engine runs them. If, two years from now, someone reproduces our numbers and gets a different answer, the test suite is what locates the disagreement.

This is what "reproducible computational science" is, when reproducibility is taken seriously rather than gestured at. It is not a clean README and a frozen dependency list. It is a test architecture that asserts, structurally and continuously, that the engine computes what it claims to compute.

---

The cross-evaluator congruence in Layer 3 assumes there is a second evaluator to be congruent with. The Basso evaluator was ours. The second one we did not derive. The next post is about where it came from, what it cost to reimplement, and what we owed to the people who derived it first.

---

_Next: **Learning from the masters**, on studying QOKit, the JPM team's open-source toolkit, finding a deeper mathematical structure than we had derived on our own, and reimplementing it from first principles in Julia._

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). The full test suite is in [`test/`](https://github.com/johnazariah/qaoa-xorsat/tree/main/test). The cross-evaluator congruence tests live in `test/test_charge.jl`. The 273 bounds and normalisation assertions are in `test/test_normalization.jl`._
