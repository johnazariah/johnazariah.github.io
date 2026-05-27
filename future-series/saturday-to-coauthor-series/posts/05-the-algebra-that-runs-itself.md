---
    layout: post
    title: "The algebra that runs itself"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, functional-programming, tagless-final]
    author: johnazariah
    summary: Part 5 of the project report. The cost algebra as a tagless final encoding, and how the same fold engine ended up running two unrelated problem families and four gradient strategies without modification.
---

_Part 5 of From Saturday to Co-Author. [Part 4 covered the walls](/tags/from-saturday-to-coauthor/). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

[Part 4](/tags/from-saturday-to-coauthor/) closed on a question it had been quietly raising for three posts: the same fold engine had absorbed three gradient strategies, two precision regimes, a swarm wrapper, fifteen $(k, D)$ instances of Max-$k$-XORSAT, and (Phase 2 would later add) a second problem family entirely. None of those additions had required modifying the engine. The natural question is why.

This is the post that answers it. It is the most programming-languages-flavoured post in the series, and it links to a pattern I have written about before; if you have read my [tagless final series](/2025/12/12/tagless-final-01-froggy-tree-house.html), you will recognise the shape immediately. If you have not, this post stands on its own.

---

## The shape of the problem

The Basso recurrence is a single mathematical object. The fold engine that walks it (from [Part 2](/tags/from-saturday-to-coauthor/)) is a single piece of code. But "the problem" has several variants:

- Max-$k$-XORSAT for various $(k, D)$. The Phase 1 paper's fifteen instances.
- MaxCut on regular graphs ($k = 2$). The Phase 2 follow-on, which is where the $p = 14$ numbers in [Part 8](/tags/from-saturday-to-coauthor/) come from.
- The same problem evaluated in `Float64`, in `Dual{Float64}` for ForwardDiff, in `Double64` for high-precision recovery.
- The same problem optimised by L-BFGS, by the swarm, by L-BFGS warm-started from swarm winners.

The naive way to handle this is `if` branches everywhere. `if problem == :maxcut then ... else if problem == :xorsat then ...`. Every new variant means touching every function in the pipeline. The fold engine, the adjoint, the test harness, the optimiser, all get conditional. This is the textbook open-closed violation, and it is how bugs get added at three in the morning when the deadline is the next afternoon.

The other way is to write the engine once, parametrise the parts that differ, and never branch on the problem type.

## The cost algebra

The parts that actually differ between problem variants are surprisingly few:

```julia
struct CostAlgebra
    k::Int           # constraint arity
    D::Int           # regularity
    clause_sign::Int # +1 for XORSAT, -1 for MaxCut
end
```

with a small set of trait methods that give the algebra meaning:

```julia
constraint_kernel(a::CostAlgebra, γ)        # the kernel the fold applies at each level
root_observable_kernel(a::CostAlgebra, angles)  # the root contraction
expectation_from_parity(a::CostAlgebra, p)  # (1 + clause_sign * p) / 2
```

The fold engine takes a `CostAlgebra` and does not branch on what it represents:

```julia
function evaluate(alg::CostAlgebra, angles, p)
    # ... fold over light-cone tree, parametrised by alg ...
    parity = root_contraction(alg, angles, ...)
    expectation_from_parity(alg, parity)
end
```

MaxCut on 3-regular graphs is `CostAlgebra(2, 3, -1)`. Max-3-XORSAT on $D = 4$ instances is `CostAlgebra(3, 4, +1)`. The fold engine, the adjoint, the optimiser, the test harness are unchanged. The compiler sees a parametrised struct and specialises the entire pipeline for the specific $(k, D, \mathrm{sign})$ at the call site.

## Where I have seen this pattern before

The functional-programming reader will already have recognised what this is. A "program" parametrised by an interpreter; the program does not know what the interpreter does, it just calls the interface; different interpreters can evaluate, pretty-print, optimise, verify. The program is written once; the interpretations are open for extension. That is [tagless final](/2025/12/12/tagless-final-01-froggy-tree-house.html).

The correspondence is exact:

| Tagless final | This codebase                              |
|---------------|--------------------------------------------|
| Program       | Light-cone tree structure (fixed by $p$)   |
| Interpreter   | `CostAlgebra`                              |
| Evaluate      | `evaluate(alg, angles, p)`                 |
| Differentiate | `evaluate_and_gradient(alg, angles, p)`    |
| Verify        | Cross-evaluator congruence tests           |
| Extend        | A new `CostAlgebra` + a test file          |

This is not an analogy. It is the same pattern applied to a different domain. The Basso recurrence is a recursive expression in an algebra; the algebra is the cost-problem definition; the interpretation is whatever the fold engine produces from a particular concrete algebra.

I did not set out to implement tagless final here. I set out to avoid copying the evaluator when a second problem family entered the picture. The pattern was the natural shape of the refactor. Recognising what the shape was, after the fact, told me which capabilities I could expect for free; that is what the rest of this post is about.

## Four capabilities, one engine

### Evaluation

The forward pass takes any `CostAlgebra`. The Phase 1 paper exercised it on fifteen Max-$k$-XORSAT instances. The Phase 2 follow-on exercised it on MaxCut at $D = 3, 4, 5$ and $p$ up to fourteen. No engine changes between the two phases. The same compiled code, with a different concrete algebra, produces results for a different problem family.

This is not a feature you appreciate until you need it. The moment Stephen and I started talking about Phase 2, the cost of starting was zero. There was no port. There was no re-validation of the fold. There was a new test file that asserted the engine returned the published Farhi value at $(k = 2, D = 3, p = 1)$, and then we were running.

### Differentiation

The three gradient strategies from [Part 3](/tags/from-saturday-to-coauthor/) (finite differences, ForwardDiff, manual adjoint) all go through the algebra. They differ in how they propagate gradients through the constraint kernel; they all consume the same algebra trait methods. Adding a fourth strategy later (the charge manual adjoint from [Part 7](/tags/from-saturday-to-coauthor/)) was structurally identical: the algebra was the input, the gradient was the output, the dispatch was a keyword argument.

The `autodiff = :finite | :forward | :adjoint | :charge_adjoint` knob from [Part 3](/tags/from-saturday-to-coauthor/) is the algebra trait in another guise. It is exactly the freedom that tagless final promises and that an `if`-tree denies.

### Validation

This is the capability that earned the rest of the project, and it is the one I want to spend the most time on.

The Basso evaluator has a published reference point: Farhi, Goldstone and Gutmann (2014) computed MaxCut on 3-regular graphs at $p = 1$ analytically. The answer is $\tilde c = \tfrac{1}{2} + \tfrac{\sqrt{3}}{9} \approx 0.6924$. We can compute that value with our fold engine in microseconds, compare to ten digits, and either we agree with Farhi or our fold is wrong.

But Max-$k$-XORSAT at finite $D$ has no published exact answer. There is no Farhi-equivalent paper to check against. How do we trust the XORSAT numbers?

The structural answer: both problems run through the *same engine*. The Basso evaluator that returns 0.6924 for `CostAlgebra(2, 3, -1)` is bit-for-bit the same compiled code that returns the XORSAT numbers for `CostAlgebra(3, 4, +1)`. If the engine were subtly wrong, both would be wrong, and the MaxCut comparison against Farhi would catch it. The trust in the engine is what is being established by the MaxCut check, and trust in the engine transfers to the XORSAT runs because the algebra is parametric.

This is the argument I made to Stephen on the evening of Tuesday 24 March, the day after the first p = 9 result. The way I phrased it then:

> *We re-optimised from scratch with independent L-BFGS multistart and random initial angles, rather than warm-starting from anyone's published values. The fact that our independently optimised values match Farhi's to three or four decimal places is in some ways a stronger validation than reproducing his angles would have been.*

That email was sent before the scoop email arrived the next morning. The reason it mattered the next day, and the reason it kept mattering for the rest of the project, is exactly what this section is about: the validation was *structural*. It was not "we got the same number on one test case." It was "the engine that gets the same number on a published case is the same engine that gets the new numbers on cases that have no published reference." That property is a property of the algebra, not of any individual run.

[Part 6](/tags/from-saturday-to-coauthor/) is what that argument looks like when codified as a test architecture. The argument in the email is the load-bearing claim; the 1875 assertions are how the claim is institutionalised.

### Extension

Adding a new problem family is a `CostAlgebra` constructor, a test file that anchors against any published reference (or that asserts agreement between two independent implementations), and nothing else. The fold engine does not change. The gradient backends do not change. The optimiser does not change. The diagnostics do not change.

In Phase 2 this turned out to matter. The same engine that had carried the Phase 1 paper went on to carry [Part 8](/tags/from-saturday-to-coauthor/)'s $p = 14$ run on a Mac, with no compromise in the test harness and no fork in the implementation.

## Why this costs nothing in Julia

A note on something I have understated. In most languages, this kind of abstraction has a runtime cost. Virtual methods, dynamic dispatch, indirection through interfaces; the abstraction is paid for at every call site.

Julia's parametric types and multiple dispatch flip this. When `evaluate(CostAlgebra(3, 4, +1), angles, 10)` is called, the compiler specialises the entire fold for $k = 3$, $D = 4$, $\mathrm{sign} = +1$ at compile time. The algebra struct is erased. The trait methods are inlined. The machine code is exactly what a hand-rolled, hardcoded Max-3-XORSAT evaluator would produce. The abstraction is at the source level. The specialisation is at the machine level. No virtual dispatch survives to runtime.

This is the design philosophy of the language paying off. It is also the reason the same pattern that would be a maintenance liability in some other languages is the cheapest piece of architecture in this codebase.

## What the algebra is not

The cost algebra did not make the code faster; the Walsh-Hadamard transform from [Part 2](/tags/from-saturday-to-coauthor/) did that. The cost algebra did not derive new mathematics; the recurrence is Basso's. The cost algebra did not add features; it took features that already existed and made them composable.

What the algebra did was let the test harness from [Part 6](/tags/from-saturday-to-coauthor/) validate the engine on a problem with known answers, with the structural consequence that the engine is then validated for problems with no known answers. That is the whole job. It is a small job, and it is the job on which everything else in the series rests.

---

The argument so far has assumed there is a test harness worth invoking. The next post is that harness, in the form it actually took: twenty-two files, 1875 assertions, seven layers, and one specific bounds check whose absence cost a day at $(k = 7, D = 8)$ from [Part 4](/tags/from-saturday-to-coauthor/).

---

_Next: **Eighteen hundred reasons**, on the seven-layer test architecture that made the numbers publishable when there was no external reference to compare against except the ones we built ourselves and forced to agree._

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). The cost algebra lives in `src/cost_algebra.jl`; the dispatch into the fold engine is in `src/basso_finite_d.jl`; the algebra-parametric tests are in `test/test_cost_algebra.jl`._
