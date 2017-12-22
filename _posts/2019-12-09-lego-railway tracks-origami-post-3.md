---
    layout: post
    title: "Lego, Railway Tracks, and Origami - Post 3"
    tags: [functional-programming, functors, applicatives, monads, composition, F#]
    author: johnazariah
    summary: This is Post 3 in a series of posts contributing to FsAdvent 2019
    excerpt: A post about how the foundations of functional programming have neat effects on real-life programming
---


## Introduction
In the [previous post](lego-railway-tracks-origami-post-2.html), we discussed types and their composition and augmentation.

We'll explore augmented types a little more in this post, and work out one of the simplest ways to use them to capture and reason about context.

## Maybe
Let's revisit the `Maybe` type from the last post. Here it is again:

{% highlight fsharp %}
type Maybe<'t> =
    | None
    | Some of 't
{% endhighlight %}

As we said, this is an explicit way to capture the a certain context of `'t` - namely its absence or value - so we can reason about the availability of something without knowing exactly what that something is.

We can construct values of this type as follows:

{% highlight fsharp %}
// three : Maybe<int>
let three = Some 3 // indicates the definite existence of the value 3 

// unanswered : Maybe<string>
let unanswered = Maybe<string>.None // indicates that no answer was provided
{% endhighlight %}

## Operating on Augmented Values

Consider a function that doubles an integer:

{% highlight fsharp %}
// double : int -> int
let double x = x + x 
{% endhighlight %}

We can call this function on an integer and it will work just fine :

{% highlight fsharp %}
let six = 3 |> double // six : int = 6
{% endhighlight %}

## The "Billion Dollar" Mistake
Now, consider a situation where the integer may or may not present. Let's say we got a string from a web page, which we want to first convert to an integer and then double. We now have a situation where the conversion function may fail to produce a proper integer. 

{% highlight fsharp %}
// convertFromStringFlaky : string -> int
let convertFromStringFlaky input = 
    System.Int32.Parse (input, System.Globalization.NumberStyles.Integer | System.Globalization.NumberStyles.AllowThousands)
{% endhighlight %}

A naïve approach might use exceptions to deal with failure. But of course, this means that `convertFromStringFlaky` is no longer a total function even though it claims to be. It does not transform all elements of type `string` to `int` as claimed, but rather resorts to using side-effects to encode failure. We are unable to lean on any guarantees on the function call any more - it may return an int or it may not - and this depends on the runtime value of `input`.

{% highlight fsharp %}
// this works
let six = 
    "3"
    |> convertFromStringFlaky
    |> double

// this compiles, but crashes with a null-reference exception
let ouch = 
    "three"
    |> convertFromStringFlaky
    |> double
{% endhighlight %}
Ouch.

In order to reclaim reasonability, we would need to make `convertFromStringFlaky` a total function - eliminating the side effects.

A commonly used approach with some languages is to use sentinel values like `null`.
{% highlight fsharp %}
// convertFromStringOrNull : string -> Nullable<int>
let convertFromStringOrNull input : System.Nullable<int> =
    try
        System.Int32.Parse input
        |> System.Nullable
    with
    | _ -> System.Nullable()
{% endhighlight %}

This is a total function now.

Whilst this code enables interop with other languages like C#, the calling language needs to have operators such as `??` and `?.` to handle the `null` value in a special way. This may seem like a user-friendly way to solve the problem, but deceptively so. In practice, this just moves the brittleness to the call site. Not using the prescribed null-aware operators will leave a compiling program which chokes on a null encountered at runtime. 

Unfortunately, the following snippet will not even compile in F#, because F# has no short-cuts for `Nullable` like C# does. 

{% highlight fsharp %}
// this does not compile
let thisWouldBeSix_IfItCompiled = 
    "3"
    |> convertFromStringOrNull
    |> double

// this does not compile
let thisWouldBeNull_IfItCompiled = 
    "three"
    |> convertFromStringOrNull
    |> double
{% endhighlight %}

Null checking (and the absence thereof) continue to plague software engineers and maintainers even in large, mature code-bases, so we will eschew `null` and other sentinel values and look for a better way.

This post makes a bold claim - that a much better way to deal with this situation is to use augmented types, which allow us to capture the value *and* the context (its presence or absence) and then reason about the context robustly. When we do this, we can rely on the compiler to force us to consider the possiblity that a value could be absent, and explicitly deal with this case. 

Consider the following improvement:

{% highlight fsharp %}
// convertFromString : string -> Maybe<int>
let convertFromString input = 
    try
        System.Int32.Parse input
        |> Some
    with
    | _-> None
{% endhighlight %}

This function never throws, so it either successfully extracts an integer (and stores it as a `Some`), or fails (and records it as a `None`). Of course, we have the same situation with `Maybe` as we did with `Nullable`. F# complains at compile-time that we can't call `double` on a `Maybe<int>`. Additionally, there are no "friendly" operators in C# that automatically handle the `None` case, so we are going to have to handle everything explicitly or risk a compile failure - which is a good thing.

{% highlight fsharp %}
// this does not compile
let convertedAndDoubled input = 
    input
    |> convertFromString
    |> double
{% endhighlight %}

The problem is that `convertFromString` returns an augmented value, and `double` expects an unaugmented argument. Our lego-like simplicity of composition has broken because the types don't line up any more.

## Map

But all is not lost - the unaugmented value is still present, wrapped up inside the augmented value. We now have two options:
    
1. Peel out the augmented value and extract the internal value, so that the types line up again. This has all sorts of problems. Firstly, what happens if the augmented value is a `None`? Next, the logic to deal with each condition will then need to litter all the call-sites like the `?.` approach that C# provides without the C# short-hand! Ugly!

1. Extend the augumented value with a mechanism to operate on the internal value with the desired function. This way, the smarts are baked into the augmented type, and we only apply the desired function when it makes sense to do so. This also means that _all_ such augmented types can have a common way to work with the internal value.

Consider this:

{% highlight fsharp %}
type Maybe<'t> with
    member this.Map f =
        match this with
        | None -> None
        | Some x -> Some (f x)
{% endhighlight %}

This is pretty neat:

1. Every 'container-like' augmented type can provide a `Map` function this way.

1. The implementation of this function is within the definition of the type. So when the type evolves to support new functionality, we limit the impact of the changes.

For good measure, let's create a global function that we can call with infix notation.

{% highlight fsharp %}
let (<|>) (mv : Maybe<'t>) (f : 't -> 'r) : Maybe<'r> =
    mv.Map f
{% endhighlight %}

This allows us to write code in a way that we are familiar with:

{% highlight fsharp %}
let   six =       3   |> double //   six :       int  =      6
let m_six = (Some 3) <|> double // m_six : Maybe<int> = Some 6
{% endhighlight %}

So now we only need to change our calling function slightly to get it to compile:

{% highlight fsharp %}
let convertedAndDoubled input = 
    input
    |> convertFromString
    <|> double
{% endhighlight %}

There are two important things to notice:

1. It is not accidental that the choice of the 'map' operator `<|>` optically looks like the `|>` operator. We are talking about function application in both cases, but the function application in the `<|>` case occurs with augmented types, whilst `|>` occurs with unaugmented types.

1. We can't leave the augmentation behind. With `<|>`, the transform `f` uses only the internal un-augmented value within the context of the augmentation, so as long as the transform `f` is total and has no side-effects, there is no way to fall out of the context carried by the augmentation. This is a good thing, as we will see as we support more operations.

To drive home the similarities, let's consider another case of chained application :

{% highlight fsharp %}
let isTrue   =      3  |> double  |> square  |> isEven //      true
let m_isTrue = Some 3 <|> double <|> square <|> isEven // Some true
{% endhighlight %}

## Terminology

Any augmented type `F` which offers a `Map` function with the following properties is called a **Functor**:

1. The signature matches `map : ('a -> 'b) -> F<'a> -> F<'b>` (or with swapped arguments)
1. Satisfies "Identity Law". Given `id x = x`, then `map id = id`
1. Satisfies "Distribution". `map (f >> g) = map f >> map g`

_The two laws are expressed in 'point-free style', where the augmented value argument to `map` is implicit on both sides of the equality._

## Summary
This post outlined the use of augmented types to capture and reason about context independent of the actual values. This allowed us to implement the generic `Map` functionality on the specific case of `Maybe`. 

We further observed that such a map function allows for chained application of unaugmented functions to augmented values, very analogous to chained applications of unaugmented functions to unaugmented values. 

In the [next post](lego-railway-tracks-origami-post-4.html), we'll talk about another structure which allows us to apply an _augmented_ function to augmented values.