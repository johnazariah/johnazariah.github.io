# Three Gradients and a Type Parameter

_Part 2 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — how Julia's parametric type system made it trivial to test three completely different gradient strategies in one codebase, and why the hand-derived adjoint was the only one that could reach depth 14._

---

The WHT factorisation from [Part 1](/tags/from-saturday-to-coauthor/) gave us an evaluator that could compute $\tilde{c}(p)$ in seconds instead of centuries. But evaluation is only half the problem. The QAOA angle optimisation is a continuous, non-convex optimisation over $2p$ parameters ($\gamma_1, \ldots, \gamma_p, \beta_1, \ldots, \beta_p$). To find the optimal angles, L-BFGS needs gradients.

This is the story of three gradient strategies — finite differences, automatic differentiation, and a hand-derived adjoint — tested head-to-head in a single Julia binary. The punchline: only one of them works at depth $p \geq 4$, and Julia made it embarrassingly easy to find out which.

## Why Gradients?

QAOA angle optimisation is a continuous, non-convex problem over $2p$ real parameters. You're searching for the angles $(\gamma_1, \ldots, \gamma_p, \beta_1, \ldots, \beta_p)$ that maximise $\tilde{c}$ — the expected fraction of satisfied constraints. The landscape is smooth but has many local optima, especially at high depth.

The workhorse algorithm for this kind of problem is [L-BFGS](https://en.wikipedia.org/wiki/Limited-memory_BFGS) — a quasi-Newton method that approximates the curvature of the objective function using the last few gradient evaluations. Think of it as gradient descent with a sense of direction: instead of just walking downhill, it remembers enough about the terrain to take intelligent diagonal steps. It's the default choice for smooth optimisation with 10–100 parameters, and it converges dramatically faster than gradient descent — but it needs accurate gradients. Give it noisy gradients and it will confidently walk off a cliff.

So: three gradient strategies, one optimizer, same convergence criteria. Which gradient wins?

---

## The Obvious First Try: Finite Differences

$$\frac{\partial \tilde{c}}{\partial \gamma_i} \approx \frac{\tilde{c}(\gamma_i + h) - \tilde{c}(\gamma_i - h)}{2h}$$

Cost: $(4p + 1)$ evaluations per gradient. At $p = 14$, that's 57 evaluations. Simple, universal, requires no knowledge of the function's internals.

The problem shows up at $p = 4$. The objective function has a noise floor from floating-point arithmetic — the $O(4^p)$ complex additions accumulate roundoff. By $p = 4$, the step size $h$ that minimises the total error (truncation + roundoff) still produces gradient estimates too noisy for L-BFGS to converge. The optimizer oscillates instead of descending.

Finite differences cannot converge at $p \geq 4$. We needed exact gradients.

## The Elegant Second Try: ForwardDiff.jl

Julia's `ForwardDiff.jl` provides automatic differentiation by propagating _dual numbers_ through arbitrary code. A dual number carries both the value and its derivative: $a + b\epsilon$ where $\epsilon^2 = 0$. Every arithmetic operation — addition, multiplication, `sin`, `cos`, `exp` — is overloaded to propagate the derivative chain automatically.

For this to work, every function in the pipeline must accept dual numbers instead of plain `Float64`s. In most languages, this would require rewriting the entire evaluator, wrapping it in a framework, or writing a code transformation pass.

In Julia, I made _one change_:

```julia
# Before
struct QAOAAngles
    γ::Vector{Float64}
    β::Vector{Float64}
end

# After
struct QAOAAngles{T<:Real}
    γ::Vector{T}
    β::Vector{T}
end
```

That's it. One type parameter. Julia's compiler infers `T = Dual{Float64, N}` when ForwardDiff calls the function, and the _entire 500-line evaluation pipeline_ — WHTs, constraint folds, branch tensor iterations, complex exponentials — works with dual numbers without a single other line changed.

No wrappers. No code generation. No "tape" recording operations. The same source code that evaluates `Float64` values also computes exact derivatives through `Dual{Float64}` values, compiled to specialised machine code for each type.

The result: exact gradients, correct to machine precision, with zero manual differentiation.

The cost: ForwardDiff propagates $2p$ dual partials through every operation, making each gradient evaluation cost $\sim 2p$ times a plain forward pass. At $p = 8$, that's 16× overhead. At $p = 14$, it's 28×. Tractable but expensive.

## The Brutal Third Try: Hand-Derived Adjoint

ForwardDiff's cost scales linearly with the number of parameters because it propagates derivatives _forward_ — one partial per parameter. The reverse-mode alternative (backpropagation, in neural network parlance) propagates derivatives _backward_ from the output, computing the entire gradient in a single backward pass.

Julia has reverse-mode AD packages — Zygote.jl, Enzyme.jl — but they all choked on our evaluator. Zygote doesn't support in-place mutations (`_wht_transform!(out, in)`). Enzyme failed on the complex-to-real conversion at the root. Neither handled the mix of complex arithmetic, in-place WHTs, and power operations that make up the Basso pipeline.

So I did it by hand.

The key insight that made this feasible: **the WHT is its own adjoint.** The Hadamard matrix $H$ satisfies $H = H^\top$, so the backward pass through a WHT is... another WHT. The most expensive operation in the forward pass has a trivial backward pass.

The $\beta$ gradients use a log-derivative trick. The mixer matrix $M(\beta)$ has entries $\cos\beta$ and $-i\sin\beta$. The derivative $\partial M / \partial \beta$ introduces $-\sin\beta$ and $-i\cos\beta$ terms — which, after simplification, yield factors of $-\tan\beta$ for the cosine entries and $\cot\beta$ for the sine entries. No need to differentiate through the entire tensor contraction; just multiply by a diagonal correction matrix.

The $\gamma$ gradients work similarly through the constraint kernel: $\partial\kappa/\partial\gamma_i = -\tfrac{1}{2}s_i \sin(\ldots)$, which the WHT diagonalises just like the forward pass.

The result: the full gradient — all $2p$ partial derivatives — in **1.6× a single forward evaluation**, independent of $p$.

Let me say that again. ForwardDiff costs $2p$ times a forward pass. The adjoint costs 1.6×. At $p = 14$, that's a **17.5× speedup** over ForwardDiff and the difference between a three-hour optimisation and a two-day one.

## The Head-to-Head

You could do this comparison in any language. Write three gradient implementations, call them from the same optimizer, compare the results. The difference isn't capability — it's cost.

In C++ or Python, each gradient strategy requires its own version of the evaluator. Finite differences call the evaluator as a black box — easy. ForwardDiff-style AD needs the evaluator rewritten to accept dual numbers, or wrapped in a tape-recording framework like JAX, which means translating the WHT and complex arithmetic into framework-compatible operations. The manual adjoint needs a separate backward pass that mirrors the forward — a second implementation that must stay in sync with the first.

Three gradient strategies, three codebases. Every time the forward evaluator changes, three things need updating.

In Julia, all three strategies use the **same 500 lines of evaluator code**. Finite differences call it with `Float64`. ForwardDiff calls it with `Dual{Float64}` — the same source code, compiled to different machine code by the type parameter. The adjoint caches the forward pass intermediates and reverses through them, but the forward pass itself is the same function.

One evaluator. Three gradient backends. One keyword argument to switch:

```julia
optimize_angles(params; autodiff=:finite)    # finite differences
optimize_angles(params; autodiff=:forward)   # ForwardDiff
optimize_angles(params; autodiff=:adjoint)   # manual adjoint
```

Same evaluator. Same optimizer (L-BFGS). Same convergence criteria. Different gradient backends:

| Method | $p = 5$ time | $p = 8$ time | Converges at $p \geq 4$? |
|--------|-------------|-------------|--------------------------|
| Finite diff | 91 s | — | No |
| ForwardDiff | 9.5 ms | 971 ms | Yes |
| **Adjoint** | **0.85 ms** | **81 ms** | **Yes** |

This comparison directly informed the production design. The `:auto` mode selects the adjoint when it fits in memory, and falls back to charge-based methods at extreme depths. The decision logic is three lines of code, because all three backends share the same evaluator and the same API.

The point isn't that Julia is the only language where you _can_ do this. It's that Julia is the language where this experiment took an afternoon instead of a week — because the evaluator didn't need to be rewritten, wrapped, or duplicated for each gradient strategy. When the cost of an experiment is low enough, you actually run it.

## Why Multiple Dispatch Matters

The adjoint was added as a _new function_ alongside the existing evaluator:

```julia
function basso_expectation_and_gradient(params, angles; clause_sign=1)
    cache = forward_pass(params, angles; clause_sign)
    γ_grad, β_grad = backward_pass(cache)
    (cache.value, γ_grad, β_grad)
end
```

No existing code was modified. The forward pass is identical to the evaluator used for plain function values. Multiple dispatch routes the optimizer to the adjoint when gradients are needed, and to the plain evaluator when they aren't.

This matters more than it sounds. When we later discovered the charge decomposition evaluator ([Part 6](/tags/from-saturday-to-coauthor/)), we needed a _different_ adjoint for a _different_ forward pass. Same function name — `charge_expectation_and_gradient` — different mathematics, dispatched by the type of the first argument. The optimizer doesn't know or care which evaluator is computing its gradients. It calls the same API.

This is the tagless final pattern — and we recognised it, named it, and leaned into it. The cost algebra is an interpreter. The evaluator is a program parameterised by its interpreter. Adding a new problem (MaxCut → XORSAT) means writing a new algebra, not modifying the engine. Adding a new gradient strategy means writing a new backward pass against the same forward API. The engine is closed for modification, open for extension — the open-closed principle, realised through algebraic structure rather than class hierarchies. More on this in [Part 5](/tags/from-saturday-to-coauthor/).

## The Lesson

The gradient story has a simple moral: **the best gradient method depends on the problem, and the only way to know is to test them all.** Finite differences hit a noise floor. ForwardDiff hit a cost wall. The adjoint required mathematical derivation but delivered 17× over ForwardDiff at $p = 14$.

Julia made this comparison trivial — not because Julia is "fast" (though it is), but because its type system lets you swap the numerical substrate under the same source code. `Float64`, `Dual{Float64}`, hand-derived backward pass: three completely different computational strategies, tested in one afternoon, in one binary, with one keyword argument.

Without ForwardDiff, we wouldn't have known finite differences fail at $p = 4$. Without the head-to-head comparison, we wouldn't have known the adjoint was 12× faster than ForwardDiff at $p = 8$. Without Julia's parametric types, the comparison would have taken weeks instead of hours.

The evaluator had its speedup. Now the gradients had theirs. Next came the walls — the overflow, the flat landscapes, and the scale problems that almost stopped the whole project.

---

_Next: [The Walls](/tags/from-saturday-to-coauthor/) — three problems that nearly ended the project: floating-point overflow at high arity, flat loss landscapes at high regularity, and the sheer scale of p=13 on a 50-node cluster._

_Previous: [The Fold That Changed Everything](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`src/adjoint.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/adjoint.jl) (Basso manual adjoint), [`src/tensors.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/tensors.jl) (`QAOAAngles{T}` type parameter), [`src/optimization.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/optimization.jl) (`:autodiff` keyword dispatch). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
