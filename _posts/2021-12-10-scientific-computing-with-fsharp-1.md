---
    layout: post
    title: "Scientific Computing with F# - Post 1 : Introduction"
    tags: [functional-programming, scientific-computing, evolutionary-algorithms, TSP, BRKGA, Ising, F#]
    author: johnazariah
    summary: This is my contribution to FsAdvent 2021
    excerpt: A series of posts outlining how we can use F# for solving some interesting scientific computing problems
---

_This blog series is dedicated to [Helmut Katzgraber](https://twitter.com/katzgraber), who, as Guide, Mentor and Friend, unselfishly and persistently invested time and effort to educate, inspire and encourage me to stretch my boundaries. Thank you!_

## Introduction

My stint in the Microsoft Quantum team introduced me to physicists and mathematicians, most of whom were also programmers. This lead to lots of interesting conversations around the various programming languages and approaches used for scientific computing. A lot of people preferred to use Python because of its vast library ecosystem, and several preferred to use C/C++ for performance reasons. Mostly, the discussion around functional programming was dismissed as an interesting curiosity, with just a few exceptions.

I learned a lot from those discussions, and I'm still convinced that F# and Rust are excellent choices for at least some of these problems, simply because of the expressive power of the languages, and the ability to quickly write correct and terse code.

I'm going to demonstrate the development of an evolutionary algorithmic technique and make some notes about development and runtime performance.

## Optimization is a hard, but useful, problem

Optimization algorithms show up all over the landscape of business and scientific computing - whether you look at layout of components on a silicon minimizing crossovers and connector length, or last-mile delivery of items from an Amazon delivery centre to the houses in your neighbourhood, or routing your journey over a map, avoiding real-time traffic - these are all manifestations of some kind of [NP-complete](https://en.wikipedia.org/wiki/NP-completeness) problem, solved heuristically at scale, solving a real-world business need. In many cases, the quality of the heuristic solution could have profound impacts on the company's efficiency and financial bottom-line.

Computationally, there are a lot of challenges solving NP-complete problems. At the time of this writing, we don't have a lot of knowledge about the relationship between `NP` and `P`, suffice it to say that anyone bringing a claim of either `NP = P` or `NP <> P` is viewed with extreme suspicion. The burden of proof of any such claim is justifiably very high, because a claim of `NP = P` has profound implications on computability.

What we _do_ know, however, is that all NP-complete problems are equivalently hard (for some polynomial-time factor of "equivalent"), so loosely speaking, if you can solve _one_ NP-complete problem, you can with polynomial cost, solve _any other_ NP-complete problem. This turns out to be a very useful observation, as we will see in more detail later.

## The Travelling Salesman Problem

The Travelling Salesman Problem (TSP) is a popular example of an NP-complete problem. Simply stated, given a set of cities and roads connecting all cities, find the shortest path that connects all cities just once and returns to the starting point.

_I note, with the appropriate degree of contempt, that this venerable problem has made its way to the abominable set of "leetcode" interview questions, where the ability to develop a brute-force approach to finding a solution is dubiously used as a proxy measure of coding skill._

The reality is that this problem grows super-exponentially (the complexity for `n` cities is `O(n!)`, which approximates to `O(n^n)` _and is strictly above the bound of `O(2^n)`_), and the usefulness of the problem only manifests for large `n`. Knowing the perfect solution for small `n` is not as useful as having a good solution for large `n`.

The TSP can itself model several real-world problems - the first of which is, in fact, the travelling delivery person problem: the problem of last mile package delivery by a fleet of drivers. It also can model some routing problems like component layout on chips; but because it is NP-complete, having a good solution for solving the TSP allows _other_ problems solved by reducing them to the TSP.

Therefore, it is no surprise that the TSP is a well-studied algorithm and there are several test cases and benchmarks available from the scientific community - not to mention a plethora of libraries and samples. The canonical set of TSP test cases with several variants is available as [TSPLib](http://comopt.ifi.uni-heidelberg.de/software/TSPLIB95/).

For this series, we will use the test data provided in TSPLib to benchmark our TSP solution.

## Biased Random Key Genetic Algorithms

Genetic algorithms, and evolutionary algorithms in particular, take a nature-inspired approach to detect and improve a solution over time. There are many evolutionary algorithms, using different variants and combinations of genetic operators to evolve a notionally "fitter" population from the previous generation.

We will consider the Biased Random Key Genetic Algorithm (BRKGA) as outlined in [this paper by José Gonçalves and Mauricio Resende](http://mauricio.resende.info/doc/srkga.pdf). Dr Resende is somewhat of a ground-breaking pioneer in this area, and has a lot of material available showcasing this approach and its application to various business-critical functions, and makes for a generally very good introduction to Genetic Algorithms in general and the BRKGA in particular.

Because the nature of BRKGA is that it a _general_ framework for solving any combinatorial optimization problem, we will first implement a complete general BRKGA solution in F#, and then employ it by means of appropriate encoders and decoders to solve TSP.

We will also have some discussion of _improving_ the approach of BRKGA by using types for doing the computation, and demonstrate a typed BRKGA implementation.

## The Ising On The Cake

Just like the TSP, the 2D Ising Spin Glass is another NP-complete problem with wide usefulness. A [really influential paper by Andrew Lucas](https://arxiv.org/abs/1302.5843) outlines the mapping of all of Karp's 21 NP-Complete problems to the Ising Spin Glass problem.

[Dr. Katzgraber was recently elected a fellow of the American Physical Society](https://www.amazon.science/latest-news/helmut-katzgraber-elected-fellow-of-the-american-physical-society) partially for his seminal papers in the area of spin-glasses, and it has been my privilege to be introduced to the subject by him.

We'll use the BRKGA to evolve solutions for the simpler Ising 2D model in this blog series, and have some notes about performance in comparision to a Rust implementation.

## Further Reading

There exists an enormous amount of literature about the TSP and Genetic Algorithms, and studying each of those areas in some detail may provide _many_ ways to improve upon our solution here. This blog series simply scratches the surface of this area of scientific computing, and brings to bear some interesting perspectives from the use of F# in doing so.

## The Key Takeaways

At the end of this blog series, it is my hope that I have convincingly made the following points:

1. Scientific Computing is enormous fun
1. Scientific Computing can be approached with FP, with benefits in development time and code clarity without significant loss of performance

Happy reading!

-----

### This "Scientific Computing With F#" Series

1. [Introduction]({% link _posts/2021-12-10-scientific-computing-with-fsharp-1.md %})
1. [The Travelling Salesman Problem]({% link _posts/2021-12-10-scientific-computing-with-fsharp-2.md %})
1. [Biased Random Key Genetic Algorithm]({% link _posts/2021-12-10-scientific-computing-with-fsharp-3.md %})
1. [The Ising Model]({% link _posts/2021-12-10-scientific-computing-with-fsharp-4.md %})
1. [Conclusions]({% link _posts/2021-12-10-scientific-computing-with-fsharp-5.md %})
