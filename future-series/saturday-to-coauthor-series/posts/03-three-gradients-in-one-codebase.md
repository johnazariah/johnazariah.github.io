---
    layout: post
    title: "Three gradients in one codebase"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, automatic-differentiation, optimization]
    author: johnazariah
    summary: Part 3 of the project report. Why optimising over angles needs gradients, why three different ways of computing them ended up in the same codebase, and what Julia's type system did to keep them honest.
---

_Part 3 of From Saturday to Co-Author. [Part 2 covered the fold under the tree](/tags/from-saturday-to-coauthor/). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

By Monday evening the evaluator from [Part 2](/tags/from-saturday-to-coauthor/) computed $\tilde c(p)$ at usable depths in seconds. That was the engine for evaluating a fixed set of angles. The science of the project, though, was a maximum: for each $(k, D, p)$, what is the largest $\tilde c$ that QAOA can reach by choosing the angles well? The answer is not a single evaluation. It is an optimisation, over $2p$ real numbers, of a smooth non-convex function, and the workhorse method for that kind of optimisation needs gradients.

This post is about what it took to give it gradients. Three strategies ended up in the codebase. Two of them got us through depth 9 on Tuesday afternoon, the day after the evaluator first compiled. The third, the one that earned the rest of the project, took a sheet of paper and an afternoon.

---

## What the optimiser actually wants

The QAOA angles are a vector $(\gamma_1, \ldots, \gamma_p, \beta_1, \ldots, \beta_p) \in \mathbb{R}^{2p}$. The evaluator is a smooth function of those angles. The function has many local maxima at high depth, but it is smooth, so quasi-Newton methods are the natural fit. We used [L-BFGS](https://en.wikipedia.org/wiki/Limited-memory_BFGS): a method that approximates the local curvature of the objective from the last few gradient values, and steps in a direction that respects that curvature rather than just running straight up the slope.

L-BFGS does not need second derivatives. It does need a gradient at every point it visits, and it is unforgiving of bad ones. Give it noisy gradients and it will confidently walk off a cliff. Give it correct gradients and it converges in tens of iterations on problems where plain gradient descent takes thousands.

So: the evaluator's existence buys nothing on its own; an optimisation loop needs a *gradient function* on top of it. Three ways of producing one ended up in the codebase. They are not equivalent.

---

## The first try: finite differences

The textbook estimator is

$$
\frac{\partial \tilde c}{\partial \gamma_i} \;\approx\; \frac{\tilde c(\gamma_i + h) - \tilde c(\gamma_i - h)}{2h}.
$$

It is universal: it treats the evaluator as a black box. The whole gradient costs $4p + 1$ evaluations: one centre plus two per parameter. At $p = 14$ that is 57 evaluations per gradient call, which is more than it sounds but not the killer.

The killer is precision. The evaluator does $O(4^p)$ complex additions per call; each addition pays floating-point roundoff; the noise floor of $\tilde c$ grows with $p$. By $p = 4$, there is no step size $h$ that makes the truncation error and the roundoff error small at the same time. L-BFGS receives gradient estimates with a sign that is sometimes wrong, and stops converging. It does not warn you. It just oscillates.

Finite differences are useful as a sanity check on something else: a quick numerical second opinion on whether an analytic gradient has the right sign and rough magnitude at low $p$. As an optimisation gradient at the depths we needed, they are not an option.

This is the standard story for any sufficiently large floating-point pipeline. It is mentioned here for two reasons: because the test harness ([Part 6](/tags/from-saturday-to-coauthor/)) leans on finite differences at $p \le 3$ to validate everything else, and because finite differences are how we *measured* the noise floor, which is how we knew exact gradients were the only path forward.

---

## The second try: automatic differentiation, via a type parameter

The Julia ecosystem has several automatic-differentiation packages. The one that fit this pipeline was [ForwardDiff.jl](https://github.com/JuliaDiff/ForwardDiff.jl), which propagates *dual numbers* through arbitrary code. A dual number is a pair $a + b\,\epsilon$ where $\epsilon^2 = 0$; addition and multiplication of duals carry both the value and the derivative through any expression that knows how to handle them.

For this to work, every function in the pipeline has to be polymorphic over the numeric type of its inputs. In many languages that requires rewriting the pipeline, wrapping it in a framework, or generating dual-aware code from the original. In Julia, the move was one line.

The angles structure had been:

```julia
struct QAOAAngles
    γ::Vector{Float64}
    β::Vector{Float64}
end
```

It became:

```julia
struct QAOAAngles{T<:Real}
    γ::Vector{T}
    β::Vector{T}
end
```

That is the entire diff. Every function downstream of `QAOAAngles` was already written in terms of arithmetic operations that Julia overloads for any `Real`-like type. The compiler infers `T = Dual{Float64, N}` when ForwardDiff calls the function, and the same source code, the WHT butterfly, the constraint kernel, the complex exponentials, the reductions, compiles to a second binary that propagates the $2p$ partial derivatives alongside the value.

This is, on its own, the part of the project I find easiest to explain to people who do not write code: *the source did not change; the type changed; and now the same source produces gradients.* In a language with a less expressive type system, the same effect requires writing the differentiation logic by hand, or generating it from the original source, or running the whole computation under a tape that records every operation. In Julia, it is a parameter.

ForwardDiff produces exact gradients, correct to machine precision. The cost is the $2p$ overhead: every operation in the pipeline now propagates $2p$ partials in lockstep with the value. At $p = 5$ the gradient call is about 31× faster than the finite-difference call and, more importantly, gives an answer L-BFGS can actually descend on. At $p = 8$ the gradient call is roughly $2p$ times the forward call, which is tolerable but not free. The legacy state of the art for this method had stopped at $p = 5$. By Tuesday afternoon the evaluator was at $p = 9$ for $k = 3, D = 4$, with ForwardDiff feeding the optimiser, $\tilde c = 0.8613$, wall time under an hour on a Mac.

That number, $0.8613$, was the first one that meant anything. It was past where the published method had stopped. It was the answer ForwardDiff converged on. And it was the moment the project's *cost-of-gradient* question went from "can we afford this at all?" to "can we afford it at $p = 14$?"

ForwardDiff's cost is linear in $p$. We needed sublinear.

---

## The third try: the manual adjoint

Reverse-mode differentiation, what neural-network practitioners call backpropagation, computes the entire gradient in a single backward pass: the cost is independent of the number of parameters, and proportional to the cost of the forward pass instead. For a problem with $2p$ parameters, that is a $2p$-fold win over forward mode in principle.

Julia has reverse-mode AD packages. None of them, in March 2026, handled this pipeline cleanly: in-place WHT butterflies, complex arithmetic with a real-valued root, element-wise powers, type parameters that needed to specialise correctly. The packages either failed outright or produced incorrect gradients at $p \ge 2$.

So the adjoint was derived by hand. Two observations did most of the work.

**The WHT is its own adjoint.** The transform we used to diagonalise the constraint fold is a real, symmetric, involutive linear map: $H = H^\top$ and $H^2 = I$ (up to a scaling). The backward pass through any WHT is another WHT. The most expensive operation in the forward pass has a backward pass that is a literal re-invocation of the same butterfly with the same cost. That is rare; it is what makes hand-deriving the adjoint feasible at all.

**The mixer and constraint pieces have log-derivative shortcuts.** The mixer matrix $M(\beta)$ has entries $\cos\beta$ and $-i \sin\beta$. Its derivative is the same matrix with two entries swapped and a sign change; written as a diagonal correction in the appropriate basis, that becomes a multiplication by $-\tan\beta$ on the cosine entries and $\cot\beta$ on the sine entries. The constraint kernel's derivative with respect to $\gamma_i$ has a similar shape, and the WHT diagonalises it the same way it diagonalises the kernel itself. None of this required differentiating through the tensor contractions; the contractions only had to be replayed once.

The result is the manual Basso adjoint: the full $2p$-dimensional gradient at a cost of **about 1.6× a single forward evaluation**, independent of $p$. At $p = 8$ that is about 12× faster than ForwardDiff. At $p = 14$ the same scaling holds, and the gradient that ForwardDiff would have to evaluate by propagating 28 partials in lockstep collapses into one forward pass and one backward pass with a few extra multiplications. The gradient becomes a fixed overhead on top of the evaluator, rather than a multiplier of it.

This is the gradient that the rest of the campaign was built on. The two attempts before it produced results we could trust at low $p$; this one produced results we could afford at high $p$.

---

## Three strategies, one engine

Three gradient strategies, three different mathematical approaches, one evaluator. The evaluator was not modified to add automatic differentiation; it became polymorphic over its numeric type and that was enough. The evaluator was not modified to add the manual adjoint either; the adjoint is a new function that calls the same forward primitives and then walks back through their intermediates. The optimiser does not know or care which path is producing its gradients. It calls:

```julia
optimise_angles(params; autodiff = :finite)
optimise_angles(params; autodiff = :forward)
optimise_angles(params; autodiff = :adjoint)
```

and gets the same answer to roughly machine precision in each of the cases where all three converge, with three very different cost profiles.

This is what made the evaluator usable. Finite differences gave us a slow, low-$p$ sanity check that nothing was off by a sign. ForwardDiff gave us correct gradients in the range where its $2p$ cost was tolerable, which is where Tuesday's numbers lived. The manual adjoint gave us the budget to push depth as far as the memory would hold, which is where the rest of the series lives.

The architectural point is that the evaluator is *closed for modification but open for extension*: adding a gradient strategy did not touch the existing code, only added new code that called into it. Two posts later, this pattern is named ([Part 5](/tags/from-saturday-to-coauthor/)). Five posts later, the same pattern is used for a different forward pass entirely, with its own adjoint ([Part 7](/tags/from-saturday-to-coauthor/)).

---

## What this bought us

On Tuesday 24 March at 17:33, I sent Stephen the postscript that Part 1 already quoted: *"PS - I am expecting it to beat DQI at P=11."* The number that supported that prediction was $p = 9$ on a Mac with ForwardDiff gradients. The adjoint that would actually carry the project to $p = 11$ and beyond was finished that same day, and a reproducibility note went to Stephen that evening describing how the angles had been optimised from random seeds rather than warm-started from anyone else's published values. That email is the methodology piece that Part 5 returns to. It mattered, on Wednesday morning, more than the headline number did.

The evaluator had a fold and a Fourier transform. The optimiser now had a gradient that did not scale with the parameter count. What was left was a sequence of practical walls, none of them in the original plan, all of them in the way.

---

_Next: **The walls**, on numerical overflow at high arity, optimisation landscapes flat enough to fool four versions of plateau detection, a brief and instructive GPU dead end, and the moments where the right move was to throw work away rather than push through._

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). The type parameter lives in `src/tensors.jl`; the manual adjoint is in `src/adjoint.jl`; the autodiff dispatch is in `src/optimization.jl`._
