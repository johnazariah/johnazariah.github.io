---
    layout: post
    title: "Lego, Railway Tracks, and Origami - Post 4"
    tags: [functional-programming, functors, applicatives, monads, composition, F#]
    author: johnazariah
    summary: This is Post 4 in a series of posts contributing to FsAdvent 2019
    excerpt: A post about how the foundations of functional programming have neat effects on real-life programming
---


## Introduction
In the [previous post](lego-railway-tracks-origami-post-3.html), we introduced `Functor`s, and observed a symmetry between operating with unaugmented functions on unaugmented and augmented values.

In this post, we'll talk about how to augment functions as well, and then operate with them on augmented values.

## Simple Addition
Let's consider addition:

We know how to do addition with unaugmented values in F#

{% highlight fsharp %}
let seven = 3 + 4
{% endhighlight %}

But what happens when we want to add augmented values? Clearly, the following will not work

{% highlight fsharp %}
let howEvenDoesThisWork = (Some 3) + (Some 4)
{% endhighlight %}

Of course, we could write some form of magic operator in `Maybe` to handle addition, but this approach has two severe limitations:

1. We would have to write a similar function for every other function that operates on unaugmented values - subtraction, multiplication and everything else. Clearly this is painful.
1. We would have to replicate this effort for every other _functor_. This is exponentially more painful, and perhaps would prevent us from embarking down the road of using these structures in the first place... (_Maybe this is why these approaches are not commonplace with other languages?_) 

Clearly we need a nicer way to apply existing functions.

Part of the reason this is a problem is because `(+)` takes two arguments. If it had just taken _one_ argument, we could use `Map` to allow `Maybe` to operate on the internally stored raw value with any old properly-typed function! Now that we have _two_ arguments, we need to support partial application, and that is going to mess things up for us. Basically, what we need is some way to "lift" a function into the `Maybe` context in a general way so we can support partial application.

In F#, partial application is the normal way to operate on functions with more than one argument. This automatically happens with anonymous lambdas but let's get explicit here so we can see what's going on 

{% highlight fsharp %}
// (+) takes two arguments
// (+) : int -> int -> int

// partially applying the first argument results in a lambda taking the remaining arguments. We'll name this lambda for clarity.
// addThree : int -> int
let addThree = 3 |> (+) 

// finally, applying the last argument results in the result
// seven : int
let seven = addThree 4
{% endhighlight %}

We have already encountered `Map`. So far, we have written it as:

{% highlight fsharp %}
// map : F<'a> -> ('a -> 'b) -> F<'b>
{% endhighlight %}

We could flip the arguments without any change to its behaviour

{% highlight fsharp %}
// map : ('a -> 'b) -> F<'a> -> F<'b>
{% endhighlight %}

If we squint a little, we can see `Map` as a function that takes a function of type `('a -> 'b)`, and returns a function of type `(F<'a> -> F<'b>)`. One might say that `Maybe.Map` **lifts** a function `f` into the `Maybe` context. The lifted function is surely partially applicable, so perhaps we only need to use `Map` for the `(+)` as well!

{% highlight fsharp %}
// addThree_m : Maybe<int -> int>
let addThree_m = (Some 3) <|> (+)
{% endhighlight %}

But, alas, this is not quite sufficient, because there's no way to apply a function of type `Maybe<int -> int>` onto the second argument. Therefore, we need to extend `Maybe` to give us this functionality.

{% highlight fsharp %}
type Maybe<'t> with
    member this.Apply<'r> (f : Maybe<'t -> 'r>) =
        match f with
        | Some op -> this.Map op
        | None -> Maybe<'r>.None
{% endhighlight %}

This function applies a value of type `Maybe<'t>` to a function of type `Maybe<'t -> 'r>`, and returns a `Maybe<'r>`. Now we have the building blocks in place to do this:

Let's give this an infix form too!

{% highlight fsharp %}
let (<*>) (mf : Maybe<'a ->'b>) (ma : Maybe<'a>) = ma.Apply mf
{% endhighlight %}

{% highlight fsharp %}
// addThree_m : Maybe<int -> int>
let addThree_m = (Some 3) <|> (+)

// seven_m : Maybe<int>
let seven_m = addThree_m <*> (Some 4)
{% endhighlight %}

which allows us to write

{% highlight fsharp %}
let seven   =       3       +            4
let seven_m = (Some 3) <|> (+) <*> (Some 4)
{% endhighlight %}

This is indeed quite elegant, although the bit of operator soup in the middle might seem somewhat irritating. 

Let's clean things up one more step by providing an operation to just lift something into the `Maybe` context without actually mapping over a value:

{% highlight fsharp %}
type Maybe<'t> with
    static member Pure (t : 't) : Maybe<'t> =
        Some t
{% endhighlight %}

We can now write something that should contrast very well indeed with our traditional function operations:

{% highlight fsharp %}
let mplus = Maybe<_>.Pure (+)

let seven   =   (+)           3            4
let seven_m = mplus <*> (Some 3) <*> (Some 4)
{% endhighlight %}

This is really very elegant indeed. It is very satisfying to get a visual similarity between partial application of unaugmented functions on unaugmented values, and partial application of augmented functions on augmented values. 

## Terminology
This structure is still a `Functor`, but the two new methods `Pure` and `Apply`, which allow for a nice way to incorporate partial application within the functor's context make it a special kind of functor called an **Applicative Functor**, or sometimes just an **Applicative**

In fact, in may usage scenarios, we may never actually use the `Map` method explicitly, because, (and this is fun to work out), 

{% highlight fsharp %}
// x <|> f = pure f <*> x
{% endhighlight %}

## Summary
We have come a long way. We started with [functions and function application, chaining and composition](lego-railway-tracks-origami-post-1.html); [types, type composition and augmentation](lego-railway-tracks-origami-post-2.html); [functors and mapping](lego-railway-tracks-origami-post-3.html); and now applicatives and (partial) application.

We had to put a fair bit of effort into arcane terminology, and do some mathematical gymnastics to get here.

Now, whilst all of this may be of academic interest, what does it have to do with real-world programming? In other words, why does knowing any of this help us in any practical way?

The [next post](lego-railway-tracks-origami-post-5.html) addresses specifically this question. Read on!