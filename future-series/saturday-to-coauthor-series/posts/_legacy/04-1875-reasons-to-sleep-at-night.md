# 1,875 Reasons to Sleep at Night

_Part 4 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — how do you know your exact evaluator is exact? When you're computing numbers nobody has computed before, the only reference you can check against is the one you build yourself._

---

Here's the uncomfortable truth about computational science: when you produce a new number — a number nobody has computed before, using an algorithm nobody has implemented at this scale before — how do you know it's right?

You can't check it against a published value, because there isn't one. You can't compare it to another implementation, because yours is the first. You can't even sanity-check it by eyeballing the output, because $\tilde{c} = 0.8807$ looks exactly as plausible as $\tilde{c} = 0.8812$, and the difference matters.

This post is about the testing architecture that let us sleep at night. It's the least glamorous part of the project and the most important.

---

## The Confidence Pyramid

Our testing strategy has seven layers, each catching a different class of error. They're ordered from cheapest to most expensive, and each layer assumes the ones below it are passing.

### Layer 1: Structural Tests

The boring stuff. Tree construction, tensor dimensions, hyperindex bit positions, branching factors. If `TreeParams(3, 4, 5)` doesn't produce a tree with branching factor $(D{-}1)(k{-}1) = 6$, nothing else matters.

These tests run in microseconds and catch typos, off-by-one errors, and misunderstandings of the Basso paper's indexing conventions. They're the foundation.

```julia
@test branching_factor(TreeParams(3, 4, 5)) == 6
@test variable_count_at_level(TreeParams(3, 4, 5), 1) == 3
@test total_nodes(TreeParams(2, 3, 3)) == 40
```

### Layer 2: Known-Value Anchors

MaxCut on 3-regular graphs ($k = 2, D = 3$) at $p = 1$ has a known exact answer: $\tilde{c} = \tfrac{1}{2} + \tfrac{\sqrt{3}}{9} \approx 0.6924$, from [Farhi et al. (2014)](https://arxiv.org/abs/1411.4028). The optimal angles are known too: $\gamma^* = 0.6156, \beta^* = 0.3927$.

This is our anchor. If the evaluator doesn't return 0.6924 at these angles, something is fundamentally wrong. We test this to 10 digits:

```julia
@test c_charge ≈ 0.6924500847885 atol = 1e-6
```

We also test the trivial case: zero angles should always give $\tilde{c} = 0.5$ (no quantum advantage when the circuit does nothing). This catches sign errors and normalisation bugs.

### Layer 3: Cross-Evaluator Congruence

This is the layer that catches the subtle bugs. We have _four independent implementations_ of the same mathematical function:

1. **Basso evaluator** — the original WHT-based branch-tensor recurrence
2. **Charge evaluator** — the Z₂×Z₂ charge decomposition (completely different algorithm)
3. **Reduced-basis evaluator** — exploits tensor symmetry for a 4× constant-factor speedup
4. **GPU evaluator** — Float32 Metal implementation (limited precision but independent code path)

All four must agree to $10^{-10}$ at every $(k, D, p)$ we test:

```julia
@testset "k=$k, D=$D, p=$p" for (k, D, p) in [
    (2, 3, 1), (2, 3, 2), (2, 3, 3),
    (3, 4, 1), (3, 4, 2), (3, 4, 3),
    (3, 2, 1), (3, 2, 2),
    (4, 3, 1), (4, 3, 2),
]
    basso_val = basso_parity_expectation(params, angles)
    charge_val = charge_parity_expectation(params, angles)
    @test charge_val ≈ basso_val atol = 1e-10 rtol = 1e-10
end
```

Why this matters: the Basso evaluator and the charge evaluator share _no code_. They use different data structures, different iteration orders, different mathematical decompositions. If they agree, the agreement is strong evidence that both are computing the right thing — because the only thing they share is the underlying mathematics.

This is the computational equivalent of reproducibility in experimental science. Two independent labs, different equipment, same result.

### Layer 4: Gradient Congruence

Four gradient methods, each using different mathematics:

1. **ForwardDiff** — exact via dual numbers, independent of our code
2. **Central finite differences** — approximate but uses only the forward evaluator
3. **Basso manual adjoint** — hand-derived reverse-mode through the WHT pipeline
4. **Charge manual adjoint** — hand-derived reverse-mode through the charge pipeline

The charge adjoint was validated against all three others:

```julia
@testset "gradient matches ForwardDiff" begin
    fd_grad = ForwardDiff.gradient(objective, x)
    _, γ_grad, β_grad = charge_expectation_and_gradient(params, angles; clause_sign)
    @test γ_grad ≈ fd_grad[1:p] atol = 1e-6
    @test β_grad ≈ fd_grad[p+1:2p] atol = 1e-6
end
```

If the charge adjoint disagrees with ForwardDiff, the adjoint has a bug. If both disagree with the Basso adjoint, _somebody_ has a bug. If all four agree, the gradient is almost certainly correct — because finding a bug that produces plausible-looking wrong gradients across four different algorithms would be spectacularly unlucky.

### Layer 5: Optimizer Congruence

L-BFGS with `:adjoint` (Basso) and `:charge_adjoint` (charge manual adjoint) must converge to the same $\tilde{c}$ at every depth. Different gradient backends, same optimum:

```julia
r_basso = optimize_angles(params; autodiff=:adjoint)
r_charge = optimize_angles(params; autodiff=:charge_adjoint)
@test r_basso.value ≈ r_charge.value atol = 1e-8
```

If they disagree, the gradients have a bug that's only visible under optimisation — a sign error, a scaling error, or a convention mismatch that happens to produce the right value at test points but the wrong direction during descent.

### Layer 6: Physical Bounds

Regardless of implementation details, certain properties must always hold:

- $\tilde{c} \in [0, 1]$ — it's a probability
- Gradients are finite — no NaN, no Inf
- Value at zero angles = 0.5 — the trivial circuit
- Near-zero gradient at known optima — the optimizer already found this point

```julia
@test -1e-9 ≤ v ≤ 1.0 + 1e-9
@test all(isfinite, γg)
@test all(isfinite, βg)
```

These tests run at high $(k, D)$ where overflow and cancellation lurk. They caught the normalisation bug (Entry 25), the always-normalise bug (Entry 26), and two separate cancellation failures.

### Layer 7: Monotonicity

For MaxCut, $\tilde{c}(p) \geq \tilde{c}(p{-}1)$ — deeper circuits can only do better, because they include the shallower circuit as a special case (set the extra angles to zero).

If depth $p + 1$ produces a worse $\tilde{c}$ than depth $p$, the optimizer found a bad basin — not a bug per se, but a signal that the warm-start angles are leading somewhere suboptimal. The monotonicity filter flags these cases for investigation.

This isn't a unit test; it's a data quality check that runs on the full results table. It caught three cases where the memetic optimizer had converged to a local minimum that was worse than the warm-start from $p - 1$.

---

## The Numbers

Twenty-two test files. 1,875 individual test assertions. Here's the breakdown:

| Test file | What it tests | Count |
|-----------|--------------|-------|
| `test_tree.jl` | Tree structure, branching factors | ~30 |
| `test_tensors.jl` | Tensor construction, hyperindex | ~50 |
| `test_basso_finite_d.jl` | Basso evaluator forward pass | ~60 |
| `test_charge.jl` | Charge evaluator vs Basso | ~30 |
| `test_charge_adjoint.jl` | Charge gradient (4 reference methods) | 76 |
| `test_adjoint.jl` | Basso gradient vs ForwardDiff | ~40 |
| `test_checkpointed_adjoint.jl` | Checkpointed gradient vs full | ~30 |
| `test_normalization.jl` | Overflow safety, bounds, high-(k,D) | 273 |
| `test_optimization.jl` | Optimizer convergence, mode selection | ~80 |
| `test_cost_algebra.jl` | MaxCut vs XORSAT dispatch | ~20 |
| ... (12 more files) | GPU, WHT, spectral, reduced basis | ~1,186 |

The normalization tests alone are 273 assertions — because that's where the bugs hid. Every (k, D) pair that overflowed, every threshold that was wrong, every signal-crushing normalisation that looked right but wasn't: each one became a test.

## Why This Matters

The paper's results were trusted because every number was independently verified through multiple code paths. No single bug could produce a plausible-looking wrong answer without being caught by at least two other test layers.

When Stephen ran our code on Google's 50-node cluster and got $(3,4)$ $p = 13 = 0.8807$, neither of us stayed up worrying about whether it was right. The cross-evaluator tests said the Basso and charge evaluators agree. The gradient tests said all four methods produce the same derivatives. The optimiser tests said both autodiff modes converge to the same optimum. The normalization tests said the values are physically meaningful.

1,875 assertions. That's how many reasons we had to sleep at night.

---

_Next: [The Algebra That Runs Itself](/tags/from-saturday-to-coauthor/) — how the CostAlgebra pattern is a tagless final encoding, and why that matters for extensibility and correctness._

_Previous: [The Walls](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`test/`](https://github.com/johnazariah/qaoa-xorsat/tree/main/test) (all 22 test files), [`test/test_normalization.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/test/test_normalization.jl) (273 overflow/bounds tests), [`test/test_charge_adjoint.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/test/test_charge_adjoint.jl) (76 gradient congruence tests). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
