---
    layout: post
    title: "What language taught us about mathematics"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, programming-languages, reflection]
    author: johnazariah
    summary: Part 10 of the project report. The two bookend emails, the ten innovations and where they came from, and the case that programming languages are research tools, not just implementation tools.
---

_Part 10 of From Saturday to Co-Author. [Part 9 covered the AI collaboration honestly](/2026/06/25/saturday-to-coauthor-09-the-collaborator-that-never-sleeps.html). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

This is the last post in the series. I want to set the technical work to one side for it, and tell the rest of the story plainly.

The series opened with a Saturday morning. It closes with a Saturday morning a week later. In between are eight posts of code, mathematics, walls, tests, algebras, manual adjoints, a stubborn Mac, and a small permanent piece of infrastructure I would do differently if I were starting over.

This post is what the eight weeks were really about, through the two emails that mark the beginning and the end.

---

## Saturday, 21 March, 03:47

> *The problem I have in mind...*

The email arrived in the early hours of a Saturday. The problem was specific: implement an exact evaluator for finite-depth QAOA on Max-$k$-XORSAT and on regular MaxCut, sweep $(k, D)$ at various depths, compare against the published classical baselines, see whether QAOA beats them and where. The baseline table at the bottom of the message had the numbers we would be trying to beat.

I had spent years on the Microsoft Quantum team as a language designer and software architect; Q# is the quantum-programming language I co-invented. So I knew enough about quantum computing to recognise the words in Stephen's email and most of the symbols on the page. What I had not done was *quantum optimisation*: running quantum algorithms to extract numerical answers from concrete combinatorial problems. QAOA was a name I had encountered, not an algorithm I had worked with; the Max-$k$-XORSAT and MaxCut analyses, and the analytical machinery for evaluating QAOA exactly at finite depth, were new ground. I read the three papers Stephen referenced over the rest of the day. By the evening I had recognised the central computation as a fold over a tree.

That recognition is the seed of everything else in the series.

## Saturday, 28 March, 08:06

> *I think the most likely course of action is to weave your numbers into the attached paper and include you as a coauthor... Your results are in some ways more helpful to me because you have shared your code and data and documented what you have done. Also, the sanity checks against Eddie's published numbers are very reassuring.*

Eight days later, on the next Saturday morning, Stephen's reply to the draft I had sent the previous afternoon offered me a co-author position on the paper his team was preparing. The relevant beats in the message were not the offer; they were the reasons. The shared code mattered. The shared data mattered. The methodology document mattered. The sanity checks against published reference values mattered.

The decision was structural. The work that had been done was the kind of work that could be brought into a published paper because it could be inspected, reproduced and trusted, independently of whether I was a known quantity to the broader collaboration; and that was the case because of the algebra from [Part 5](/2026/06/11/saturday-to-coauthor-05-the-algebra-that-runs-itself.html), the test architecture from [Part 6](/2026/06/15/saturday-to-coauthor-06-eighteen-hundred-reasons.html), and the documentation discipline from [Part 9](/2026/06/25/saturday-to-coauthor-09-the-collaborator-that-never-sleeps.html).

The two emails above are, by themselves, the entire arc this series is reporting. The middle eight posts are how the second email followed from the first.

---

## The ten innovations, and where they came from

Each piece of mathematics or engineering in the project came from one of four sources. Tagging them this way is the closest thing I have to a thesis for the series:

| # | Innovation                                            | Source                                  |
|---|-------------------------------------------------------|-----------------------------------------|
| 1 | Catamorphism recognition (Sat 21 Mar)                 | Code clarity (the fold made it visible) |
| 2 | Walsh-Hadamard factorisation (Sun–Mon)                | Code clarity (refactor exposed XOR)     |
| 3 | Type-parametric angles (`QAOAAngles{T<:Real}`) (Tue)  | Julia's type system                     |
| 4 | Manual Basso adjoint (Tue 24 Mar)                     | Mathematics on paper                    |
| 5 | Threshold normalisation $10^{30}$ (Fri 4 Apr)         | Engineering against failure modes       |
| 6 | Plateau detection v4 (circular buffer) (Tue 31 Mar)   | Borrowing (Helmut Katzgraber's lineage) |
| 7 | Multi-machine orchestration (early Apr)               | Engineering                             |
| 8 | `Double64` precision via the same type parameter (Fri 10 Apr) | Julia's type system             |
| 9 | $\mathbb{Z}_2 \times \mathbb{Z}_2$ charge decomposition (post-paper) | Borrowing (JPM team, via QOKit)  |
| 10 | Charge manual adjoint, with instrumented forward (post-paper) | Mathematics on paper + Julia's type system |

Some of these are mine. Some belong to other people who built the foundations I stood on. The case I want to make in this post is that the language we built in is the reason the borrowed pieces composed with the homegrown ones at all.

### What came from code clarity

The factorisation in [Part 2](/2026/06/01/saturday-to-coauthor-02-the-fold-under-the-tree.html) was not the result of a derivation. It was the result of *reading my own code*. When the kernel was a separate function from the fold, and the fold was a separate function from the recurrence, the XOR convolution structure was visible in two lines. In a language where the kernel was inlined and the fold was a nested loop, the structure would have been there in principle and invisible in practice. Julia did not derive the Walsh-Hadamard factorisation; I did. But Julia made the structure of the code clear enough that I could.

The same thing is true of the threshold normalisation. The reason I knew where to put the threshold was that the overflow point was a specific named function on a specific named tensor at a specific named level. Visible structure makes for visible diagnoses.

The cost algebra of [Part 5](/2026/06/11/saturday-to-coauthor-05-the-algebra-that-runs-itself.html) is the same lesson at the architectural scale. It is hard to see the difference between an `if`-tree and an open extension if the language gives them similar surface syntax; it is easy when the cost of the abstraction is zero and the cost of the `if`-tree shows up at every call site.

### What came from Julia's type system

Three pieces of the work, two weeks apart, used the same parametric-type trick to absorb fundamentally different machinery. The angles struct parametrised by `T <: Real` lets `T` be `Float64` for production, `Dual{Float64}` for ForwardDiff, and `Double64` for high-precision recovery. The same one-line change ran in three eras of the project.

This is the part of the language whose presence I would not have noticed if it were absent. In Python you would write three pipelines or a framework. In C++ you would write template machinery. In Julia it is a sentence.

I am not making a parochial case. I have worked in five language families and I am not going to claim Julia is uniformly better for everything. I am claiming, narrowly, that for the shape of work this project was, the language was a partner in the work rather than a venue for it.

### What came from borrowing

The swarm optimiser came from Helmut Katzgraber. Five years before this project, he taught me about population-based search in rugged landscapes; the design of the swarm in this codebase descends from his BRKGA lineage. The charge decomposition came from the JPM team (Abid Khan, Ruslan Shaydulin, and Sami Boulebnane on the joint paper), who derived it, published it, and shipped it as part of QOKit, their open-source toolkit, clean enough that an outsider could learn the mathematics from the code.

I want to say something I think is sometimes neglected in computational science write-ups. The interesting question is not whether a piece of work is original. The interesting question is whether the work *acknowledges its sources*, and whether the new contribution is properly distinguished from what was borrowed. The JPM team's charge decomposition made [Part 8](/2026/06/22/saturday-to-coauthor-08-fourteen.html)'s headline numbers possible. My implementation in Julia made it accessible from inside a different test harness and a different algebra. Both are real contributions; neither is the same contribution.

### What came from engineering

Plateau detection v4, the multi-machine orchestration, the threshold rule, the five memory fixes from [Part 8](/2026/06/22/saturday-to-coauthor-08-fourteen.html), the diagnostics module. None of these is mathematics. All of them are the difference between the work producing numbers and the work producing a missing process and a shrug. The full case for engineering as a first-class research contribution is one of the standalone sections below.

---

## Three sentences

If the eight weeks of work have a thesis about programming languages, it is the following three sentences. The first I have used in this series before, at the end of [Part 2](/2026/06/01/saturday-to-coauthor-02-the-fold-under-the-tree.html). The second is its companion at the operational scale. The third is the one that did not exist five years ago.

**The language you think in shapes the theorems you can see.** The recognition of the Basso recurrence as a catamorphism, and of the constraint kernel as an XOR convolution, did not require the language. They required the language to put the structure where I could see it. In a less expressive language I would have written equivalent code that hid the structure under indexing arithmetic, and the structure might have stayed hidden.

**The language you write in shapes the experiments you can run.** The three-way gradient comparison from [Part 3](/2026/06/04/saturday-to-coauthor-03-three-gradients-in-one-codebase.html) was cheap because the language made it cheap. The `Double64` fix in [Part 4](/2026/06/08/saturday-to-coauthor-04-the-walls.html) was cheap because the same parametric type carried it. The cross-evaluator congruence in [Part 6](/2026/06/15/saturday-to-coauthor-06-eighteen-hundred-reasons.html) is cheap because both evaluators are parametrised over the same algebra. When experiments are cheap, you run them. When you run them, you learn things you would not have predicted.

**The language you write in shapes how efficient the LLM is.** This is the claim [Part 9](/2026/06/25/saturday-to-coauthor-09-the-collaborator-that-never-sleeps.html) made in detail: when a substantial fraction of the code is generated by an LLM, the language's value is measured in compiler feedback per generation cycle. Strongly typed, immutable-by-default, functional-leaning languages compress the LLM's feedback loop; weakly typed, mutable-by-default languages stretch it. The calculus on language choice has shifted, because the producer of the code is shifting. Julia sits on the right side of that line; F#, Haskell, OCaml, and Rust sit beside it. The work in this series moved at the pace it did partly because the language and the new producer agreed about what *correct* looks like.

Three claims, three scales: cognitive, operational, conversational. Each is a different shape of the same observation. The language is part of the work; the work changes when the language changes; and what counts as "the right language" changes when who is writing changes.

---

## Engineering is research

The boundary between "scientific contribution" and "engineering work" does not exist in projects like this. The plateau detector, the threshold rule, the five memory fixes from [Part 8](/2026/06/22/saturday-to-coauthor-08-fourteen.html), the diagnostics module: every one of these is engineering. Every one of them is also the reason a number in the paper means what it means. Treating engineering as separate from the research is a category error of the publication system, not of the work.

Computational science is research *because of* the engineering, in exact proportion to how much of its claim depends on what the code did. A result that cannot be reproduced because the infrastructure was not described is a result that has not been published; it has only been announced. The discipline that closes that gap is, in plain English, engineering. The community knows this. The journal system does not always reward it. A blog series is one place where it can be written down without varnish.

---

## The numbers, defended

For a final score-keeping in plain English, with FACTS-defendable numbers only:

- **Catamorphism recognition** (Sat 21 Mar). The branch-tensor recurrence is a fold over the light-cone tree, parametrised by an algebra. This is the first piece of mathematical insight; everything else in the series rests on it.
- **Walsh-Hadamard factorisation** (Mar 22–23). $O(4^{3p}) \to O(p^2 \cdot 4^p)$. At $k = 3, p = 8$: roughly 65 000 times fewer operations, and a wall-clock that went from "did not complete" to about eleven minutes. Both numbers are the right answer to different questions about the same change.
- **ForwardDiff via a type parameter** (Tue 24 Mar). The only gradient method that converged in the regime where finite differences fell below their noise floor; 31× faster than FD at $p = 5$.
- **Manual Basso adjoint** (Tue 24 Mar). About $1.6\times$ a single forward evaluation, independent of $p$. At $p = 8$, about twelve times faster than ForwardDiff.
- **Plateau detection v4** (late March). Cut the $p = 12, (3, 4)$ run from over two hours to about forty minutes for the same converged value $\tilde c \approx 0.877$.
- **Charge decomposition** (post-paper, from the JPM team, via QOKit). Strips a factor of $p$ from the forward cost.
- **Charge manual adjoint** (post-paper). About $4.5\times$ a single forward evaluation, independent of $p$.
- **Test harness.** 22 files, 1875 assertions, seven layers.
- **Phase 1, XORSAT campaign** (15 Mar – 27 Apr): fifteen $(k, D)$ instances; thirteen of fifteen beat the strongest classical comparison; arXiv submission on 27 April.

The Phase 2 campaign deserves a table of its own:

| $D$ | max $p$ | $\tilde c$ | wall | peak RSS | DQI bound | gap |
|-----|-----|-----|-----|-----|-----|-----|
| 3 | 14 | 0.891385 | 8.98 h | 55.65 GB | 0.85355 | +0.038 |
| 4 | 14 | 0.831515 | 5.27 h | 56.51 GB | 0.78868 | +0.043 |
| 5 | 14 | 0.801254 | 6.25 h | 56.39 GB | 0.75000 | +0.051 |
| 6 | 14 | 0.771627 | 4.96 h | 54.95 GB | 0.72361 | +0.048 |
| 7 | 14 | 0.752437 | 5.79 h | 55.05 GB | 0.70412 | +0.048 |
| 8 | 14 | 0.735341 | 4.02 h | 56.50 GB | 0.68898   | +0.046 |
| 9 | 12 | 0.717764 | 2.70 h | n/a      | 0.67678   | +0.041 |

Exact finite-depth QAOA expectations for $k = 2$ MaxCut on $D$-regular infinite-graph instances, all computed on a Mac Studio M4 (64 GB unified memory, Julia 1.12.5), in the campaign that began the day the Phase 1 paper was submitted. The six $p = 14$ rows account for about 35 hours of total wall clock. The DQI upper bound is $\tfrac{1}{2} + \tfrac{1}{2\sqrt{D-1}}$, the strongest published classical comparison on this family; the QAOA value clears it at every $D$ in the table, by between 0.04 and 0.05. At $D = 3$ the value additionally clears the Goemans-Williamson worst-case guarantee of $\approx 0.8786$. The $D = 9$ row is shown at its current best depth ($p = 12$); the Mac is still working towards $p = 14$ for it, and the table will update when that run lands.

I am stating these because they are defensible and because someone can check them. They are not stated as a victory lap. The point is that the work survives inspection.

---

## Two stories, one dedication

There are two arcs through this series.

The technical arc, whose protagonist is the problem: the problem went from "I do not know what QAOA is" on a Saturday morning, to a complete fifteen-instance sweep with an arXiv submission five weeks later, to depth fourteen on a Mac eight weeks after that. The problem bowed. That is the headline.

The interpersonal arc, whose protagonist is Stephen: a person extended a hand to someone outside his usual collaboration network on a Saturday morning, watched what came back, extended a co-author invitation a week later, and then continued to participate in the work for the rest of the project on equal terms. That is the dedication.

The way the technical arc happened the way it did is inseparable from how the interpersonal arc was conducted. The methodology document I sent on the Tuesday evening, the validation argument it made, the willingness to share code and tests, the AI involvement named openly on day two, the explicit acknowledgement of the JPM team's prior work: none of these are technical pieces. All of them mattered for whether the technical work landed where it did.

This is why the series is dedicated to Stephen. The technical work is mine, the JPM team's, the assistant's, Helmut's, and the people who built the Julia language and its ecosystem. The shape of the project is Stephen's, because the way he conducted the collaboration is what made the rest of it possible.

## Thank You

To **Stephen Jordan**, for the problem, the patience, and the eight days. The dedication on this series is the most concrete thank-you I have available.

To the **JPM team** (**Abid Khan, Ruslan Shaydulin, and Sami Boulebnane**), for the charge decomposition, for QOKit (the open-source toolkit through which the mathematics was learnable from code), and for engaging directly with this work as co-authors on the joint paper. To Abid in particular for the verifier repo.

To **Helmut Katzgraber**, for teaching me about swarms in rugged landscapes five years before I needed them.

To **Jairam Manjunathiah**, for the constant encouragement over these, lo, 4 decades - and for making the Mac Studio available for my work. I wish I'd splurged on the 128GB machine, but then this blog series would probably be considerably shorter! :)

To the **AI assistants** that worked alongside me, whose contribution is in the previous post and not glossed here.

To **Julia and its ecosystem**, for being expressive enough that the mathematics had nowhere to hide.

---

## The series

> 1. [Saturday](/2026/05/29/saturday-to-coauthor-01-saturday.html)
> 2. [The fold under the tree](/2026/06/01/saturday-to-coauthor-02-the-fold-under-the-tree.html)
> 3. [Three gradients in one codebase](/2026/06/04/saturday-to-coauthor-03-three-gradients-in-one-codebase.html)
> 4. [The walls](/2026/06/08/saturday-to-coauthor-04-the-walls.html)
> 5. [The algebra that runs itself](/2026/06/11/saturday-to-coauthor-05-the-algebra-that-runs-itself.html)
> 6. [Eighteen hundred reasons](/2026/06/15/saturday-to-coauthor-06-eighteen-hundred-reasons.html)
> 7. [Learning from the masters](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html)
> 8. [Fourteen](/2026/06/22/saturday-to-coauthor-08-fourteen.html)
> 9. [The collaborator that never sleeps](/2026/06/25/saturday-to-coauthor-09-the-collaborator-that-never-sleeps.html)
> 10. What language taught us about mathematics

[Part 1](/2026/05/29/saturday-to-coauthor-01-saturday.html) ended on "wear a wetsuit and bring a torch". This last post does not need either. The torch has done its work. The cave system is mapped.

Thank you for coming spelunking.

---

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). 1875 tests passing. The Phase 1 paper is on arXiv (submitted 27 April 2026). The Phase 2 $p = 14$ results are on branch `feature/charge-adjoint-memory-fix`, commit `74cf598`._
