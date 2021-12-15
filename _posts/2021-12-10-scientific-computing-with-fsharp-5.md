---
    layout: post
    title: "Scientific Computing with F# - Post 5 : Conclusion"
    tags: [functional-programming, scientific-computing, evolutionary-algorithms, TSP, BRKGA, Ising, F#]
    author: johnazariah
    summary: This is my contribution to FsAdvent 2021
    excerpt: A series of posts outlining how we can use F# for solving some interesting scientific computing problems
---

## Conclusions

We have arrived at the end of a short series of scientific computing in F#, and we make the following observations:

* The F# code has been terse without sacrificing clarity - testifying to the expressivity of the language.
* We have, in a few hundred lines of code, written a parser for TSPLib data. Parsers are awesome when brought out as tools at the right time, as they vastly simplify comprehension and maintainability of code because they force the edge cases to the fore and require them to be handled.
* We have, in a couple hundred lines of code, written the general problem-agnostic BRKGA algorithm. F#, despite not having a dependently-typed type system, allows us to write code with particular focus on safety and future maintainability.
* In under a hundred lines of code, we wrote the encoder, decoders and fitness functions for the TSP to be solved with BRKGA. We are able to demonstrate good convergence of our result.

## Code

All the code for the project built here is [on github](https://github.com/johnazariah/tspsolver). Happy to accept PRs and bug fixes!

## Next Steps

We can actually improve the BRKGA using F# types, replacing the `encoding` and `decoding` steps of the algorithm with functions dispatched via the type itself.

We should be able to demonstrate the efficacy of such a "Typed BRKGA" with another classically hard optimization problem such as the Ising Spin-Glass.

We can study our solution further and see how close we are to optimal for those problem where an optimal solution is known.

We might consider a language that provides a stronger type system, such as Rust, and write a comparative solution.

## Notes

I've had enormous fun putting together this blog post series. There is joy in learning new concepts, and great satisfaction in writing beautiful code which runs, more often than not, the first time it compiles successfully!

I reiterate the critical role played by Dr. Helmut Katzgraber in educating, guiding and inspiring me in this pursuit of knowledge.

Other key people who have influenced my understanding of optimizing problems and the underlying physics include my colleagues Dr. Nicolas Delfosse, Dr. Ruben Andrist, Dr. Brad Lackey, and Dr. Stephen Jordan.

Please feel free to contact me if I can be part of your journey. I am happy to pay forward what others have invested in me.

Happy coding in F#!

-----

### This "Scientific Computing With F#" Series

1. [Introduction]({% link _posts/2021-12-10-scientific-computing-with-fsharp-1.md %})
1. [The Travelling Salesman Problem]({% link _posts/2021-12-10-scientific-computing-with-fsharp-2.md %})
1. [Biased Random Key Genetic Algorithm]({% link _posts/2021-12-10-scientific-computing-with-fsharp-3.md %})
1. [Solving TSP with BRKGA]({% link _posts/2021-12-10-scientific-computing-with-fsharp-4.md %})
1. [Conclusions]({% link _posts/2021-12-10-scientific-computing-with-fsharp-5.md %})
