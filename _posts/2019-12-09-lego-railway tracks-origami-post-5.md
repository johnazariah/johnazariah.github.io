---
    layout: post
    title: "Lego, Railway Tracks, and Origami - Post 5"
    tags: [functional-programming, functors, applicatives, monads, composition, F#]
    author: johnazariah
    summary: This is Post 5 in a series of posts contributing to FsAdvent 2019
    excerpt: A post about how the foundations of functional programming have neat effects on real-life programming
---


*For [Laurence Rouesnel](https://twitter.com/_laurencer), whose profound interview question about folds I failed so spectacularly that it took me 3 years to finally understand that it was about fold composition. I am grateful I got the job anyway, and got to work with you. You were a skilled and generous teacher!*

## Introduction
In the [previous post](lego-railway-tracks-origami-post-4.html), we introduced `Applicative Functor`s, and observed a symmetry between operating with unaugmented functions on unaugmented values, and augmented functions on augmented values.

In this post, we'll bring all our concepts together into an application, and see how functional programming simplifies things in the real world.

## A simple real-world problem
Let's consider the following hypothetical problem:

A space-craft is chasing down a comet, and sending back a sequence of telemetry data messages (let's call this type `TMsg`). We are tasked with writing a software library that processes the sequence of these message and derives aggregates. There are complex rules governing cases like duplicate and corrupted messages, so we use a library provided from the science division where the scientists write up the following functions whose implementations we don't get to see or change.

{% highlight fsharp %}
let add (result : TMsg) (item : TMsg) : TMsg = ... 
let inc (result : int)  (item : TMsg) : int  = ...
{% endhighlight %}

Now, using the provided `add` and `inc`, can we compute the _total_ and _count_ of an arbitrary sequence of values? 

### A simple solution

This seems pretty straightforward, and we knock out something like this in C#:
{% highlight csharp %}
    public static TMsg Sum<TMsg> (this IEnumerable<TMsg> messages)
    {
        TMsg result = default(TMsg);

        foreach (var message in messages)
        {
            result = add (result, message);
        }

        return result;
    }

    public static int Count<TMsg> (this IEnumerable<TMsg> messages)
    {
        int result = 0;

        foreach (var message in messages)
        {
            result = inc (result, message);
        }

        return result;
    }
{% endhighlight %}

### A little wrinkle

Now here's the first doozy: Can we compute the _average_ of this sequence (defined as the ratio of the `sum` to the `count` - of course the boffins in the science division have given us such a function!) **without iterating over the sequence twice**?

Hmm...this is a little ugly, but we're made of stern stuff, so we dig up the editor and source code, and come up with:
{% highlight csharp %}
    public static TAve Average<TMsg, TAve> (this IEnumerable<TMsg> messages)
    {
        TMsg sum  = default(TMsg);
        int count = 0;

        foreach (var message in messages)
        {
            sum   = add (sum, message);
            count = inc (count, message);
        }

        return div (result, count);
    }
{% endhighlight %}

OK - that was close, but we made it work. Most people won't find a sense of unease looking at this code because this is quite in line with how we write and extend code, but some things should start becoming obvious, even you don't consider them problems:

1. We needed to change the compile-time signature to account for the type of the notional `average` value.
1. We can conceive that there will be a proliferation of these functions going forward.
1. We needed to muck with the _inside_ of the for-loop, injecting the second computation of `count` adjacent to the first computation of `sum`.

### Making it...better?

Let's solve the first two concerns first, using traditional abstraction techniques.

{% highlight csharp %}
    class Aggregates 
    {
        public TMsg Sum   { get; }
        public int  Count { get; }
        public TAve Average { get; }
        public Aggregates (TMsg sum, int count, TAve average) 
        {
            Sum = sum;
            Count = count;
            Average = average;
        }
    }

    public static Aggregates BuildAggregates(this IEnumerable<TMsg> messages) 
    {
        TMsg sum  = default(TMsg);
        int count = 0;

        foreach (var message in messages)
        {
            sum   = add (sum, message);
            count = inc (count, message);
        }

        TAve average = div (result, count);

        return new Aggregates (sum, count, average);
    }
{% endhighlight %}

We now have a *single function* which we can use to build aggregates, and a *single return-type* returning all the values we computed. So at least we have limited the number of functions as requirements change. Definitely a little less ugly, but it still leaves us with the task of having to mess with the innards of this function (and the structure of the result type, but perhaps that's not a terrible problem) every time we want to extend this function.

### The end of the road

There's a rumour going around that the scientists are going to give us some kind of comparison operators, expecting us to detect a notional `max` and `min`, and then we may also have to compute some kind of standard deviation on the sequence. It dawns on us that we have no idea what the scientists will dream up next, so unless we want to keep opening up the `BuildAggregates` function and rewriting it for every new combinator, we need to think of a better solution.

Perhaps we want a function that looks something like this

{% highlight csharp %}
    public delegate TResult Combinator<TResult>(TResult result, TMsg message); 

    public static ??? BuildAggregates<???>(this IEnumerable<TMsg> messages, params Combinator<???>[] combinators) ...
{% endhighlight %}

But now the types really start getting in our way. 

- What kind of type arguments does this function take?
- How even can we pass in an array of arbitrarily typed combinators?
- What is the result type of our function?

It almost looks like we have tied ourselves up in knots going with a typed approach and maybe our only noninvasive solution will use dynamic types - sacrificing type safety for conciseness.

Is there a better way?

### A New Hope :)

This is where Functional Programming concepts start to shine!

Each of the operations `sum`, `count` and `average` is technically called a `catamorphism`. A catamorphism is a kind of `fold`ing operation that breaks down a structure into a single element. While this might be the most common `fold`ing operation you encounter, `fold`s are actually super-powerful, and we can implement many operations that you might not associate with folding with folds.

Let's look at folds in an abstract way.

## Origami Programming

A `fold` over a structure containing `item`s takes a `seed` value, an operation to combine the `seed` with an `item`, and returns the `seed` value with all the `item`s subsumed in it.

{% highlight fsharp %}
type Fold<'item, 'seed> =
    {
        Seed : 'seed
        Accumulate : 'seed -> 'item -> 'seed
    }
with
    static member Construct (s, f) = { Seed = s; Accumulate = f}
{% endhighlight %}

So `count` and `sum` over a set of integers might be written as :

{% highlight fsharp %}
let count = { Seed = 0; Accumulate = (fun seed _    -> seed + 1)    }
let sum   = { Seed = 0; Accumulate = (fun seed item -> seed + item) }
{% endhighlight %}

This structure describes an operation which will use on other structures, like lists.

For example, we could use this structure to operate on `IEnumerable<int>` as follows, and we could use `Fold` instead of `Aggregate`.

{% highlight csharp %}
    public static T Fold<T>(this IEnumerable<int> xs, Fold<T, int> fold) =>
        xs.Aggregate(fold.Seed, fold.Accumulate);
{% endhighlight %}

We can now conceivably compose two folds together to give a single fold:

{% highlight fsharp %}
type Fold<'seed, 'item> with
    static member (+) (l : Fold<_, 'item>, r : Fold<_, 'item>) =
        {
            Seed       = (l.Seed, r.Seed)
            Accumulate = (fun (ls, rs) item ->
                ((l.Accumulate ls item), (r.Accumulate rs item)))
        } 
{% endhighlight %}

It is important to note that each of the constituent folds has a different type of `'seed` - but they all have to fold over the same kind of `'item`! 

What is cool about this is that we are composing folds by tupling the seeds and creating a composite accumulator that uses the constituent accumulators and tuples the result. We can now get the sense that we can compose an arbitrary number of folds together into a single fold using this technique.

OK, so let's turn this into an applicative functor, and see how that turns out for us!

### Map & Apply

One way to map over a fold is to map over the folded result. Basically, this gives us a way to process a resultant value after the fold is run. Let's put that in:

{% highlight fsharp %}
type Fold<'seed, 'item, 'result> =
    {
        Seed : 'seed
        Accumulate : 'seed -> 'item -> 'seed
        Finish : 'seed -> 'result
    }
with
    static member Construct (s, a, f) =
        {
            Seed       = s
            Accumulate = a
            Finish     = f
        }

    static member (+) (l : Fold<_, 'item, _>, r : Fold<_, 'item, _>) =
        {
            Seed       = (l.Seed, r.Seed)
            Accumulate = (fun (ls, rs) item ->
                ((l.Accumulate ls item), (r.Accumulate rs item)))
            Finish     = (fun (ls, rs) ->
                (l.Finish ls, r.Finish rs))
        }

    member this.Map f =
        {
            Seed       = this.Seed
            Accumulate = this.Accumulate
            Finish     = this.Finish >> f
        }
{% endhighlight %}

Now, it's important to note that the `Fold` structure is actually already a representation of functions in context, so `Apply`ing a fold over another fold is simply a matter of combining the folds exactly as we have in the `(+)` case.

Go ahead and see if you can work out the type of the `(+)` function. This practice is called **equational reasoning** and is a useful skill to develop when you are working with pure, statically typed functions.

{% highlight fsharp %}
type Fold<'seed, 'item, 'result> with
    member this.Apply other = this + other
{% endhighlight %}

Let's put in some inline wrappers for good measure:

{% highlight fsharp %}
let newFold s a f = Fold<_,_,_>.Construct (s, a, f)

let (<|>) (fold : Fold<_,_,_>) f = fold.Map f
let (<*>) (l : Fold<_,_,_>) (r : Fold<_,_,_>) = l.Apply r
{% endhighlight %}

And find a way to use this over something foldable - like a `List`. 

_This is actually an annoyance in F# because have to create one of these for each type we consider "Foldable". This means you will have to write a separate one for `Seq`, `Map` and `Option` - instead of having a concise way of specifying and abstracting over all types that are Foldable - as you can with languages such as Scala and Haskell._

{% highlight fsharp %}
let foldList fold = List.fold fold.Accumulate fold.Seed >> fold.Finish
{% endhighlight %}

## An elegant and extensible solution

Let's put the whole solution in a single block so we get the beauty of the solution at one shot. I'll put some comments in the block where necessary.

You can also cut/paste the whole block into FSI to play with it...

{% highlight fsharp %}
/// The `Fold` abstraction made into an **Applicative Functor**
type Fold<'seed, 'item, 'result> =
    {
        Seed : 'seed
        Accumulate : 'seed -> 'item -> 'seed
        Finish : 'seed -> 'result
    }
with
    static member Construct (s, a, f) =
        {
            Seed       = s
            Accumulate = a
            Finish     = f
        }

    static member (+) (l : Fold<_, 'item, _>, r : Fold<_, 'item, _>) =
        {
            Seed       = (l.Seed, r.Seed)
            Accumulate = (fun (ls, rs) item ->
                ((l.Accumulate ls item), (r.Accumulate rs item)))
            Finish     = (fun (ls, rs) ->
                (l.Finish ls, r.Finish rs))
        }

    member this.Map f =
        {
            Seed       = this.Seed
            Accumulate = this.Accumulate
            Finish     = this.Finish >> f
        }

    member this.Apply other = this + other

/// Helper functions
let newFold s a f = Fold<_,_,_>.Construct (s, a, f)
let (<|>) (fold : Fold<_,_,_>) f = fold.Map f
let (<*>) (l : Fold<_,_,_>) (r : Fold<_,_,_>) = l.Apply r

/// Using the `Fold` abstraction on `List`. Create analogous functions for other foldables.
let foldList fold = List.fold fold.Accumulate fold.Seed >> fold.Finish

/// The "library" provided by the science division:
/// _Just to make everything compile, we'll define `TMsg` to be `double`, and provide implementations for `add`, `inc` and `div`_
type TMsg = double
type TAve = double
let add (result : TMsg) (item : TMsg) : TMsg = result + item
let inc (result : int)  (item : TMsg) : int  = result + 1
let div (n, d) : TAve = (float n)/(float d)

/// Our solution - which will be purely in terms of the "library" types and functions

// a summing fold
let sum = newFold 0. add id

// a counting fold
let count = newFold 0 inc id

// _et voila_, our average
let average = sum <*> count <|> div

// proof that we do a single pass for all values
[1..10000000]
|> List.map float
|> foldList average

// val it : TAve = 5000000.5

/// The science division now provides us with a couple more functions
/// It appears they read up the Naïve algorithm from https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Online_algorithm
type TSumSq = double
type TStdDev = double
let sumSq (result : TSumSq) (item : TMsg) : TSumSq = result + (item ** 2.)
let sigma ((sum : TMsg, count : int), sumSq : TSumSq) : TStdDev =
    let variance = (sumSq - (sum * sum) / float count) / (float count)
    sqrt variance

// a sumOfSquares Fold
let sumOfSquares = newFold 0. sumSq id

// and a stddev Fold from the sum, count and sumOfSquares Folds
let stdDev = sum <*> count <*> sumOfSquares <|> sigma

// now iterate over the input *ONCE* and compute everything, outputting both stddev and avg
[1..10000000]
|> List.map float
|> foldList (stdDev <*> average)

// val it : TStdDev * TAve = (2886751.346, 5000000.5)
{% endhighlight %}

### Recap
So what have we achieved? 

1. We represented an abstract `Fold` by a structure
1. We gave that structure the characteristics of an _Applicative Functor_
1. We used the mechanics afforded by the applicative functor to compose arbitrary folds directly from the provider combinators
1. We had no need to rewrite structures or wrangle type signatures
1. We computed many aggregates with a _single pass_ over the input foldable
1. We maintained complete compile-time type safety
1. _If we had more expressive types in F#, we would never write this structure more than once - and indeed, this abstraction would've been provided by the language!_

In other words, we used FP and some mathematical foundations to come up with a totally general way to compose combinator functions - something we _couldn't actually do_ with the mechanics afforded by the traditional OO programming models.

### How does it work?

While the rigorous mathematical underpinning of this approach is outside the scope of this blog post, we can try and get a handle, in loose terms, on why this approach works.

Consider what happens to each of the three members of the `Fold` type when we compose two folds together:

{% highlight fsharp %}
type Fold<'seed, 'item, 'result>
with
    static member (+) (l : Fold<_, 'item, _>, r : Fold<_, 'item, _>) =
        {
            Seed       = (l.Seed, r.Seed)
            Accumulate = (fun (ls, rs) item ->
                ((l.Accumulate ls item), (r.Accumulate rs item)))
            Finish     = (fun (ls, rs) ->
                (l.Finish ls, r.Finish rs))
        }
{% endhighlight %}

#### Seed
When we are given two folds, the seed of the composite fold needs to reflect each of the constituent folds.

In a traditional approach, we would define the composite type statically much like we did with the `Aggregates` type, and this is where things come unstuck.

What we actually need is a way to generically compose types together without losing type safety and without having to statically name the composed type, then we can compose types safely at runtime. Leaning on a formal type algebra which sits on a mathematical foundation (aka Category Theory) gives us the concept of **product types** or tuples. _Modern C# does have tuples, and so the approach we took can be implemented with the functional aspects of modern C#._

The key to figuring out how this is extensible is to note that each time we compose a fold we increase the arity of the tuple. Strictly speaking, we could do this an arbitrary number of times, resulting a super-complicated type at runtime - but the compile-time simplicity and elegance of the structure doesn't change; and the mathematical foundation ensures that the type safety is preserved the whole time.

#### Accumulate
Since we are smashing two simpler folds together into a composite one, the composite `Accumulate` function needs to work with the tupled value as its input. F# (and modern C#) allow us to destructure the tuple argument and operate on the individual components. So we create new lambda that closes over the constituent `Accumulate` functions and generates a result tuple!

#### Finish
The reasoning for the `Finish` function is analogous to the `Accumulate` function.

The important thing to see is that we can use `Type` algebra to do the composition, and `Applicative Functor` mechanics to do the composition within the `Fold` context.

## Conclusion
This has been a long post, with a lot of foundational material preceding. But I think it showcases a fairly plausible real-world situation, with real-world constraints, where the FP solution is far more elegant, safe and scalable than the alternative.

I hope this series has kindled a spark of curiosity within you, gentle reader, to dip your toes into the areas of FP and mathematically-sound reasoning.

Feel free to reach out to me on Twitter ([johnazariah](https://twitter.com/johnazariah)) or here on GitHub ([johnazariah](https://github.com/johnazariah)) if you feel I can help you take your next steps!

Happy Holidays!