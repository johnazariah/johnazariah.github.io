# The Manual Adjoint, Manually

_Part 7 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — translating 550 lines of C++ adjoint code into Julia, finding three bugs that were all invisible at $p = 1$, and the test-first methodology that caught every one of them._

---

The charge evaluator from [Part 6](/tags/from-saturday-to-coauthor/) gave us $O(p \cdot 4^p)$ forward evaluation — 14× faster than the Basso evaluator at $p = 14$. But with finite-difference gradients, each optimisation step still cost $(4p{+}1)$ forward evaluations. At $p = 14$: 57 evaluations per gradient, ~27 minutes per step, ~100 hours for a full optimisation.

We needed exact gradients through the charge evaluator. The QOKit team had published a C++ adjoint implementation alongside their Python forward pass. Time to study it.

---

## Tests First

Before writing a single line of adjoint code, we wrote the tests.

76 of them.

```julia
@testset "Charge adjoint differentiation" begin
    @testset "value matches charge_expectation" begin ... end       # 8 tests
    @testset "value matches basso_expectation" begin ... end        # 5 tests
    @testset "gradient matches ForwardDiff" begin ... end           # 16 tests
    @testset "both clause_sign values" begin ... end                # 24 tests
    @testset "zero angles" begin ... end                            # 10 tests
    @testset "matches basso adjoint gradient" begin ... end         # 10 tests
    @testset "near-zero gradient at MaxCut optimum" begin ... end   # 3 tests
end
```

The tests validated against _four independent gradient references_: ForwardDiff (exact, algorithmic), central finite differences (approximate, numerical), the Basso adjoint (exact, different mathematics), and known optima (where the gradient should be near zero).

Every test case included $p \geq 2$ — because we'd learned from the charge evaluator translation that $p = 1$ is the degenerate case where everything works.

We ran the tests against the existing finite-difference `charge_expectation_and_gradient` first: 76/76 passing. Now we had a reference. Any new implementation that passes all 76 tests is correct. Any that fails is wrong — and the specific failing test tells us _where_ it's wrong.

## The Translation

The QOKit team's C++ adjoint lives in three files: `adjoint_branch.cpp` (~550 lines), `adjoint_primitives.cpp` (~155 lines), and `adjoint_root.cpp`. The architecture:

1. **Forward pass with caching** — run the full charge evaluation, saving all intermediates
2. **Root backward** — reverse through the final measurement and WHT chain
3. **Branch backward** — reverse through mode products, power, permutation, Phase 2, Phase 1

We translated this into a single Julia file: `charge_manual_adjoint.jl`, ~500 lines. The adjoint primitives — `_mode_product_adjoint!` and `_wht_adjoint!` — were verified individually against finite differences before being composed.

Then we ran the 76 tests.

52 passed. 24 failed.

## Bug 1: Wirtinger Conjugation

The first test output told us exactly what was wrong:

```
fb[2]: code = 0.01971 - 0.03598i
       FD   = -0.01971 - 0.03598i    ← real part sign-flipped
```

Every complex element of the root backward output had its _real part sign-flipped_ compared to the finite-difference reference. Real elements were correct. The imaginary parts were correct. Only the real parts of complex elements were wrong.

The diagnosis: the derivative of a real-valued loss with respect to a complex variable requires Wirtinger calculus. For a real loss $L$ and complex variable $z$:

$$\frac{\partial L}{\partial z} = \overline{\left(\frac{\partial L}{\partial \bar{z}}\right)}$$

In the root backward, we were accumulating `fb += dz * tv` — but we needed `fb += conj(dz) * conj(tv)`. The conjugation is the Wirtinger correction for propagating a real gradient backward through complex-valued operations.

This bug was invisible at $p = 1$ because the branch tensor at $p = 1$ has only real entries — `conj(x) = x` when `x` is real.

Fix: two `conj()` calls. One line changed.

**Score: 73/76 passing.**

## Bug 2: The Missing Coefficient Adjoint

The root contraction applies `root_charge_weights(γ[ℓ])` at each intermediate round — a set of four complex coefficients that scale the charge channels. At the final round ($\ell = p$), the gamma gradient through these coefficients was implemented. At intermediate rounds ($\ell < p$): a TODO comment.

```julia
# TODO: coefficient adjoint for gamma through u
# For now this contribution is small and partially captured
# by the branch-level gamma gradients
```

"Small" and "partially captured" are not "correct." The test suite disagreed:

```
gg_root[1] = 0.000000    ← should be -0.319
```

The fix required propagating $\partial(\text{ca\_final}) / \partial(\gamma_\ell)$ forward through the coefficient chain — replaying the coefficient expansion from round $\ell$ onward and accumulating the gradient via the chain rule. About 30 lines of code.

**Score: still 73/76.** This fix only affected gamma gradients, which were already passing for the cases that worked. The remaining 3 failures were beta gradients at $p = 3$.

## Bug 3: C-Order V\_flat

The last bug was the most insidious.

The charge evaluator's Phase 1 produces a matrix $V$ of shape `(n_ch, 4)`, which the Phase 2 backward uses to compute beta gradients. The `_replay_branch` function (which reconstructed intermediates for the backward pass) stored `V_flat = vec(V)` — Julia's column-major flattening.

But the Phase 2 backward indexed `V_flat` in C-order: `V_flat[(i-1)*4 + s]`. For `n_ch = 1` ($p \leq 2$), there's no difference — a $1 \times 4$ matrix has the same layout in both orderings. For `n_ch = 4` ($p = 3$): different.

```
# F-order: V_flat = [V[1,1], V[2,1], V[3,1], V[4,1], V[1,2], ...]
# C-order: V_flat = [V[1,1], V[1,2], V[1,3], V[1,4], V[2,1], ...]
```

The fix: `V_flat = _vec_c(V)` — our C-order flattening helper, the same one we built for the charge evaluator translation.

**Score: 76/76. All tests passing.**

## The Elimination of \_replay\_branch

With the adjoint working, we had a performance problem. The `_replay_branch` function recomputed the entire forward pass for every backward level — doubling the work. The forward pass already computed these intermediates; we were throwing them away and recomputing them.

The fix: `_charge_branch_instrumented` — an instrumented version of the forward pass that computes $F$ and saves intermediates in a single pass. The adjoint then reads the saved intermediates instead of replaying.

This eliminated ~50% of the adjoint overhead. Cost dropped from ~6× forward to ~4.5× forward.

## The Speedup

The adjoint replaced finite-difference gradients (57 forward evaluations per gradient at $p = 14$) with exact gradients in ~4.5× a single forward evaluation. Measured wall-clock times for MaxCut ($k = 2, D = 3$) optimisation:

| $p$ | FD gradient | Adjoint gradient | Speedup |
|-----|------------|------------------|---------|
| 9 | 111.9s | 16.8s | 6.7× |
| 10 | 772.0s | 74.8s | 10.3× |
| 11 | 4,331s (72 min) | 373s (6 min) | 11.6× |
| 12 | ~19,800s (5.5 hr) | 1,646s (27 min) | 12× |
| 13 | ~90,000s (25 hr) | 6,653s (111 min) | 13.5× |

Every $\tilde{c}$ value matched between FD and adjoint to 10+ digits. Same optimum, different speed.

The QOKit team's C++ adjoint achieves ~3× forward cost. We're at ~4.5×. The gap is allocation overhead in the recursive Phase 2 backward — fresh `zeros(ComplexF64, N)` at every recursive call, where the C++ preallocates. Closing it would mean preallocating buffer pools, which is doable but diminishing returns. The real win was 57× → 4.5×.

## The Lesson

Three bugs. All invisible at $p = 1$. All caught by tests written before the code.

This is the test-first methodology paying off in the most concrete way possible. Without the 76-test suite, we would have shipped a charge adjoint that produces wrong gradients at $p \geq 2$ — wrong gradients that look plausible, feed into L-BFGS, and converge to suboptimal angles that nobody would question because the values are in the right ballpark.

The tests didn't just find the bugs. They _localised_ them. "Real parts of complex elements are sign-flipped" pointed directly at a conjugation issue. "Beta gradient wrong only at $p = 3$" pointed directly at a layout issue that only manifests when `n_ch > 1`. Each failing test was a diagnostic, not just a red light.

Write the tests first. Make them comprehensive. Include the degenerate cases _and_ the non-degenerate ones. Then write the code. Then run the tests. Then fix what's broken. This isn't software engineering dogma — it's how you avoid publishing wrong numbers in a Google Quantum AI paper.

---

_Next: [p=14 (and What It Means)](/tags/from-saturday-to-coauthor/) — the first exact $p = 14$ MaxCut result on commodity hardware. Three attempts, one overnight crash, and the diagnostics module born from it._

_Previous: [Learning From the Masters](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`src/charge_manual_adjoint.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/charge_manual_adjoint.jl) (manual adjoint, ~500 lines), [`test/test_charge_adjoint.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/test/test_charge_adjoint.jl) (76 gradient congruence tests), [`src/diagnostics.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/diagnostics.jl) (runtime diagnostics module). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
