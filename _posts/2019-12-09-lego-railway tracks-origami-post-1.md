---
    layout: post
    title: "Lego, Railway Tracks, and Origami - Post 1"
    tags: [functional-programming, functors, applicatives, monads, composition, F#]
    author: johnazariah
    summary: This is Post 1 in a series of posts contributing to FsAdvent 2019
    excerpt: A post about how the foundations of functional programming have neat effects on real-life programming
---

_For [Jacob Stanley](https://twitter.com/jacobstanley), who introduced me to wonderful insights and neat ideas in every conversation we had without making me feel stupid. Thank you!_

## Introduction
Functional Programming (FP) can sometimes be seen as a bit of a black art.

To most people on the programming spectrum - from students to seasoned enterprise developers, and across a wide variety of popular programming languages - from C++; C# and Java; to Python and JavaScript, the language of functional programmers seems unfamiliar, unnecessarily strange, and maybe even a little intimidating. 

This short series of posts helps to demystify some of these concepts, shows some neat symmetries and patterns, and provides a real-life practical example where FP indeed makes life easier. 

My hope is that at least one person will be inspired to take their first steps digging into FP after reading this series. 

Happy Reading!

## Functions as mappings
Most programming languages support functions as a kind of sub-routine returning a computed value. This is a useful introductory way of getting some intuition around them, but functional programmers take things a step further - they lean on the mathematical definition of functions, and aim to derive some benefits from building on this foundation.

From a mathematical perspective, a function is nothing more than a mapping between an input set and an output set. That is, the function represents a transformation for every element in an input set to some element in an output set.

This may seem an overly restrictive definition, but let's explore what that really means:

1. Each element of the input set is mapped to _a single_ element in the output set. This, for example, implies that once we have computed the output of a given input, we would never need to do the computation again - we could just store the (input, output) pair and refer to that instead. This can have profound implications in how we write our programs - but that is really another story for another day.
1. The function does nothing more than transform an input element to an output. This means _no side-effects_ can take place in the function - no missile-launches, no calls to the internet, and, if we're being super strict, no printing to the console. Many languages relax this rule for convenience sake, but some don't. The ones that don't allow us to write code that is significantly easier to reason about.
1. Because there are no side-effects, the size of the mapping between a finite sized input set and a finite sized output set becomes definitely finite. It could still be a large number but it is definitely finite. Again, this can have profound implications, but it is a story for another day.

I'll be using `F#` to demonstrate these concepts in this series of posts. F# does relax the _no side-effect_ rule, but we'll just be strict by convention and not put in side-effects to illustrate the benefits.

Here's how you define a function in F#. This function computes whether a given `byte` is even.

{% highlight fsharp %}
// isEven : byte -> bool
let isEven x = (x % 2uy = 0uy)
{% endhighlight %}

The `Type Signature` of the function is: `isEven : byte -> bool`. Read it as "isEven is a function that takes a `byte` and returns a `bool`"

As you can see, there are no side-effects in this function - so with sufficiently large memory (_how much memory, exactly?_), we could replace this function with a look-up table!

## Function Application
To extract a result from a function, we need to _apply_ it to an input value. This input value is traditionally called an "argument".

In F#, we can do this multiple ways. The first form might be more familiar to programmers of other langugages

{% highlight fsharp %}
isEven (23) // evaluates to 'false'
{% endhighlight %}

Of course, the parantheses are optional in F#, so this is equivalent (and preferable)

{% highlight fsharp %}
isEven 23 // evaluates to 'false'
{% endhighlight %}

However, F# has a more idiomatic way of "giving" an argument to a function. This is more than an intellectual curiosity, and it's useful to learn this F# idiom.

{% highlight fsharp %}
23 |> isEven // evaluates to 'false'
{% endhighlight %}

You can read this as "`23` given to `isEven`"

The reason this is idiomatic in F# is that it provides for an elegant way to chain function applications:

Traditionally, this is done "maths-style" where you have read right-to left:

{% highlight fsharp %}
let square x = 
    x * x

let squareIsEvenTraditional x =
    isEven (square x)
{% endhighlight %}

Written idiomatically, this reads more naturally (left-to-right and top-to-bottom) than the traditional way to chain applications.

{% highlight fsharp %}
let squareIsEven x =
    x
    |> square
    |> isEven
{% endhighlight %}

Apart from readability, this way of "feeding" values to a transform leads to some neat symmetries, as we will see in the future.

## Function Composition
If we continue with the perspective that a function is a transform between elements, then it follows that we can compose transforms together to make new transforms. When we compose two transforms, we get a single transform that goes from the input set of the first transform to the output set of the second - eliminating the intermediate set from the equation.

In F#, we can write

{% highlight fsharp %}
let first  : 'input -> 'intermediate = ... 
// "`first` takes an `'input` to an `'intermediate`

let second : 'intermediate -> 'output = ... 
// "`second` takes an `'intermediate` to an `'output`

// composed : 'input -> 'output 
let composed = first >> second 
// `composed` takes an `'input` to an `'output`
{% endhighlight %}

The `>>` operator composes the functions in the given order and returns the composite function.

Let's explore the relationship between `|>` and `>>` a little more. These expressions both evaluate to the same value:

{% highlight fsharp %}
// chained function application
let thisIsTrue = 
    2
    |> square
    |> isEven

// composed function
let squareIsEvenComposed = 
    square
    >> isEven

let thisIsAlsoTrue =
    2
    |> squareIsEvenComposed
{% endhighlight %}

Indeed, mathematically they are operationally equivalent, but the ability for us to compose functions means that we can compute and cache (and reason about) the composite function as a single entity.

## Higher-Order Functions
Let's look at composition again and figure out how to implement the composition operator:

{% highlight fsharp %}
let compose first second =
    (fun x -> x |> first |> second)

// let's use F# syntax to give this a friendly inline name
let (>>) = compose
{% endhighlight %}
Let's take a closer look at the `(>>)` function. It's a transform between elements of an input set and an element of an output set. But these sets aren't `int`s or `byte`s, as we have traditionally encountered. Hover over the `(>>)` function and you'll see that the type signature is a tad more complex:

{% highlight fsharp %}
// (>>) : ('a -> 'b) -> ('b -> 'c) -> ('a -> 'c)
{% endhighlight %}

The way to read that is "compose is a function that takes a function of type `('a -> 'b)`, and a function of type `('b -> 'c)`, and returns a function of type `('a -> 'c)`.

The first observation we can make is that there are sets of _function_, just like we have sets of _int_ and _byte_.

Specifically, there is a set of functions that take `'a` to `'b`, which is denoted as the set of `('a -> 'b)`. Similarly, there are the sets of functions denoted `('b -> 'c)` and `('a -> 'c)`.

Then `(>>)` is a transform that takes one member each from `('a -> 'b)` and `('b -> 'c)`, and maps that pair to a member of `('a -> 'c)`. 

It's almost like stacking lego-blocks, so that you get a single block with twice the height. Of course, these blocks stack as high as you want, as long as you match up the output set with the input set of the next level.

The next observation is that we can implement `>>` in terms of `|>`. What this means is that chained application can be abstracted into function composition.

## Summary
We have introduced a minimalistic, mathematically-founded intuition for how to view functions, and we have explored function application, function chaining, and function composition. 

We have also introduced idiomatic F# constructs for each of these operations, and it is worth getting familiar with these idioms because patterns start emerging once we start playing with different input and output sets.

In the  [next post](lego-railway-tracks-origami-post-2.html), we'll talk about these sets and how important they are to programming.