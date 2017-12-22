---
    layout: post
    title: "F# & Q# - A tale of two languages"
    tags: [programming-languages, functional-programming, quantum-computing, Q#, F#]
    author: johnazariah
    summary: This is a post contributing to FsAdvent 2018 and QsAdvent 2018
    excerpt: "F# and Q# are more tightly related than you might expect. Here's the story of how F# influenced and enabled the development of Q# - Microsoft's language for Quantum Computing"
---

_F# and Q# are more tightly related than you might expect. Here's the story of how F# influenced and enabled the development of Q# - Microsoft's language for Quantum Computing_

## Introduction

It's always interesting to see "real-world" uses of Functional Programming and `F#`. A lot of us are not lucky enough to use FP and/or `F#` in our day jobs, but I recently got the opportunity to work on something really cool, where `F#` and FP both made a huge impact in contributing to the quality of the outcome.

This is that story. Hopefully this will continue to encourage you to learn and use FP concepts whenever you can, so when the time comes to apply them you'll be ready!

Happy Advent(ur)ing!

## Once upon a time...

Microsoft started on its Quantum Computing effort a long time ago. 

Quantum Computing, in a nutshell, uses natural quantum mechanical phenomena to perform complex calculations. Just like we can describe the motion of a ball thrown in the air with Algebra, it turns out we can describe the behaviour of electrons and atoms using Quantum Mechanics expressed in Linear Algebra. 

Linear Algebra is both computation-intensive and space-intensive, so we are severely limited in our ability to mathematically describe complex systems such as commonly occurring molecules like hemoglobin (in blood, essential to life), or chlorophyll (in leaves, essential to photosynthesis). Indeed, the space requirements for describing such molecules requires the ability to store and process more complex numbers than there are atoms in the universe - which makes it impossible to classically describe these molecules in the traditional way. While we cannot write down the Linear Algebra equations to fully describe these molecules so we can get mathematical insight into why they have the properties they do, we are faced with the reality that _the molecules themselves exist_. That is to say, Nature has somehow found a way to "solve the equation", as it were, and stabilize the collection of atoms and electrons in such a way as to make the molecule exist.

So if, instead of struggling with trying to work out the really intractable linear algebra describing these molecules, we see these molecules more as Nature's short-circuiting the hard math problem in the first place, perhaps we can craft other difficult problems in a way that Nature can do the hard work for us even if we cannot get Nature to "show all work" on how the problem is solved. Very roughly speaking, this is how quantum computing works on a physical level.
 
This is, of course, easier said than done. The task of isolating and controlling these quantum mechanical phenomena is technically challenging and physically difficult - generally involving exotic environments that are isolated from stray radiation and chilled to within thousandths of a degree above absolute zero - and exotic materials that exhibit the appropriate properties in these environments, if only for a few thousandths of a second at a time. The task of actually building quantum hardware is highly complex and there are only a few entities investing the time and effort in doing this right now.

However, if we used a standard _classical_ computer, we might be able to democratize the study and development of quantum algorithms and optimization techniques, so Microsoft put together a _Quantum Simulator_ to assist students and researchers in this endeavor.

Microsoft's simulator was called 
[LIQUi|>](https://www.microsoft.com/en-us/research/project/language-integrated-quantum-operations-liqui/), and this was written in `F#` by [Dave Wecker](https://www.microsoft.com/en-us/research/people/wecker/). You can still go and get the tool down and simulate algorithms with it if you like, but we have something better.

This post is actually the story of how we evolved our own programming language, [`Q#`](https://docs.microsoft.com/en-us/quantum/language/?view=qsharp-preview), which enables you to develop your own quantum programs and do much more than simulation!

## The LIQUi|> Simulator

`LIQUi|>` was a pure simulation environment. Developers would write native `F#` code, and `LIQUi|>` provided library functions which performed the equivalent quantum computing operation.

A typical piece of `LIQUi|>` code might look like this:

{% highlight fsharp %}
[<LQD>]
let Entangle1 (entSize : int) =
    let ket = Ket entSize

    // Start with the full state vector
    let _   = ket.Single()

    // Get the qubit array
    let qs  = ket.Qubits

    // Put the first qubit in superposition
    H qs

    let q0  = qs.Head
    for i in 1..qs.Length - 1 do
        let q = qs.[i]
        // entangle all the other qubits
        CNOT [q0, q]
    
    // Measure all the qubits
    M >< qs
{% endhighlight %}

It is instructive to notice that the code is generally idiomatic `F#`, with all constructs expressed as native `F#`. It's also intresting to notice that the _quantum_ operations (`H`, `CNOT` and `M`) are actually side-effectful and not strictly FP-kosher! This observation is not a criticism, but something that turns out to be critically important to recognise. 

The primary learnings from this effort are:

* **`F#` is powerful enough to express quantum constructs**
* **Quantum operations seem to be primarily side-effectful**

`LIQUi|>` was wildly successful in the research circles that needed it, and a slew of important papers were published that used work that was written in `LIQUi|>`.

## Disentangling Intent

As Microsoft began to work on a physical quantum computer, the question naturally arose as to how we would program it.

The current state of the art in quantum computation indicates that quantum devices are highly specialized in the kind of computing they can perform. They are analogous to specialized graphics or encryption co-processors in that regard. Specifically, this means that they will always work in conjunction with a general-purpose classical processor which would actually run the main program and delegate the complex operations to the co-processor. 

Indeed, given the exotic environment in which the quantum devices perform, it is difficult to get other general-purpose computation components (memory, IO, storage) to work physically alongside the quantum device, so the model is even more disconnected. All parts of the program that deal with IO, storage and memory have to run as part of the classical computer, and the quantum operations have to be sequenced, optimized as much as possible, and sent down wires to the cold environment for the quantum devices to do their thing.

Since `LIQUi|>` was an unqualified success in terms of a quantum programming model for simulation, the natural approach was to find a way of take `LIQUi|>` code, extract out the quantum operations into a kind of abstraction layer, and then provide implementations of the abstraction to do simulaton and execution.

There is a really elegant solution for this in `F#`. 

`F#` provides the `[< ReflectedDefinition >]` attribute for functions. When this attribute is applied to a function, `F#` will provide the abstract syntax tree representing the function's code, and we can walk the code and perform surgery on the tree to introduce the abstraction layer and deduce the quantum operation sequence.

Here's the `Teleport` function written this way:

{% highlight fsharp %}
[<ReflectedDefinition>]
module TeleportOps = 
    let EPR (q1 : Qubit) (q2 : Qubit) =
        H q1
        CNOT q1 q2 

    let Teleport (msg : Qubit) (here : Qubit) (there : Qubit) =
        EPR here there
        CNOT msg here
        H msg
        if JM "Z" [here] = MinusOne then X there
        if JM "Z" [msg]  = MinusOne then Z there
{% endhighlight %}

The benefit of this approach, of course, is that all the existing `LIQUi|>` code would just work on whatever physical quantum computer we developed.

However, there are two drawbacks to this approach. The first, of course, is performance - there's a lot of AST wrangling that needs to be performed on each module decorated with `[< ReflectedDefinition >]`, done at run-time, which takes a lot of time. Further, the AST that has been surgically altered has to dispatch to the concrete implementations reflectively. The impact of this was very noticable as we had some test cases that would take hours to run.

More seriously, though, was the fact that the quantum operation detection code became exponientially complex once the full power of idiomatic `F#` was unleashed. `F#` is an incredibly powerful language, and when concepts like *function composition*, *partial application*, *recursion*, *computation expressions* and *point-free functions* were incorporated into a "quantum" program, the AST walkers soon became intractably harder and quickly approached the complexity of the `F#` compiler itself.

The primary learnings from this effort were: 

* **The co-processor model of quantum computing is reasonable and scalable**
* **`[< ReflectedDefinition >]` is an excellent extension point to the language**
* **`F#` is _too_ expressive**
    * The variety of idioms it supports makes it hard to scale the approach of AST tree surgery
    * Interleaved quantum and classical code makes it hard to reason about the code in the quantum domain
    * Easy to write code that was correct in `F#` but meaningless in the quantum domain
* **Performance needs to be considered from the start**

## Enter the Monad

One of the observations we made early was that the quantum operation was fundamentally side-effectful. 
In the case of `LIQUi|>` this was just a design choice, but when the abstraction layer was introduced, it quickly became apparent that the quantum state was a property of the layer that implemented the abstraction, and not a property of the program itself. (The simulator implementation had its state stored in state vectors, whilst the execution implementation would store the state in the quantum device).

So a natural progression was to use the FP concept of monadic composition, where the state of the system (and possibly the quantum state itself) was concealed inside a `quantum` monad, and the operations on the state were performed in the tightly constrained context of monadic composition.

*Of course, it actually does make sense to think about quantum computing as inherently monadic. The idea behind a monad is that there is some structure that hides details (state, control flow, whatever) in a context, and that operations in that context are sequenced by the structure so that the hidden details are never actually seen outside the structure. There is a direct analogy to quantum state and qubits here - you can never ask a qubit what its state is without altering state irreparably, but you can ask the qubit to operate on the state for you. So in a very real sense, quantum computing inherently revolves around a physically manifested Quantum State monad!*

At any rate, this immediately and vastly improved the ability to reason about the quantum code itself as the quantum code sequences were effectively contained within a monadic context, reduced the degrees of idiomatic freedom to only those supported by monadic composition, and allowed performance to sky-rocket because there was no more AST-rewriting or reflection involved.

{% highlight fsharp %}
member quantum.Teleport (msg : Qubit) (here : Qubit) (there : Qubit) = 
    quantum {
        do! quantum.EPR here there
        do! quantum.CNOT msg here
        do! quantum.H msg

        let! hereZ = quantum.JM [Z] here
        if hereZ = MinusOne then do! quantum.X there

        let! msgZ = quantum.JM [Z] msg
        if msgZ = MinusOne then do! quantum.Z there
    }
{% endhighlight %}
In fact, the 8h test completed in 330ms after applying this approach, and effectively underscored for us that the `[< ReflectedDefinition >]` approach was to be abandoned.

However, it's easy to spot the extra noise (`quantum.`, `do!`).

One further drawback was that it was difficult to do any kind of optimization on this, as the only way to use the `quantum` monad was to just run and execute the monadic chain.

## Freedom

In order for us to be able to analyse and optimize these monadic seqences, we turned to yet another FP concept - that of the `Free Monad`.

The `Free Monad` effectively provides a mechanism to implement the `Interpreter` pattern in a functional language. 

*This tool is generally not found in the toolbox of `F#` practitioners because of the mechanical work involved in writing the Free monadic interpreter every time a new use is required. Practitioners of Haskell, by contrast, get a single, pre-written interpretation of `Free` which can simply be applied because the pattern for it has been abstracted over types for general use. For this to be similar in `F#`, it would need to support Higher Kinded Types. Also, because it generally is stack-heavy in its use, a stack-less approach is necessary to run long sequences. A technique known as trampolining is generally employed for this.*

We implemented the ["Stackless Scala With Free Monads"](http://blog.higher-order.com/assets/trampolines.pdf) paper by [Runar Bjarneson](https://twitter.com/runarorama) in `F#` to give us the ability to run unbounded sequences in our `Free Monad` using trampolines. You can find a gist of our implementation [here](https://gist.github.com/johnazariah/a5785f754c978a3e12df5509dbafaf41), and more information about the interpretator pattern in this excellent post by [Scott Wlaschin](https://twitter.com/scottwlashchin) [here](https://fsharpforfunandprofit.com/posts/13-ways-of-looking-at-a-turtle-2/#way13).

By doing this, we were able to interpret the sequences twice - once for an optimization phase where we could inspect the sequences before execution, and subsequently an execution phase where the optimized sequences could be interpreted. 

{% highlight fsharp %}
member quantum.Teleport (msg : Qubit) (here : Qubit) (there : Qubit) = 
    quantum {
        do! quantum.EPR here there
        do! CNOT msg here
        do! H msg

        let! hereZ = JM [Z] here
        if hereZ = MinusOne then do! X there

        let! msgZ = JM [Z] msg
        if msgZ = MinusOne then do! Z there
    }
{% endhighlight %}

Textually, the code hasn't changed much in structure - some of the `quantum.` have gone away - but now the way in which this code is executed is drastically different from before.

The drawback at this point is that while we were able to inspect and optimize sequences _at run time_, we were still not able to do whole program analysis before running the quantum program. This meant that code-reuse and optimization were still hard to achieve.

The key learnings from this phase were:

* **It's useful to learn other functional languages and constructs even if they are not directly supported in `F#`**
* **The interpreter pattern is useful for run-time optimization, but not sufficient for whole program analysis**

## A Language is Born

At this point, what we finally realized was that all our findings pointed to creating our own language. This would give us an immense amount of flexibility in many ways:

* Permit a custom syntax 
* Permit the choice of carefully chosen set of classical and quantum control constructs
* Allow for whole program analysis
* Permit a completely independent type-system and typing rules
* Enable the auto-synthesis of functors like `adjoint` and `controlled`
* First-class tooling and syntax-analysis support.

In short, we could get Intelli-sense and red squiggly lines and all the good stuff that other languages provide us.

Logically, it made sense - a compiler is just an interpreter on steroids that runs ahead of time and generates some executable code.

So we built a language. We designed its syntax; wrote a parser, type-checker and code-generator; put in some tooling support; and even wrote a documentation generator for code written in our new language. We called the language `Q#`.

{% highlight qsharp %}
operation Teleport (msg : Qubit, here : Qubit, there : Qubit) : () 
{
    body
    {
        EPR (here, there);
        CNOT (msg, here);
        H (msg);

        If (One == M (here)) { X (there); }
        If (One == M (msg))  { Z (there); }
    }
}
{% endhighlight %}

`F#` played a critical role in this stage of the process as well. 

### Language Construct Influences

You can see that the custom syntax allows for minimal noise and high readability. We picked a familiar `C#` syntax, but the `F#` language influences are visible through the language. 

* No Classes or Objects - Functions/Operations are first class
* Higher-Order Function support
* Immutable-by-default

We are still working on other language-design ideas as the evolution of the language is still underway, and `F#`'s influence will be felt as new ideas evolve.

### `F#` in the tool-chain

When we wrote the preview version of the language, we wrote the parser entirely in [fparsec](https://github.com/stephan-tolksdorf/fparsec); the type-checker and symbol-table manipulation code in `F#`; and the code-generation library was [BrightSword.RoslynWrapper](https://github.com/johnazariah/roslyn-wrapper) - an `F#` wrapper over the Roslyn Code-Generation libraries. The documentation-generator tool was writting in `F#` as well. 

We were enormously productive - a small team was able to go from the `Free Monad` prototype to having the preview announced and shipped in about 4 months!

### `F#` as the host language

Of course, since `Q#` is only used to write the code that is to be executed on the quantum co-processor, the host language can be any high-level language that allows for interop with .NET Core. We have several demos of quantum code being hosted in C# and, of course, `F#`!

## Summary

This is really a story about how languages influence and enable each other. We all stand on someone else's shoulders, and we all learn from others successes and failures. It's fair to say that the existence of `Q#` has a great deal to do with the existence of, and familiarity with, `F#` and functional programming concepts.

Ultimately, `Q#` was evolved through struggling with real software-engineering issues - readability, maintainability, re-usability and reasonability of quantum code. The functional foundation of `F#`, along with its ease of use and expressive power, aided and guided us at every stage. Finally, the ability to use the tooling and libraries that were part of the `F#` ecosystem, pushed it across the final hurdle. 

Hopefully this story gives you some encouragement to keep learning, trying new things, and adapting every tool in the toolbox, to ultimately give you a breakthrough in your problem solving.

Happy Holidays!