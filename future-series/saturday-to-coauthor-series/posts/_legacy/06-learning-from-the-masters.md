# Learning From the Masters

_Part 6 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — how studying Marwaha, Wurtz, and Lykov's QOKit implementation taught us a deeper mathematical structure that removed a full factor of $p$ from the evaluator. A case study in why reading expert code is a research skill._

---

By mid-April, the evaluator was at $O(p^2 \cdot 4^p)$ and the adjoint was at $1.6\times$ forward. We had results for all fifteen $(k, D)$ pairs. But while I had managed to get p=13 on my Mac Studio, and we were labouring to get p=14/15 on Stephen's high-memory machines, our collaborators at JP Morgan Chase had gotten real numbers at p=16 with GPUs and a cluster!

By now, the paper was being written. I'd been invited to be a co-author — the collaboration with Stephen had evolved from "could you have a crack at this?" to a shared effort with the broader team, and as part of putting together the code package to accompany the paper, I started studying the other implementations in the ecosystem more carefully.

---

## The Implementation Next Door

[QOKit](https://github.com/jpmorganchase/QOKit) is an open-source QAOA toolkit published by researchers at JP Morgan Chase — Kunal Marwaha, Jonathan Wurtz, and Ruslan Lykov. It implements exact QAOA evaluation for several problem classes, including Max-$k$-XORSAT. Stephen's team had been in communication with them, and their code was one of the reference implementations for the comparison.

These are true masters in the field - unlike my n00b status as a quantum optimization researcher, these were people who had spent years solving QAOA, and I was keen to study how they got to p=16!

## The Insight We'd Missed

Our evaluator uses the _branch basis_ — a vector of $2^{2p+1}$ configurations, contracted via WHT at each of $p$ rounds. Each WHT touches the full vector, so the cost is $O(p \cdot 4^p)$ per round, times $p$ rounds: $O(p^2 \cdot 4^p)$.

The QOKit team had found a way to decompose the computation differently. Instead of working in the branch basis, they decompose the doubled density matrix into four independent _charge channels_ via the $\mathbb{Z}_2 \times \mathbb{Z}_2$ character table — a $4 \times 4$ transform that's a WHT butterfly in disguise.

In each channel, the contraction reduces to a sequence of _mode products_ — 4×4 matrix multiplications applied along successive axes of a $4^p$-element tensor. One pass through all $p$ axes costs $O(p \cdot 4^p)$. Total: $O(p \cdot 4^p)$, not $O(p^2 \cdot 4^p)$.

One fewer factor of $p$. At $p = 14$, that's a 14× speedup for the forward evaluation alone.

## The Translation

Reading the insight is one thing. Reimplementing it is another.

Their code is Python with JAX. Ours is Julia. The mathematical operations are the same; the conventions are not. I spent four days on the translation, and every bug I found was invisible at $p = 1$.

### Bug 1: C-order vs F-order reshape

Python's NumPy reshapes arrays in C-order (row-major). Julia's `reshape` uses F-order (column-major). For a 1D vector, there's no difference. For a 2D reshape at $p = 1$, there's no difference either — the vector has 4 elements, and both orders produce the same $1 \times 4$ matrix.

At $p = 2$, the vector has 16 elements, and the $4 \times 4$ reshape differs. We needed a `_reshape_c` helper:

```julia
function _reshape_c(v, dims...)
    # Reverse dims, reshape in F-order, then reverse axes
    permutedims(reshape(v, reverse(dims)...), length(dims):-1:1)
end
```

This function exists because two languages disagree about which axis is "first." It took an afternoon to find because the $p = 1$ tests all passed.

### Bug 2: transpose vs adjoint

Python's `.T` is a plain transpose. Julia's `'` is the conjugate transpose (adjoint). For real matrices, they're identical. For complex matrices — which is everything in our evaluator — they differ.

In the Phase 2 recursive trace, the QOKit code does `V @ MD[a].T`. In Julia, that's `V * transpose(MD[a])`, not `V * MD[a]'`. One operator, one character, wrong answer at $p \geq 2$ with complex entries.

### Bug 3: the $\gamma/2$ convention

The QOKit code's charge weight matrix takes the _full_ angle $\gamma$. Our API convention is $\gamma/2$ (the Basso convention). The charge primitives internally use $\gamma_s = \text{clause\_sign} \cdot \gamma / 2$. Getting this wrong produces values that are plausible, self-consistent, and completely wrong — because $\cos(\gamma)$ and $\cos(\gamma/2)$ are both smooth functions with values in $[-1, 1]$.

The only way to catch this: cross-validate against the Basso evaluator at $p \geq 2$ with non-trivial angles. Which is exactly what the [Layer 3 tests from Part 4](/tags/from-saturday-to-coauthor/) are designed to do.

## The Validation

After fixing all three bugs, the charge evaluator matched the Basso evaluator to $10^{-10}$ at every $(k, D, p)$ we tested. The moment of truth:

```
charge_val ≈ basso_val atol = 1e-10 rtol = 1e-10  # ✓ at all test points
```

Two completely independent algorithms — different data structures, different iteration orders, different mathematical decompositions — producing the same number to ten digits. That's the cross-evaluator congruence from Part 4 in action.

The forward speedup:

| $p$ | Basso $O(p^2 \cdot 4^p)$ | Charge $O(p \cdot 4^p)$ | Speedup |
|-----|--------------------------|-------------------------|---------|
| 8 | 0.06s | 0.004s | 15× |
| 10 | 4.2s | 0.16s | 26× |
| 12 | 180s | 2.3s | 78× |

For context: the QOKit team used this same charge decomposition, combined with GPU acceleration and cluster-scale compute, to push their evaluator to $p = 16$ — a depth we weren't going to reach on a single Mac Studio. Their implementation is the state of the art for raw depth. What we brought to the table was the Julia infrastructure — the test harness, the cost algebra, the cross-evaluator validation — that made the charge evaluator trustworthy enough to publish results from.

But a faster forward evaluation with finite-difference gradients still costs $(4p{+}1) \times \text{forward}$ — at $p = 14$, that's $57 \times 28\text{s} \approx 27$ minutes per gradient. To fully exploit the charge evaluator, we needed a charge adjoint.

That's [the next post](/tags/from-saturday-to-coauthor/).

## The Lesson

I want to dwell on what happened here, because it illustrates something about how computational science actually works.

We didn't derive the charge decomposition. We didn't discover the $\mathbb{Z}_2 \times \mathbb{Z}_2$ character table insight. The QOKit team — Marwaha, Wurtz, and Lykov — did that work. They published it. They open-sourced it. They wrote clean, readable code that made the mathematics accessible.

What we did was study their implementation as co-authors preparing a joint code package — understand the mathematics behind it, and reimplement it from scratch in Julia with our conventions and our test harness. The value we added was:

1. **Cross-validation** — our charge evaluator and their QOKit implementation are independent codebases that agree to $10^{-10}$
2. **Integration** — the charge evaluator plugs into the same optimizer, the same test harness, and the same cost algebra as the Basso evaluator
3. **The adjoint** — which required understanding their mathematics deeply enough to differentiate through it (next post)

This is how collaborative science is supposed to work. Different teams contribute different pieces. The QOKit team's mathematical insight made our computational infrastructure dramatically faster. Our evaluator and test harness made their insight independently verifiable. The paper is stronger because both implementations exist.

Studying a colleague's implementation is a research skill. It requires humility (they solved something you didn't), patience (their conventions are different from yours), and rigour (every assumption you carry from your own codebase is a potential bug). The reward is that you get to stand on shoulders instead of digging foundations.

Thank you to the QOKit team for making that possible.

---

_Next: [The Manual Adjoint, Manually](/tags/from-saturday-to-coauthor/) — translating 550 lines of C++ adjoint code into Julia. Three bugs, all invisible at $p = 1$, caught by 76 tests._

_Previous: [The Algebra That Runs Itself](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`src/charge.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/charge.jl) (charge decomposition evaluator), [`docs/charge-decomposition.md`](https://github.com/johnazariah/qaoa-xorsat/blob/main/docs/charge-decomposition.md) (mathematical documentation), [`test/test_charge.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/test/test_charge.jl) (cross-evaluator congruence tests). The QOKit codebase is at [github.com/jpmorganchase/QOKit](https://github.com/jpmorganchase/QOKit). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
