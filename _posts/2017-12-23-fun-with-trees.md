---
    layout: post
    title: "Monkeying Around : Fun with Trees"
    tags: [functional-programming, trees, functional-data-structures, immutable-data-structures, free-monad, F#]
    author: johnazariah
    summary: My FsAdvent Post for 2017 (Dec 23)
---

Trees, like lists, are powerful, ubiquitous data structures. Also, like lists, they are recursively defined. 

## A Naive Solution

At first blush, a reasonable way of defining a tree might be:

{% highlight fsharp %}
type Tree<'a> = {
    Tag : 'a
    Children : Tree<'a> list
}
{% endhighlight %}

This looks clean and elegant, but we immediately find it restrictive when we want to actually create a nested and branched structure. Unless we always build the tree up from the leaves towards the root, we need more sophisticated ways of walking the structure, and this leads to an interesting problem. 

Specifically, if we need to introduce a reference to the parent of a given node, things get out of hand very quickly. This is the equivalent of trying to building a doubly-linked list with immutable data structures - which turns out to be a very difficult problem to solve. 

## An Object Lesson

F# is a multi-paradigm language, and we can easily sacrifice immutability to get bi-directional links.

{% highlight fsharp %}
module Tree =
    type NodeId = | Id of string

    [<AutoOpen>]
    module internal Node =
        [<AbstractClass>]
        type NodeBase<'a> () = class
            member val private _children : NodeBase<'a> list = [] with get, set 
            member internal this.Children 
                with get () = this._children
                and  set ns = this._children <- ns
            abstract member Level : int
        end

        type Root<'a> () = class
            inherit NodeBase<'a> ()
            override this.Level = 0
        end

        type Node<'a> (id: NodeId, value : 'a option, parent : NodeBase<'a>) = class
            inherit NodeBase<'a> ()
            member this.Id     = id
            member this.Parent = parent
            member val Value   = value with get, set
            override this.Level  = parent.Level + 1
        end
        
    type Tree<'a> () = class
        let root = Root<'a> ()
        member private this.Root = root
        member val private this.Current = root

        // Modify the value of the current node
        member this.ModifyValue f   = ...

        // Push a child on to the current node and make it the current node
        member this.PushChild  name = ...

        // Add a sibling to the current node and make it the current node
        member this.AddSibling name = ...

        // Pop to the parent of this node and make it the current node
        member this.Pop l           = ...

        // Other modification operations elided...

        // Visit this tree in pre-order starting at the root
        member this.VisitPreOrder f = ...

        // Visit the path to the root from the current node
        member this.VisitToRoot   f = ...
    end
{% endhighlight %}

We note the following immediately:

1. This is a familiar coding pattern. It's entirely conceivable that you would see similar code in C# or Java.
1. We are taking advantage of F#'s module system to encapsulate the `Node` and `Tree` data structures, hiding implementation details, and exposing just the operations we wish to provide
1. We are full-blown object-oriented and mutable at this point, so we are obliged to address several concerns that immutable data structures obviate.
1. The `Current` member is modified only whilst tree-building, and serves as the starting point for the `VisitToRoot` operation. 
1. `VisitPreOrder` and `VisitToRoot` must each have their own way of traversing the tree without modifying either `Root` or `Current`. Traversing the tree should necessarily be a read-only operation.

([Pre-Order Traversal](https://en.wikipedia.org/wiki/Tree_traversal#Pre-order) is one way to walk a tree from its root - other traversals are also possible.)

This solution may suffice for some cases, but we're going to consider a situation where immutablity is actually something we need for the purposes of the domain. For example, let's say we're building the tree as part of an operation, and we want to ensure that the tree returns to its original state if that operation fails. Keeping track of the tree as it grows, and being able to roll-back to a given state, is not something that is pleasant to do correctly when mutability is in the picture - and doubly so when concurrency and mutability meet as part of the problem.

So we are faced with an interesting quandary - having the ability to `VisitToRoot` or `Pop` requires bi-directional linking - which is hard to do with immutable data structures; and having the ability to check-point and roll-back tree-modification operations is difficult to do correctly without immutable data structures! What do we do?

## Painting By Numbers

What if, instead of actually creating and modifying a tree like we were taught in CS 101, we simply keep track of the list of tree-modification instructions as a kind of program? This list would have to support a limited form of mutability in that the only way to modify the list would be to append to it, but the existing contents of the list could never change.

When the tree needs to be visited, we take the list of instructions and interpret them to build a tree using the mutable approach, but since the contents of the list at this point is fixed, the tree that we create from it, is, in some sense, constant even though it contains mutable parts. Indeed, the only operations that the tree needs to support from that point on are (possibly repeated) traversals.

This approach is quite a powerful one, and can be applied to a variety of problems. We could, in fact, generalize the pattern completely in other languages that allow abstraction over types, and this forms the general principle behind what is known as the 'Free Monad'. However, since the concept is quite powerful, we are going to explore the concept concretely, and leave the abstraction of the pattern to Haskell and Scala programmers!

{% highlight fsharp %}
module Tree =
    // other members elided ...

    type internal ConstructOperation<'a> =
    | PushChild   of NodeId * 'a option
    | AddSibling  of NodeId * 'a option
    | ModifyValue of ('a option -> 'a option)
    | Pop         of int option

    type Tree<'a> () = class
        member val private ops : ConstructOperation<'a> list = [] with get, set

        member this.PushChild    x =
            this.ops <- PushChild x :: this.ops; this
        member this.AddSibling   x = 
            this.ops <- AddSibling x :: this.ops; this
        member this.ModifyValue  x =
            this.ops <- ModifyValue x :: this.ops; this
        member this.Pop         ?x =
            this.ops <- ConstructOperation.Pop x :: this.ops; this
    end
{% endhighlight %}

Of course, this is all well and good to build up a list of operations, but this doesn't actually build a tree - and we aren't really able to traverse the tree in any meaningful way. 

One sneaky thing we have done is to build the list in reverse. This ensures that each operation is processed in constant-time.

In order to build the tree, we start with a single node, and fold over the list processing each node in turn. We want the result of the fold to be the tree with bi-directional links.

{% highlight fsharp %}
let rec applyOp op (node : NodeBase<'a>)  : NodeBase<'a> = 
    match op with

    // Push a child on to the given node and return it        
    | PushChild (x, v) -> 
        let child = Node (x, v, node)
        node.Children <- upcast child :: node.Children
        upcast child

    // Add a sibling to the given node and return it
    | AddSibling (x, v) ->
        match node with
        | :? Node<'a> as n ->
            let sibling = Node(x, v, n.Parent)
            n.Parent.Children <- upcast sibling :: n.Parent.Children
            upcast sibling
        | _ -> failwith "Cannot add sibling to root"

    // Modify the value of the given node and return it
    | ModifyValue f -> 
        match node with
        | :? Node<'a> as n ->
            n.Value <- f n.Value
            upcast n
        | _ -> failwith "Cannot modify value of root"

    // Pop (recursively) to an ancestor of this node and return it
    | ConstructOperation.Pop l ->
        match node with
        | :? Node<'a> as n -> 
            let level = l |> Option.defaultValue (n.Level - 1)
            if (n.Parent.Level = level) then
                n.Parent
            elif (n.Level > level) then
                applyOp (ConstructOperation.Pop (Some level)) (n.Parent)
            else
                failwith "How did we get here?"
        | _ -> failwith "Cannot pop root"
{% endhighlight %}

This function takes an operation `op` and applies it to a given node, returning a result. The signature of the function has been chosen to align with one of the folding functions, so if we start with a list of operations and a root node, we should be able to build up a full tree from the list, and end up pointing to the *current* node.

{% highlight fsharp %}
let current = List.foldBack applyOp ops (upcast (Root()))
{% endhighlight %}

Of course, we will want to also have a handle to the root of the tree, so we can do traversals like a pre-order walk. We can get that by recursively walking up from the current position until we hit a root node.

{% highlight fsharp %}
let visitRoot start = 
    let rec visit (node : NodeBase<'a>) =
        match node with
        | :? Node<'a> as n -> seq {
                yield node
                yield! visit n.Parent
            }
        | :? Root<'a> as r -> seq { yield node }
        | _ -> Seq.empty
    visit start

let last = visitRoot current |> Seq.last 
let root = last :?> Root<'a>
{% endhighlight %}

Now, since we have started with a fixed list of operations, the `current` and `root` values represent a fixed tree. We can keep this pair in a structure that represents the "tree" version of the operations/

{% highlight fsharp %}
module Tree =
    // other members elided...

    type Tree<'a> () = class
        // other members elided...
        member this.Build () = TreeCursor<'a> (this.ops)
    end

    and TreeCursor<'a> internal (ops) = class
        let rec applyOp op (node : NodeBase<'a>)  : NodeBase<'a> = ...
        let current = List.foldBack applyOp ops (upcast (Root()))
        let visitRoot start = ...
        let root = visitRoot current |> Seq.last :?> Root<'a>
    end
{% endhighlight %}

While it might seem like a good idea to use a record for this, it might be better to use a class instead, because we don't want to expose the actual `current` and `root` members.

In fact, by using appropriate privacy modifiers on the constructor, we can make both the `Tree<'a>` and `TreeCursor<'a>` classes totally opaque - hiding the entire data structures within and only providing a clean programmatic interface to them.

Also, since a `TreeCursor` instance represents a `Tree` fixed at a given point, the only meaningful thing we can do to a `TreeCursor` is to traverse it, which leads to a very interesting observation. Since the tree is fixed, its traversals are *also* fixed. Which means we only have to traverse it once and build up a list of things we saw in the traversal, and then we can play back the traversal operations and process the tree in any way we choose.

{% highlight fsharp %}
module Tree =
    // other members elided...

    type VisitOperation<'a> =
        | VisitRoot
        | VisitChild of NodeId
        | ReadValue  of NodeId * 'a option
        | Pop

    type TreeCursor<'a> internal (ops) = class
        // other members elided...

        member this.PathToRoot = 
            let readValue (nb : NodeBase<'a>) = 
                match nb with
                | :? Node<'a> as n -> ReadValue (n.Id, n.Value)
                | _ -> VisitRoot 
            visitRoot current |> Seq.map readValue
        
        member this.PreOrderPath =
            let rec visit (node : NodeBase<'a>) =
                seq {
                    match node with
                    | :? Node<'a> as n -> yield ReadValue (n.Id, n.Value)
                    | _ -> yield! Seq.empty

                    for child in node.Children |> List.rev do
                        match child with
                        | :? Node<'a> as c -> 
                            yield! seq {
                                yield VisitChild c.Id
                                yield! visit child
                                yield VisitOperation.Pop
                            }
                        | _ -> yield! Seq.empty
                }
            visit root
    end
{% endhighlight %}

In the code snippet above, we have defined two interesting traversals - one starts at the current node and walks back to the root, and the other starts at the root and traverses the whole tree "pre-order". 

Each traversal results in a fixed sequence of `VisitOperation<'a>` for future use.

Tree traversals are best represented as folds. This is actually a much broader topic of discussion, but folding over trees can build all kinds of other data structures - including other trees, and allow for tree-rewriting.

In our case, we can traverse the tree, and then fold over it, as follows:

{% highlight fsharp %}
module Tree =
    // other members elided...

    type TreeCursor<'a> internal (ops) = class
        // other members elided...
        member this.VisitRoot<'o> 
            (processor : 'o -> VisitOperation<'a> -> 'o)
            (seed : 'o) =
                Seq.fold processor seed this.PathToRoot 

        member this.VisitPreOrder<'o>
            (processor : 'o -> VisitOperation<'a> -> 'o)
            (seed : 'o) = 
                Seq.fold processor seed this.PreOrderPath
    end
{% endhighlight %}

And there we have it. 

We have implemented a traditional tree which affords the benefits of immutable data structures (like check-pointing), whilst allowing for efficient tree traversals using parent-pointers, and functionally separating out the traversal concerns from the tree-node processing concerns.

And in less than 130 lines of code! 

## Soup's Up!

Let's build an example to see how this can be used:

{% highlight fsharp %}
    let t = Tree<string> ()

    let t = t.PushChild  (Id "a",  None)
    let t = t.PushChild  (Id "b",  None)
    let t = t.PushChild  (Id "b1", None)
    let t = t.AddSibling (Id "b2", None)
    let t = t.Pop ()
    let t = t.AddSibling (Id "c",  None)
    let t = t.PushChild  (Id "c1", None)
    let t = t.AddSibling (Id "c2", None)
    let t = t.Pop ()
    let t = t.Pop ()
    let t = t.PushChild  (Id "d",  None)
{% endhighlight %}

At this point, we have represented the building of a nested structure in an idiomatic manner, but the internal representation is simply a list of operations describing the building of the structure, rather than the structure itself.

We can then create the tree structure - with bi-directional links - at a fixed point in time, allowing us to traverse the tree.

{% highlight fsharp %}     
    let tc = t.Build ()
{% endhighlight %}

Now let's write a function to process each node as we encounter it in the traversal.

The signature of this function matches the signature used by a folding function, which allows us to fold over the list of visit operations and build up a composite value.

In our case, we want to build up a string containing a textual representation of the path in the traversal.

{% highlight fsharp %}
    let printNode res curr = 
        let c = 
            match curr with
            | ReadValue (id, vo) -> sprintf "%s%s" id.unapply (vo |> Option.map (sprintf " (%A)") |> Option.defaultValue "")
            | VisitRoot          -> "|"
            | VisitChild id      -> "↓"
            | VisitOperation.Pop -> "↑"
        sprintf "%s %s" res c
{% endhighlight %}

For a given visit operation, we compute a glyph describing the traversal ('up' and 'down' for `Pop` and `Push`), or the node's 'id' and 'value'.

We tack this value at the end of the string which represents the path taken so far.

Finally, we pass the printing function to the visitor methods instance

{% highlight fsharp %}
    printfn "Path to root  : %s" <| tc.VisitRoot     printNode ""
    printfn "Pre-Order walk: %s" <| tc.VisitPreOrder printNode ""
{% endhighlight %}

## Conclusion

This method of description and deferred interpretation is a very powerful technique in functional programming. In our case, it allowed us to separate out concerns between tree creation and tree traversal, and appropriate the benefits of immutability (for tree creation) and mutability (for traversals) without sacrificing cleanliness or readability. In fact, we have hoisted all the mechanics of traversal away from the user, and visiting the tree is reduced to simply providing a folding function.

The concept is well worth learning, as in other languages with higher-kinded types, a lot of mechanical work is lifted by these abstractions. For example, the IO monad in Haskell, and the Free Monad in Scala and Haskell both use and amplify this concept.

All the code for this article is available at [Fun With Trees](https://gist.github.com/johnazariah/b0571cf4f62926dabf611d43e9c7bec4)

Keep typing!