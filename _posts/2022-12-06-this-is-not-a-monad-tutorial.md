---
    layout: post
    title: "This is not a Monad Tutorial"
    tags: [functional-programming, monads, F#]
    author: johnazariah
    summary: This is my contribution to FsAdvent 2022
    excerpt: Some fundamentals of functional programming
---

_This blog post is dedicated to Mitch Denny and Avi Pilosof: my brilliant colleagues who inspired me to talk about this topic, and  steered the discussions we had with insightful and thought provoking questions as I tried to refine the explanations._

- [1. Introduction](#1-introduction)
- [2. Let's get _go_-ing](#2-lets-get-go-ing)
- [3. Improving the solution](#3-improving-the-solution)
  - [3.1. Step 1: Wrap Up The Value](#31-step-1-wrap-up-the-value)
  - [3.2. Step 2: Allow controlled access](#32-step-2-allow-controlled-access)
  - [3.3. Step 3: Chain things Up](#33-step-3-chain-things-up)
  - [3.4. Step 4: Add Some Sugar, and Shake!](#34-step-4-add-some-sugar-and-shake)
    - [3.4.1. F#](#341-f)
    - [3.4.2. C#](#342-c)
  - [3.5. Goal Achieved](#35-goal-achieved)
- [4. Discussion: What have we actually done?](#4-discussion-what-have-we-actually-done)
- [5. Conclusion](#5-conclusion)

## 1. Introduction

As a new functional programmer, I struggled with a lot of new terminology, intimidating mathematics, strange concepts – and virtually every discussion I had made me feel like my 2 decades of experience as a professional software engineer hadn’t prepared me for FP.

Now, after many years of working with FP in the industry, and having brought many people along the journey, I have some learnings about how to communicate some foundations of functional programming to professional software engineers – starting with why functional programming matters, how to get started, how to be effective, and how to improve over time.

One common pattern I've encountered in this space is that experienced FP-ers tend to talk about _what_ something is, sometimes at great length, without providing any context of why it is useful, or what problem it solves. My aim in this blog post is to try and address this issue, and derive the motivation of the pattern from a concrete problem.

## 2. Let's get _go_-ing

One of the less satisfying things I get to do in my day job is write _go_ code. For the uninitated in this regard, _go_ code looks [a lot like this](https://github.com/Azure/aks-engine/blob/ed2cad69afe09b9a8421570531be089776d710ba/pkg/helpers/ssh/ssh.go):


```golang
func client(host *RemoteHost) (*ssh.Client, error) {
	jbConfig, err := config(host.Jumpbox.AuthConfig)
	if err != nil {
		return nil, errors.Wrap(err, "creating jumpbox client config")
	}
	jbConn, err := ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig)
	if err != nil {
		return nil, errors.Wrapf(err, "dialing jumpbox (%s)", host.Jumpbox.URI)
	}
	hostConn, err := jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port))
	if err != nil {
		return nil, errors.Wrapf(err, "dialing host (%s)", host.URI)
	}
	hostConfig, err := config(host.AuthConfig)
	if err != nil {
		return nil, errors.Wrap(err, "creating host client config")
	}
	ncc, chans, reqs, err := ssh.NewClientConn(hostConn, host.URI, hostConfig)
	if err != nil {
		return nil, errors.Wrapf(err, "starting new client connection to host (%s)", host.URI)
	}
	c, err := ssh.NewClient(ncc, chans, reqs), nil
	if err != nil {
		return nil, errors.Wrapf(err, "creating new ssh client for host (%s)", host.URI)
	}
	return c, nil
}

```

This style of `if err != nil` error-checking is, _laughably_ in my opinion, celebrated as an example of the "simplicity" of _go_ programming. I think it's reasonable to see, at the very least, that it adds a lot of boilerplate code; and at worst, it really obscures the flow of data in the program.

In the example above, the value `jbConfig` is passed on to the next function if `err != nil`, which computes `jbConn`, which is then passed on to the next function if `err != nil`, and so on. I argue that the flow of data should be the centrepiece of the function, allowing us to reason about the happy path - the error check is _essential_, but it would be really nice to elide it from view.

**Sidebar:**
_While the tuple is hailed as an example of how `go` takes advantage of types to enable a function to return a value and error code, it actually turns out to be an example of egregious type abuse. A tuple is a **product** type - something used when all its components may be present. What is actually needed here is a **choice** type - something that is **either** a value or a result, and **never** both. The consequence is that the called function can return **both** the value and error components, and it is only convention that the error component is checked first!_

Not only would it be nice to elide the error check, it would be fantastic if we had a way to _enforce_ that it happened after _every_ call in the chain. As it stands, it is left to convention (and possibly to code-review) that an error check is done, and the flow is passed to the next function only if `err != nil`. It's easy to imagine how this convention is not followed as the code evolves over time, and that results in people skipping the error check and introducing runtime defects.

In my decades of writing software and managing software teams, I have learned to **never leave to convention what can be denoted unambiguously**, and to **never leave to code review what can be enforced by the compiler**!

In summary, we would like to do the following to improve the situation:

- [ ] Ensure that the error check is not left as a responsibility of the caller
- [ ] Try to get the error check flow to not obscure the happy path. Make the happy path code flow a lot more obvious.

## 3. Improving the solution

Let's talk about how we can enforce checking first.

One thing we were taught about functions is that once it returns a value, the _caller_ is responsible to use the return value. How can we get it so that a function can make demands on the caller on having to check the value before use?

### 3.1. Step 1: Wrap Up The Value
One way is by using the type system to our advantage. `go` returns a tuple in the example above, but there's no enforcement on how the tuple components are checked or used.

While this isn't the only way of doing it, let's consider using the type system to first represent _either_ a value or an error.

In C#, where _sum_ types are not natively present, we could do this:

```csharp
sealed class ErrorChecked<T, E>
{
    private readonly T? value;
    private readonly E? error;
    private ErrorChecked(T? v, E? e) { value = v; error = e; }

    public static ErrorChecked<T, E> Value(T v) => new ErrorChecked<T, E>(v, default);
    public static ErrorChecked<T, E> Error(E e) => new ErrorChecked<T, E>(default, e);
}
```

In F#, we could simply say:

```fsharp
type ErrorChecked<'v, 'e> =
    | Value of 'v
    | Error of 'e
```

Now there is an unambiguous way of indicating if we want to construct a `Value` or an `Error`, but how do we know which is which? In C#, we're completely out of luck - there's no way to get a value out of the `ErrorChecked` class at all!

If we provide a mechanism for disambiguating between `Value` and `Error` at the _caller's_ side in C#, or if we do traditional pattern-matching in F#, then we're throwing the responsibility back to the caller, who can ignore to do the check, and then we're no better off than the `go` solution. At the very least, we've not removed the boilerplate error checking after every function call.

On the other hand, if we have this opaque box, then using it becomes super ugly. For starters, the user of `jbConfig` above needs to know how to deal with this `ErrorChecked` thing instead - and it would have to do the right thing if `ErrorChecked` was an error. So we have to effectively pollute _all_ the code with the concerns of what to do.

We'll land up doing something like this:

```csharp
    ErrorChecked<Client, Error> client(RemoteHost host)
    {
        ErrorChecked<Config, Error> jbConfig = config(host.Jumpbox.AuthConfig);
        // if jbConfig is not error then take its value and call the next function otherwise exit
        ErrorChecked<Connection, Error> jbConn = ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig);
        // if jbConn is not error then take its value and call the next function otherwise exit
        ErrorChecked<HostConnection, Error> hostConn = jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port));
        // if hostConn is not error then take its value and call the next function otherwise exit
        //...
    }
```

Now, let me admit that we haven't a working solution at this point! What we have is:

- [x] Return a value that the caller _cannot_ ignore.
- [x] Make the flow of the code a lot more obvious.
- [ ] Mandate the error checking.
- [ ] Allow caller to pass values returned from one function into the arguments of a subsequent function.
- [ ] Make the code easy to read.

The first point is the most important because we have begun to address a fairly fundamental problem: How to **enforce control of how the _caller_ uses the return value of a function**.

### 3.2. Step 2: Allow controlled access

As we have seen above, `ErrorChecked` is now an opaque box, and we want to ensure that it is only used after checking for error. We've also agreed that opening the opaque box and letting the caller deal with the values is basically no better than the `go` solution!

However, we're functional programmers, and we love lambdas. We can find a way to hand a lambda to the `ErrorChecked` class and ask _it_ to call the lambda without having to have open-season on the internal values. In fact, we can find a way to only call the lambda in the happy path, and just pass along whatever error otherwise.

We'll write the `CallWithValue` method as follows:

```csharp
sealed class ErrorChecked<T, E>
{
    ...
    public ErrorChecked<R, E> CallWithValue<R>(Func<T?, ErrorChecked<R, E>> op) =>
        error is null
        ? op(value)
        : ErrorChecked<R, E>.Error(error);
}
```

In F#, instead of making the pattern match a responsibility of the caller, we can do the same kind of thing as the C# solution, and add a helper method to _always_ do the pattern match for us:

```fsharp
type Checked<'v, 'e> =
    | Value of 'v
    | Error of 'e
with
    member this.CallWithValue (op: 'v -> Checked<'r, 'e>) : Checked<'r, 'e> =
        match this with
        | Error e -> Error e
        | Value v -> op v
```

Now we have provided a controlled-access mechanism to _use_ the value of the `ErrorChecked`, as well as enforcing the null check before we use the value. Since there is _no other_ way to get at the inside of the opaque box, we enforce the safe access of the value by the `op` lambda above.

Of course, we haven't fixed everything, and the program doesn't run yet, but we're improving:

- [x] Return a value that the caller _cannot_ ignore.
- [x] Make the flow of the code a lot more obvious.
- [x] Mandate the error checking.
- [ ] Allow caller to pass values returned from one function into the arguments of a subsequent function.
- [ ] Make the code easy to read.


### 3.3. Step 3: Chain things Up

All right. We've made a few small steps forward, but we've now reached a high mountain. How are we going to approach the problem of getting subsequent functions to be called through these lambdas? Won't that make the code look completely convoluted?

These are fair questions.

Let's take a couple steps back and look at the `go` code again, and rewrite it in a way to get a different perspective on what the code is doing.

The original code was:

```golang

func client(host *RemoteHost) (*ssh.Client, error) {
	jbConfig, err := config(host.Jumpbox.AuthConfig)
	if err != nil {
		return nil, errors.Wrap(err, "creating jumpbox client config")
	}
	jbConn, err := ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig)
	if err != nil {
		return nil, errors.Wrapf(err, "dialing jumpbox (%s)", host.Jumpbox.URI)
	}
	hostConn, err := jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port))
	if err != nil {
		return nil, errors.Wrapf(err, "dialing host (%s)", host.URI)
	}
	hostConfig, err := config(host.AuthConfig)
	if err != nil {
		return nil, errors.Wrap(err, "creating host client config")
	}
	ncc, chans, reqs, err := ssh.NewClientConn(hostConn, host.URI, hostConfig)
	if err != nil {
		return nil, errors.Wrapf(err, "starting new client connection to host (%s)", host.URI)
	}
	c, err := ssh.NewClient(ncc, chans, reqs), nil
	if err != nil {
		return nil, errors.Wrapf(err, "creating new ssh client for host (%s)", host.URI)
	}
	return c, nil
}

```

Let's rewrite the happy path in C#-ish pseudocode:

```csharp
    ErrorChecked<Client, Error> client(RemoteHost host)
    {
        var jbConfig = config(host.Jumpbox.AuthConfig);
        var jbConn = ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig);
        var hostConn = jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port));
        var hostConfig = config(host.AuthConfig);
        var (ncc, chans, reqs) = ssh.NewClientConn(hostConn, host.URI, hostConfig);
        var c = ssh.NewClient(ncc, chans, reqs), nil;
        return c;
    }
```

Now let's squint a little at the first two lines:

```csharp
    var jbConfig = config(host.Jumpbox.AuthConfig);
    var jbConn = ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig);
    ...
```

Here, some function called `config` returns a `jbConfig`, but we actually don't know what else it does. It might call over the internet to get a file and deserialize that into an object - we don't really know, and at this point, we don't really care: that's what abstraction gives us.

What we care about is that it _might_ fail somewhere, so we need it to return a `ErrorChecked` - let's assume now that, for sake of argument, it does. We can no longer just pass it along to the next function, because we only get a valid `jbConfig` when `config` succeeds. We also put the value in a box and we only get to access it via this lambda mechanism, so we can't actually write code like we used to anymore.

What we need to package the rest of the function into the lambda and pass it to the `CallByValue` method on the `ErrorChecked` returned by `config`:

```csharp
    config(host.Jumpbox.AuthConfig).CallWithValue(jbConfig =>
    {
        jbConn = ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig);
        ...
    })
```

Of course, we don't stop there - now we need to do the same operation with `jbConn`, because that's a `ErrorChecked` as well.

If we do this for the whole happy path, we get something like this:

```csharp
    ErrorChecked<Client, Error> client(RemoteHost host) =>
        config(host.Jumpbox.AuthConfig)
            .CallWithValue(jbConfig =>
                ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig)
                .CallWithValue(jbConn =>
                    jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port))
                    .CallWithValue(hostConn =>
                        config(host.AuthConfig)
                        .CallWithValue(hostConfig =>
                            ssh.NewClientConn(hostConn, host.URI, hostConfig)
                            .CallWithValue((ncc, chans, reqs) =>
                                ssh.NewClient(ncc, chans, reqs), nil)))));
```

Now, this looks like an abominable mess compared to what we started with. However, we have almost succeeded in meeting our refactoring goal completely:

- [x] Return a value that the caller _cannot_ ignore.
- [x] Make the flow of the code a lot more obvious.
- [x] Mandate the error checking.
- [x] Allow caller to pass values returned from one function into the arguments of a subsequent function.
- [ ] Make the code easy to read.

### 3.4. Step 4: Add Some Sugar, and Shake!
However, we'd be hard-pressed to call this a success, because we've kind of turned everything inside out, and taken a relatively neat sequence of instructions and converted it into this deeply-indented horror!

In fact, this "deeply-indented horror" has a name: it is actually the program written in a form known as "Continuation Passing Style", and as we will see later in this blog post, is a universally powerful way to express code.

It turns out that even a normal sequence of instructions - like the semi-colon separated sequence we're familiar with - can be _mechanically_ converted (de-sugared) into continuation passing style. Of course, we want to go the other way, and apply some form of syntactic sugar to convert this continuation passing style into something more palatable.

This is where the language we're working with makes a _big_ difference. Functional languages like Scala, Haskell and F# have simple and easy ways to put a pleasant syntax on the continuation chain. C# _does_ have a way to represent continuation chains around arbitrary wrappers as well. (`go`, on the other hand, unfortunately, does not - and one is cursed to write out the tedious `if err != nil` blocks by hand.)

Let's first consider F#:
#### 3.4.1. F#

F# provides a construct called "Computation Expressions" which allow you to provide syntactic sugar over one of these chains. There are a few more technical issues to consider which I'll elide for now and come to later in the post:

```fsharp
type ErrorChecked<'v, 'e> =
    | Value of 'v
    | Error of 'e
with
    member this.CallWithValue (op: 'v -> Checked<'r, 'e>) : Checked<'r, 'e> =
        match this with
        | Error e -> Error e
        | Value v -> op v

type ErrorCheckedBuilder() =
    member _.Bind(comp: Checked<'v, 'e>, func: 'v -> ErrorChecked<'r, 'e>) = comp.CallWithValue(func)
    member _.Return(value) = Checked<'v, 'e>.Value value

let error_checked = new ErrorCheckedBuilder()
```

We have just added a `Builder` class, and given it two well-known member names, and created an instance of the builder class to give us the computation expression.

This let us to write the chain as follows:

```fsharp
let client = // ErrorChecked<Client, Error>
    error_checked {
        let! jbConfig = config(host.Jumpbox.AuthConfig)
        let! jbConn = sh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig)
        let! hostConn = jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port))
        let! hostConfig = config(host.AuthConfig)
        let! (ncc, chans, reqs) = ssh.NewClientConn(hostConn, host.URI, hostConfig)
        let! client = ssh.NewClient(ncc, chans, reqs)
        return client
    }
```

The `error_checked` invocation tells F# - and us - that these invocations are made are in the context of `ErrorChecked`.

The only added noise seems to be the `let!` instead of the `let` for the bindings, but one can read `let!` as "unwrap the context, pull out and assign the internal value".

Adding the syntactic sugar definitely makes it _much_ easier to read, and we've elided all the error checks inside the `error_checked` context.

- [x] We _have_ managed to return a value that the caller _cannot_ ignore.
- [x] The flow of the code is a _lot more obvious_ (if you ignore the comments).
- [x] We haven't actually done any error checking yet.
- [x] The caller can't actually access the returned value yet.
- [x] We've broken how to pass values from a `ErrorChecked` returned from one function into the arguments of a subsequent function.
- [x] The code is _easy to read_.

#### 3.4.2. C#

Now, in C#, due to the foresight of people like Erik Meijer, we have a way to provide syntactic sugar for such continuation chains as well.

LINQ allows us to convert any of these kinds  continuation chains into code like this:

```csharp
    var client =
        from jbConfig in config(host.Jumpbox.AuthConfig)
        from jbConn in sh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig)
        from hostConn in jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port))
        from hostConfig in config(host.AuthConfig)
        ...
        select client;
```

Now, in order to do this, we need to write a single extension method on the wrapper class to allow LINQ to give us this syntactic sugar. It turns out to be a little more general than the F# version, and here it is in all its glory.


```csharp
public static partial class LinqExtensions
{
    public static ErrorChecked<C,E> SelectMany<A, B, C, E>(
        this ErrorChecked<A,E> ma,
        Func<A, ErrorChecked<B,E>> f,
        Func<A, B, C> select) =>
            ma.Bind(a =>
                f(a).Map(b =>
                    select(a, b)));
}
```

The key thing to take away is that you can do this with _any_ wrapper class that you want to use as the chaining context.

Our friend `CallWithValue` doesn't show up in the extension method, and there are these two curiously named functions called `Map` and `Bind` instead. That's because `CallWithValue` could reasonably take two different lambdas: one which returned an unwrapped value, and one which returned a wrapped value. We'll write both in our wrapper class thus:

```csharp

public sealed class ErrorChecked<T, E>
{
    private readonly T? value;
    private readonly E? error;
    private ErrorChecked(T? v, E? e) { value = v; error = e; }

    public static ErrorChecked<T, E> Value(T v) => new(v, default);
    public static ErrorChecked<T, E> Error(E e) => new(default, e);

    public ErrorChecked<R, E> Map<R>(Func<T, R> op) =>
        error is null
        ? ErrorChecked<R, E>.Value(op(value!))
        : ErrorChecked<R, E>.Error(error);

    public ErrorChecked<R, E> Bind<R>(Func<T, ErrorChecked<R, E>> op) =>
        error is null
        ? op(value!)
        : ErrorChecked<R, E>.Error(error);
}

```

And that's all it takes to give syntactic sugar to our continuation chain. I _could_ dive into why we have these wierdly named functions, and what the various properties and relationships are between these functions but I won't, because _this is not a monad tutorial!_

Now, the LINQ syntax is not typical semi-colon-separated C#, but it is definitely clutter-free and easy to read. I therefore make the claim that we have met the goals we started out with:

- [x] Return a value that the caller _cannot_ ignore.
- [x] Make the flow of the code a lot more obvious.
- [x] Mandate the error checking.
- [x] Allow caller to pass values returned from one function into the arguments of a subsequent function.
- [x] Make the code easy to read.

### 3.5. Goal Achieved

We set out with the following goals, which we can now claim as completed:

- [x] Ensure that the error check is not left as a responsibility of the caller
- [x] Try to get the error check flow to not obscure the happy path. Make the happy path code flow a lot more obvious.

## 4. Discussion: What have we actually done?

Let's summarize the problem:

At a naive level, this is what we stated the problem to be:

* We want to consistently error-check the results of functions called in a sequence of instructions, but we don't want it to compete for attention from the focus of what this sequence is doing.

At a slightly more insightful - and perhaps a little more abstract - level, what we really wanted was:

* We want a way to sift out a boilerplate pattern from the code and allow us to take the boilerplate as part of the context of executing a sequence of instructions.

Now, there are many approaches we could have taken to do this. Let's summarize the one we took:

* In order to force the caller of a function to treat the returned value in a way that forces the boilerplate to run, we chose to wrap all return values from a function in an opaque unbreakable box.
* Since the returned value is opaque and unbreakable, one way to utilize the contained value is to invert control and package the rest of the instructions into a lambda, which we then pass to the opaque unbreakable box - allowing it to run any boilerplate and then call the lambda with the hidden value.
* The lambda actually has no idea that it's being executed in this special context. This is a profound realization, which will lead to some interesting consequences.
* We apply this inversion of control to the rest of the statements, and this results in a deeply nested chain of continuations. We can also make some observations about this chain - the first of which is that it's tail-callable, which means it can run with constant stack space. This shouldn't come as any surprise because the initial sequence of instructions was also runnable with constant stack space.
* We then apply some syntactic sugar using the mechanics afforded by the language we're using. All languages may be Turing-equivalent, but they are most definitely not created equal.
  * Some languages - like Haskell and Scala, and C#, make this syntactic sugar capability consistently available to _any_ context.
  * Some languages - like F# - make it unique for each context.
  * Some languages - like C# - make special syntax for different contexts.
  * Some languages - like Go - don't allow you to have any special syntax, so you're unable to abstract out boilerplate without resorting to continuation-passing-style

We should spend a little time looking at this and think about the perspectives of a programmer seasoned in a given language.
* The Scala and Haskell users in my audience will probably wonder what all the fuss is about - and wonder why I finessed all the hardcore mathematical concepts from this post
* The F# users will probably nod a trifle smugly at the Computation Expressions syntax, and comment about how it's within the idiom of the language to use this all the time
* The C# users will fall into two camps - a small minority will be comfortable with the LINQ syntax and its expressive power in hiding boilerplate, but the vast majority will look at this post as a kind of scientific curiosity - perhaps useful for some special people but not really applicable in daily use.
* The Go users will, reasonably, look at the horrible inverted chain of continuations and decide that the _best way_ to do error correction is to `if err != nil` everywhere.

These reactions do not reflect on the personalities, abilities, or aptitudes of the programmers themselves - they are a function of the affordances of the language they are trying to apply this concept to.

OK, now coming back to the problem statement, what were we really trying to do?

* We had a sequence of instructions, and we wanted to compose them together within the context of something that could execute some boilerplate at the point of composition. What we were trying to do was replace the benign `;` which sequences instructions in C#, for example, with some magic that ran the boilerplate at that point.

And that's where the FP perspective comes in.

Our sequence of instructions is really a sugared syntax for a deeply composed continuation chain.

Even when we use `;` as the sugar, the context is basically the _unit_ context which does nothing except make the lambda argument available to the lambda, and the sequence is actually equivalent to the continuation chain.

Specifically, this:

```csharp
    var jbConfig = config(host.Jumpbox.AuthConfig);
    var jbConn = ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig);
    var hostConn = jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port));
    var hostConfig = config(host.AuthConfig);
    var (ncc, chans, reqs) = ssh.NewClientConn(hostConn, host.URI, hostConfig);
    var c = ssh.NewClient(ncc, chans, reqs), nil;
    return c;
```

is _actually_ always equivalent to this:

```csharp
    ErrorChecked<Client, Error> client(RemoteHost host) =>
        config(host.Jumpbox.AuthConfig)
            .CallWithValue(jbConfig =>
                ssh.Dial("tcp", fmt.Sprintf("%s:%d", host.Jumpbox.URI, host.Jumpbox.Port), jbConfig)
                .CallWithValue(jbConn =>
                    jbConn.Dial("tcp", fmt.Sprintf("%s:%d", host.URI, host.Port))
                    .CallWithValue(hostConn =>
                        config(host.AuthConfig)
                        .CallWithValue(hostConfig =>
                            ssh.NewClientConn(hostConn, host.URI, hostConfig)
                            .CallWithValue((ncc, chans, reqs) =>
                                ssh.NewClient(ncc, chans, reqs), nil)))));
```

In the case of just the plain `;`, we pretend that the default `.CallWithValue` used just calls the lambda passed in.

If the language we use allows us to provide some other sugared syntax, then we can use some other wrapper class with whatever boilerplate functionality into `.CallWithValue` as we choose. The mechanics of converting to a continuation chain _remains the same_ in both cases.

This should be a profound realization - one taught in language design school - that the syntax of any language is just sugar over its semantic constructions. If we also realize that continuations are one of the most general composition forms, and that almost all language control constructs can be expressed as continuations, then we have a _very_ expressive mechanism on our hands. (These are _very_ bold claims, and I am using _very_ loose language here - so if you are Dr. Shriram Krishnamurti, please don't eviscerate me for my lassitude! :D)

We could transform the continuation passing form into different syntaxes based on the affordances (or lack thereof) of the language being used. A direct translation of this is that some languages afford greater expressivity than others. (Again, very loose language here!)

At any rate, it's easy to see that what we're trying to solve is the problem of composing functions: not plain composition, but composition within a context.

## 5. Conclusion

We had to do some work to get here, but in the end, that's all we were doing - composing functions in context.

This allows us to do a surprising amount of work, and a lot of languages realize the power of what this mental model is, and allow for the application of syntactic sugar to make this usable.

Can we do this in other ways? Absolutely!

Do we need to learn or understand a bunch of scary maths and notation to do any of this? Absolutely not!

Is there a reason why this is a better way? Absolutely - but this should serve as the starting point of your exploratory journey. We can explore _why_ this is a better way to do things. We can talk about what _other_ patterns are prevalent all over the place which can use similar treatment. We can discover more techniques to make code more robust, elegant, reason-able, and efficient. And we can do that all with functions - and functional programming, at our own pace. Yes, some of these will require more perspective shifting; perhaps some new notation and terminology; and perhaps some math.

But what I hope this post has given you is that you can start where you are, and start with a problem that is easy to relate to, and logically - almost mathematically - derive a solution to the problem...even a solution that has deep mathematical foundations, but without being frightened off by notation and terminology.

And that is a good first step. Happy Holidays!
