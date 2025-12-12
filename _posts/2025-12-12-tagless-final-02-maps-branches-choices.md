---
    layout: post
    title: "Tagless Final in F# - Part 2: Maps, Branches, and Choices"
    tags: [F#, functional-programming, tagless-final, dsl, computation-expressions]
    author: johnazariah
    summary: Introducing nondeterminism to our Froggy DSL - exploring multiple paths through the multiverse.
---

[<< Previous: Froggy Tree House](/2025/12/13/tagless-final-01-froggy-tree-house.html)

---

# Maps, Branches, and Choices: Nondeterminism Arrives

Welcome back! In the last post, we built a tiny DSL to control Froggy. But there was a problem: Froggy was a robot. He followed a strict, linear script.

```fsharp
frog {
    jump
    croak
    eat_fly
}
```

Real trees aren't linear. They branch. Sometimes Froggy has to make a choice: should he jump to the left branch or the right branch? Should he eat the fly or save it for later?

Today, we're going to introduce **Nondeterminism**.

## The Fork in the Road

We want to be able to write something like this:

```fsharp
let exploration = frog {
    jump
    choose [
        frog { croak }
        frog { jump; eat_fly }
    ]
}
```

Here, `choose` means "do any of these things". It splits the timeline. In one future, Froggy croaks. In another, he jumps and eats.

## Updating the Interpreter Definition

To support this, we need to add `Choose` to our interpreter record.

```fsharp
type FrogInterpreter<'a> = {
    Jump : unit -> 'a
    Croak : unit -> 'a
    EatFly : unit -> 'a
    // The new power!
    Choose : 'a list -> 'a
    
    Bind : 'a -> (unit -> 'a) -> 'a 
    Return : unit -> 'a
}
```

And we update our builder to support a `choose` custom operation (or just a helper function).

```fsharp
// Helper for the DSL
let choose options = fun (i: FrogInterpreter<'a>) -> 
    let interpretedOptions = options |> List.map (fun opt -> opt i)
    i.Choose interpretedOptions
```

## Interpreter 3: The Multiverse Simulator

Remember our `simulator` from last time? It returned a `FrogState -> FrogState`.
If we want to support choice, we can't return just *one* state anymore. We need to return *all possible* states.

So our new return type is `FrogState -> list<FrogState>`.

```fsharp
let multiverseSimulator : FrogInterpreter<FrogState -> list<FrogState>> = {
    // Basic actions now return a list of one result
    Jump = fun () -> fun s -> [ { s with Height = s.Height + 1 } ]
    Croak = fun () -> fun s -> [ s ] 
    EatFly = fun () -> fun s -> [ { s with Hunger = 0 } ]
    
    Return = fun () -> fun s -> [ s ]
    
    // Bind is tricky! We have to apply 'next' to ALL results from 'prev'
    Bind = fun prev next -> fun s -> 
        let possibleStates = prev s
        // For every possible state we ended up in...
        possibleStates |> List.collect (fun s' -> 
            // ...run the next step of the program
            let nextAction = next()
            nextAction s'
        )

    // Choose just concatenates the possibilities
    Choose = fun options -> fun s -> 
        options |> List.collect (fun opt -> opt s)
}
```

Now if we run `exploration` with this interpreter, we get a list of all possible outcomes!

## Interpreter 4: The Cartographer (Graph Builder)

Simulating states is cool, but it's hard to visualize. What if we want to draw a map of all possible paths Froggy can take?

We can build a **Graph Interpreter**. This interpreter will produce a list of Nodes and Edges, suitable for a tool like GraphViz (DOT format).

```fsharp
type GraphNode = { Id: int; Label: string }
type GraphEdge = { From: int; To: int; Label: string }
type Graph = { Nodes: GraphNode list; Edges: GraphEdge list; CurrentId: int }

let emptyGraph = { Nodes = []; Edges = []; CurrentId = 0 }

let graphBuilder : FrogInterpreter<Graph -> Graph> = {
    Jump = fun () -> fun g ->
        let newId = g.CurrentId + 1
        let node = { Id = newId; Label = "Jump" }
        let edge = { From = g.CurrentId; To = newId; Label = "" }
        { g with Nodes = node :: g.Nodes; Edges = edge :: g.Edges; CurrentId = newId }
    
    Croak = fun () -> fun g ->
        let newId = g.CurrentId + 1
        let node = { Id = newId; Label = "Croak" }
        let edge = { From = g.CurrentId; To = newId; Label = "" }
        { g with Nodes = node :: g.Nodes; Edges = edge :: g.Edges; CurrentId = newId }
    
    EatFly = fun () -> fun g ->
        let newId = g.CurrentId + 1
        let node = { Id = newId; Label = "EatFly" }
        let edge = { From = g.CurrentId; To = newId; Label = "" }
        { g with Nodes = node :: g.Nodes; Edges = edge :: g.Edges; CurrentId = newId }
    
    Return = fun () -> id
    
    Bind = fun prev next -> fun g ->
        let g' = prev g
        let nextAction = next()
        nextAction g'
    
    Choose = fun options -> fun g ->
        // For a graph, we create a branch point and merge all paths
        let branchId = g.CurrentId
        options 
        |> List.fold (fun acc opt -> 
            let result = opt { acc with CurrentId = branchId }
            { Nodes = result.Nodes @ acc.Nodes
              Edges = result.Edges @ acc.Edges
              CurrentId = max result.CurrentId acc.CurrentId }
        ) g
}
```

Now we can visualize Froggy's decision tree!

## The Power of Multiple Interpretations

Let's step back and appreciate what we've built.

We wrote `exploration` **once**:

```fsharp
let exploration = frog {
    jump
    choose [
        frog { croak }
        frog { jump; eat_fly }
    ]
}
```

And we can interpret it in **four different ways**:

| Interpreter | Result Type | Purpose |
|-------------|-------------|---------|
| `storyTeller` | `string` | Generate narrative text |
| `simulator` | `FrogState -> FrogState` | Single-path simulation |
| `multiverseSimulator` | `FrogState -> list<FrogState>` | All possible outcomes |
| `graphBuilder` | `Graph -> Graph` | Visualize decision tree |

The same code, four different meanings. No rewrites. No adapters. Just swap the interpreter.

## What's Next?

Froggy can now explore the multiverse. But life isn't all rainbows and flies. Sometimes there are snakes. Sometimes you reach the top of the tree.

In the next post, we'll add **Goals** and **Threats** to our DSL, and build interpreters that can detect danger before it happens!

---

*This post is part of [FsAdvent 2025](https://sergeytihon.com/fsadvent/).*

[<< Previous: Froggy Tree House](/2025/12/12/tagless-final-01-froggy-tree-house.html) | [Next: Goals, Threats, and Getting Stuck >>](/2025/12/12/tagless-final-03-goals-threats.html)
