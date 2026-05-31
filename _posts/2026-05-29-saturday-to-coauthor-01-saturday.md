---
    layout: post
    title: "Saturday"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor]
    author: johnazariah
    summary: A sixteen-week project report dedicated to Stephen Jordan, in ten parts. This is the first.
---

_Part 1 of From Saturday to Co-Author. This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

On Saturday 21 March 2026, at 03:47, an email arrived from Stephen. By the following Saturday, he had offered me co-authorship on a paper I had walked into not knowing the first thing about. Five weeks later the paper went to arXiv. Eight weeks after that, on a Mac Studio that fits under a desk, we computed the answer to the next-hardest version of the same question.

This series is the record of what happened in between.

It is told for two reasons, in this order. First, to acknowledge Stephen, who has the rare habit of keeping conversations alive across years and continents and career changes, and who answered "I don't know any of this field" with "have a crack at it." Second, to write the technical work down in enough detail that someone could reconstruct it. There are sixteen weeks of code, debugging, dead ends, and small mercies in here. They are worth recording while they are fresh.

The series is dedicated to Stephen because none of it happens without him.

---

## The problem, in plain language

Stephen's team had a table of numbers in a paper draft. The table compared a handful of algorithms for a particular family of optimisation problems: small inputs, exact answers, no approximations. The point of the table is to settle questions that nobody else has settled, by computing the exact answers and letting the reader draw conclusions.

One column of the table was empty. The column was for an algorithm whose exact answers were known to be expensive to compute, and that nobody had pushed past a depth of five. The science of the paper turned on whether the empty column, filled in at higher depths, would beat the other columns. Five was not enough to answer the question. Eleven might be. Fourteen would settle it.

Stephen knew this would take heavy compute. His exact words:

> If one writes high performance code or finds it on some existing github repository and then runs it on a big cluster computer, it should be possible to push the direct method out to some reasonable p.

That is the project. Everything else is detail.

---

## What "we" means in this series

The work was not done alone. The "we" you will read across these posts includes:

- Stephen, whose questions, encouragement, and editorial steering shaped every part of it.
- The team at Google Quantum AI whose prior published work made the problem statable in the first place.
- Researchers at JPMorganChase, whose open-source code we later learnt from, whose answers we cross-validated against, and who appear as co-authors on the paper that came out the other side.
- AI coding assistants (Gemini, Claude, Copilot) without which this would not have shipped on the timeline it did.
- The tools: Julia, a 64GB Mac Studio, a 2016-vintage 128GB dual-Xeon workstation sans GPU, fifteen nodes of a SLURM cluster Stephen runs, and fifty dollars of Azure spend that did not pan out.

The series does not use "we" loosely. Where the work was done by a person, it says so. Where it was done with a tool or a paper or someone else's code, it says so. The point of the record is the receipt.

---

## The first week

The email arrived at 03:47 on Saturday morning, 21 March. The three papers it linked were the entry point. The first day was reading, and having conversations with Gemini and Claude to get the basic concepts of QAOA explained one question at a time. By the end of the weekend the conversation had turned into a recognition: the central computation had a functional structure to it; a *fold over a tree*; something a functional programmer would know how to exploit.

A functional problem wants a functional language. I picked Julia, because I've always wanted to find something interesting to do with it! The first Julia code was not about speed; it was about correctness. We wrote tests that reproduced known values from the published literature, and only once the tests were green did we start using the structure: separating concerns, removing redundant work, letting the fold do what folds do. The published state of the art had stopped at depth five. Inside of a day we were at depth nine on a Mac, and still running.

Tuesday evening I sent Stephen a postscript:

> PS - I am expecting it to beat DQI at P=11.

On Wednesday 25 March, at 01:51 in the morning, Stephen sent the email that, in any other story, would be the ending. His team had just spoken to researchers at JPMorganChase, who had been running the same computation on a large cluster and were already at depth seventeen.

> So it looks like you have been scooped. Sorry about that.

I replied at 10:32 that morning:

> Hehehehe / I got it running p=11 on my Mac Studio. There may be something interesting in my approach as well ... have they published? I can get you a draft this afternoon.

Stephen wrote back nine minutes later:

> Let me know what you find. Among other things, it is valuable to get independent confirmation to make sure the results are correct.

After hearing about the methodology we followed, he sent a follow-up:

> "Very convincing! Now that you have built the apparatus, the next step is to run the experiments."

That's where the "race" ended, and the collaboration began.

---

## What the contribution actually was

It is tempting, with this kind of arc, to write the eight weeks that followed as a comeback story. It was not a comeback. JPMorganChase had the bigger cluster and got the further numbers, and they appear as co-authors on the paper.

The contribution from this side was different in kind, not in scale: independent verification on commodity hardware, with open code, with cross-checks against published baseline numbers. Two computations, by two groups, on very different setups, agreeing to the precision the paper required. That is the framing the series carries from here on.

On 27 April 2026, [the paper](https://arxiv.org/pdf/2604.24633) went to arXiv. Stephen Jordan and eleven other authors. I was one of the eleven.

---

## The second phase

The paper was a finish line for the first question. It was also a starting line for the next one: a single number, in a closely related family of optimisation problems, pushed to the highest depth a 64-gigabyte machine could be persuaded to hold without crashing.

That number, as of this week, is depth fourteen on the smallest non-trivial graph: $\tilde c = 0.891384992947$, computed on the Mac Studio in eight hours and fifty-nine minutes, peak memory fifty-five and a half gigabytes. Six further values of regularity now have depth-fourteen numbers from the same Mac. It is the answer to a question that several groups have been trying to compute. I do not know of another group that has computed it at depth fourteen on a single workstation. It is also, on its face, just a number; the eight posts between this one and the one that explains it are what make it meaningful.

---

## What the rest of the series covers

The remaining nine posts are mostly self-contained. Most of them are an account of one specific technical thing that had to work for the next thing to be possible.

2. **The fold under the tree.** Recognising the central computation as a catamorphism, and finding a much faster algorithm by exposing a convolution structure that was always there in the code.
3. **Three gradients in one codebase.** Why optimisation needs gradients, and how Julia's type system let us swap between three completely different ways of computing them without rewriting the engine.
4. **The walls.** Numerical overflow, flat optimisation landscapes, a GPU dead end, and the moments it became important to throw work away rather than push through.
5. **The algebra that runs itself.** A short tour of why the same fold computes two unrelated families of problems with no engine changes.
6. **Eighteen hundred reasons.** The testing architecture that gave us confidence to publish numbers with no external reference to compare against, except the ones we computed ourselves, four different ways, that had to agree.
7. **Learning from the masters.** Studying the JPMorganChase team's published code, finding a deeper mathematical structure than we had derived on our own, and reimplementing it from first principles in Julia.
8. **Fourteen.** What it took to get the last two depths out of a Mac Studio with sixty-four gigabytes of memory.
9. **The collaborator that never sleeps.** The disciplines and techniques that made eight weeks of AI-assisted research productive, and the shape of the human contribution alongside an instrument that can write a tested module in twenty minutes.
10. **What language taught us about mathematics.** The closing reflection, with two Saturday emails a week apart.

---

The arc has dramatic ingredients. There is a scoop email. There is a comeback line that begins with "Hehehehe." There is an offer of co-authorship on a major paper, eight days after I was given the problem.

I have written this series as a project report, not as a hero story. This is a series about software engineering, creativity, perseverance, and bringing every arrow in the quiver to hit its target and make a difference. 

There is plenty in here that I find funny, and where it is funny I am happy to say so; what I have tried to avoid is dressing up the work as a personal triumph. The Saturday ask and the Saturday-a-week-later co-author offer appear in the final post. Between them are eight posts of code, mathematics, and more than the occasional stupid mistake. Stephen's name is on the dedication for a reason.

---

## Come spelunking

This trailer was the dry bit. From here we go deep-diving down the cave system: folds, gradients, walls, tests, algebras, masterclasses, manual adjoints, a stubborn Mac, the price of ~~memory~~ performance. We encounter a tireless, game-changing, collaborator; and we close with reflections.

There are equations. There is Julia. There is at least one place where I admit that my first three attempts to detect a flat optimisation landscape were all wrong. Wear a wetsuit and bring a torch.

_Next: **The fold under the tree**, on recognising the central computation as a catamorphism and finding a much faster algorithm by exposing a structure that was always there._
