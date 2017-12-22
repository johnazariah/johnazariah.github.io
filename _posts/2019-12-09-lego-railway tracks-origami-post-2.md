---
    layout: post
    title: "Lego, Railway Tracks, and Origami - Post 2"
    tags: [functional-programming, functors, applicatives, monads, composition, F#]
    author: johnazariah
    summary: This is Post 2 in a series of posts contributing to FsAdvent 2019
    excerpt: A post about how the foundations of functional programming have neat effects on real-life programming
---


## Introduction
In the [previous post](lego-railway-tracks-origami-post-1.html), we discussed functions, function application and function composition. We talked about functions as transforms between elements of sets.

We'll talk a little more about those sets in this post, and outline special kinds of sets we can use.


## Types
As mentioned, we have been talking about functions as transforms between elements of sets. In some programming languages, these sets have a special name - we call them `Type`s. 

Most programming languages have **simple** types - like `int`, `char`, `byte`, and, depending on the language, even `string`. 

Most of these languages also allow you to define your own type as a composition of other types. 

The most common way to compose types to form a new type is to make a **record** type. A record type has multiple members, each of which has an associated value, and the record is effectively a grouped set of constituent values. 

Here are some record types in F#:

{% highlight fsharp %}
type PrimeLens = 
    {
        FocalLength : UInt16
        MaxAperture : float
    }

type ZoomLens =
    {
        MinFocalLength : UInt16
        MaxFocalLength : UInt16        
        MinAperture : float
        MaxAperture : float
    }
{% endhighlight %}

Some languages, like F#, allow you to compose types into a new type called a **choice** or **sum** type. These encode a set of choices, and value of a choice type is one of those choice values. 

Here is an example of a choice type in F#:

{% highlight fsharp %}
type LensType =
    | Prime of PrimeLens
    | Zoom of ZoomLens
{% endhighlight %}

A value of type `LensType` type is either a value of type `PrimeLens` called `Prime`, or a value of type `ZoomLens` called `Zoom`. If that sounds vague, don't worry - we'll be using a choice type in the rest of this blog series, and it will become obvious very soon what a choice type is. 

All of these types are `monomorphic` types. This means that once you define the composite type, its shape is fixed in the sense that the types of all the members are known.

Most of these languages usually also provide an _array_ type, which allows you to encode an ordered set of typed values. For example, an `int` array allows you to store an ordered set of `int` values. This is generally presented as an integral part of the language, but from a type-theoretic perspective, they are a special case of a kind of type called **polymorphic** or **parametric** types.

Several advanced languages, F# included, do not provide arrays as an awkward special case in the language. They offer a full-blown range of types which are parametric in nature. A polymorphic type can be viewed as a kind of template which returns a type that is parametrized by one or more `type argument`s.

Here is an example of a polymorphic type in F#:

{% highlight fsharp %}
type Maybe<'t> =
    | None 
    | Some of 't
{% endhighlight %}

The type definition describes a type called `Maybe` with a type argument `'t`. We can now write generic code for functionality that doesn't depend on the actual type that will be passed in as a type argument to the `Maybe` type.

These types are very useful in general, but this characteristic - that it enables generic programming dependent only on the behaviour of the generic type - allows us to build type structures that have a mathematical foundation and exploit the reasonability that this brings.

One intuition we can have is that the `Maybe` type _augments_ the `'t` type argument, allowing us to encode the presence or absence of a value of type `'t`. We might see references to **augmented** types in future, and this is the intuition we should apply. 

## Advanced Concepts
The idea that you can construct types parametrically is not limited to examples like `Maybe`. For example, what happens if we want the `'t` in `Maybe<'t>` to have type arguments of its own? This type calculus is a rich and exciting area to study in its own right, and these types have a correspondingly profound impact on the expressivity and reasonability of the programs that they enable. 

It is way outside the scope of these posts - and indeed, one would have to leave all of .NET to encounter a language that support types more complex than the ones we have already encountered!

## Summary
In this short blog post, we have introduced types and ways to compose and augment them with other types. In the [next post](lego-railway-tracks-origami-post-3.html), we'll construct specific types and explore their mathematical properties.