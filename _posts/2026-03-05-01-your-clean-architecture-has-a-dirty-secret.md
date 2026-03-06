---
    layout: post
    title: "Intent vs Process - Part 1: Your Clean Architecture Has a Dirty Secret"
    tags: [C#, software-architecture, clean-architecture, CQRS, dependency-injection, functional-programming]
    author: johnazariah
    summary: "N-tier, Clean Architecture, CQRS, microservices, vertical slices — they all share the same unspoken coupling. This post names it."
---

_This series is dedicated to [Christian Smith](https://www.linkedin.com/in/christian-smith-9562658/), with gratitude for all the insightful conversations that shaped the ideas in these posts._

> **Series: Your Clean Architecture Has a Dirty Secret**
>
> This is Part 1 of a 6-part series on separating intent from process in real-world C#.
>
> 1. **Your Clean Architecture Has a Dirty Secret** ← you are here
> 2. The Algebra of Intent
> 3. Intent You Can See (and Optimize)
> 4. Two Sides of the Same Coin
> 5. Standing on the Shoulders of Giants
> 6. The Strangler Fig

---

# Your Clean Architecture Has a Dirty Secret

Let me show you a piece of code that would pass any code review.

- [The Code Everyone Approves](#the-code-everyone-approves)
- [Pulling the Threads](#pulling-the-threads)
- [The Architecture Gauntlet](#the-architecture-gauntlet)
  - [Round 1: Clean Architecture](#round-1-clean-architecture--hexagonal--ports--adapters)
  - [Round 2: CQRS + Event-Driven](#round-2-cqrs--event-driven)
  - [Round 3: Microservices](#round-3-microservices)
  - [Round 4: MediatR / Vertical Slices](#round-4-mediatr--vertical-slices)
- [The Diagnosis](#the-diagnosis)
- [What If?](#what-if)

---

## The Code Everyone Approves

Here's an e-commerce order processing service. It validates inventory, calculates pricing, charges the customer, reserves the items, and sends a confirmation email. Standard stuff.

```csharp
public class OrderService
{
    private readonly IInventoryRepository _inventory;
    private readonly IPricingService _pricing;
    private readonly IPaymentGateway _payment;
    private readonly IEmailService _email;

    public OrderService(
        IInventoryRepository inventory,
        IPricingService pricing,
        IPaymentGateway payment,
        IEmailService email)
    {
        _inventory = inventory;
        _pricing = pricing;
        _payment = payment;
        _email = email;
    }

    public async Task<OrderResult> PlaceOrder(OrderRequest request)
    {
        var stock = await _inventory.CheckStock(request.Items);
        if (!stock.IsAvailable)
            return OrderResult.Failed("Out of stock");

        var price = _pricing.Calculate(request.Items, request.Coupon);

        var charge = await _payment.Charge(request.PaymentMethod, price.Total);
        if (!charge.Succeeded)
            return OrderResult.Failed("Payment failed");

        await _inventory.Reserve(request.Items);
        await _email.SendConfirmation(request.Customer, price);

        return OrderResult.Success(charge.TransactionId);
    }
}
```

SOLID principles? Check. Dependency injection? Check. Interface segregation? Check. Single responsibility? Well... let's talk about that.

This code passes code review. It has tests. It's the kind of code that gets praised in pull requests.

And it has a dirty secret.

---

## Pulling the Threads

Let's ask two simple questions.

### What does this code *want* to do?

Five things:
1. **Validate** — check inventory
2. **Price** — calculate the total
3. **Charge** — take payment
4. **Reserve** — hold the items
5. **Notify** — send a confirmation

Clear intent. Five steps. A business analyst could read this list and nod.

### What does this code *actually* do?

Oh, much more than that. It also decides:

- **Sync vs. async**: Those `await` keywords? They commit us to async I/O. The business logic didn't ask for that.
- **Error strategy**: Early return on failure. What about retry? Fallback? Partial success? The method picks one strategy and bakes it in.
- **Execution order**: Steps happen sequentially. Could `CheckStock` and `Calculate` run in parallel? The business logic doesn't care — but this code does.
- **Protocol**: Behind those interfaces, there are HTTP calls, database queries, SMTP connections. The method doesn't name them, but it's shaped by them (those `await`s again).
- **Observability**: Where's the logging? The distributed tracing? The metrics? Nowhere — and the moment you add them, they'll be *in this method*.
- **Failure semantics**: Payment succeeded but `Reserve` failed? That's a real-money problem. There's no compensation here. No rollback. No saga. Adding one will *double the size of this method* — and all of the new code will be "how," not "what."

The *what* and the *how* are inseparable in this code.

**This code knows too much.**

### Show the Pain

It's not an aesthetic complaint. This coupling has consequences.

**Testing is mock hell.** To test the business logic — "charge happens after validation" — you need to mock four dependencies:

```csharp
[Test]
public async Task PlaceOrder_ChargesAfterValidation()
{
    var inventory = new Mock<IInventoryRepository>();
    inventory.Setup(i => i.CheckStock(It.IsAny<List<Item>>()))
             .ReturnsAsync(new StockResult(true));

    var pricing = new Mock<IPricingService>();
    pricing.Setup(p => p.Calculate(It.IsAny<List<Item>>(), null))
           .Returns(new PriceResult(99.50m));

    var payment = new Mock<IPaymentGateway>();
    payment.Setup(p => p.Charge(It.IsAny<PaymentMethod>(), 99.50m))
           .ReturnsAsync(new ChargeResult(true, "txn-123"));

    var email = new Mock<IEmailService>();

    var svc = new OrderService(
        inventory.Object, pricing.Object,
        payment.Object, email.Object);

    var result = await svc.PlaceOrder(request);

    Assert.That(result.Succeeded);
    payment.Verify(
        p => p.Charge(It.IsAny<PaymentMethod>(), 99.50m),
        Times.Once);
}
```

Twenty lines of ceremony. Two lines of intent. The test is more infrastructure than assertion. And if you add a fifth dependency — say, a `IFraudService` — every single test breaks, whether it cares about fraud or not.

**Changing the "how" rewrites the "what."** Boss says: _"Make payment async via a queue instead of synchronous."_ The business intent didn't change. The five steps are still the five steps. But you're rewriting `PlaceOrder` from scratch — different types, different control flow, different error handling. The `git diff` will show the entire method body changed. Why? The business logic didn't.

**Cross-cutting concerns invade the method.** Add logging:

```csharp
_logger.LogInformation("Checking stock for {ItemCount} items", request.Items.Count);
var stock = await _inventory.CheckStock(request.Items);
_logger.LogInformation("Stock check result: {Available}", stock.IsAvailable);
```

Add retry:

```csharp
var charge = await Policy
    .Handle<PaymentException>()
    .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)))
    .ExecuteAsync(() => _payment.Charge(request.PaymentMethod, price.Total));
```

Add circuit breaker, add metrics, add distributed tracing. Each one modifies the method body. The business logic — validate, price, charge, reserve, notify — drowns in infrastructure. And the file gets modified every time an infrastructure concern changes, even though the *intent* hasn't changed at all.

**Every time the infrastructure changes, the business logic file gets modified. That's the smell.**

**Compensation is a nightmare.** Payment succeeded but inventory reservation failed. You need to refund the charge. Add that:

```csharp
public async Task<OrderResult> PlaceOrder(OrderRequest request)
{
    var stock = await _inventory.CheckStock(request.Items);
    if (!stock.IsAvailable)
        return OrderResult.Failed("Out of stock");

    var price = _pricing.Calculate(request.Items, request.Coupon);

    var charge = await _payment.Charge(request.PaymentMethod, price.Total);
    if (!charge.Succeeded)
        return OrderResult.Failed("Payment failed");

    try
    {
        await _inventory.Reserve(request.Items);
    }
    catch
    {
        // Compensate: refund the charge
        await _payment.Refund(charge.TransactionId);
        return OrderResult.Failed("Reservation failed — payment refunded");
    }

    try
    {
        await _email.SendConfirmation(request.Customer, price);
    }
    catch
    {
        // Do we compensate here? Release the reservation?
        // Refund the payment? Both? Neither? Log and alert?
        // The business rule is unclear and the code is getting out of hand.
    }

    return OrderResult.Success(charge.TransactionId);
}
```

The method doubled in size. Every line we added is "how" — not "what." The business intent (validate → price → charge → reserve → notify) is the same. The intent is *buried alive* under compensation logic.

Now, at this point, every experienced architect reading this already has a solution in mind. "That's just poorly structured," they're thinking. "I'd refactor to Clean Architecture. I'd use CQRS. I'd split into microservices."

Let's try each one.

---

## The Architecture Gauntlet

### Round 1: Clean Architecture / Hexagonal / Ports & Adapters

_"The answer is to put the business logic in the center and push infrastructure to the edges."_

Robert C. Martin's Clean Architecture. Alistair Cockburn's Hexagonal Architecture. Ports and Adapters. The circles. You know the diagram.

The `OrderService` moves to a `Core` project. The interfaces become "ports." The implementations become "adapters." The dependency arrows point inward. Project references enforce the rule: `Core` knows nothing about `Infrastructure`.

```
┌──────────────────────────────────────────────┐
│              Infrastructure                   │
│  ┌────────────────────────────────────────┐  │
│  │           Application                  │  │
│  │  ┌──────────────────────────────────┐  │  │
│  │  │         Core / Domain            │  │  │
│  │  │                                  │  │  │
│  │  │   OrderService (business logic)  │  │  │
│  │  │   IInventoryRepository (port)    │  │  │
│  │  │   IPaymentGateway (port)         │  │  │
│  │  │                                  │  │  │
│  │  └──────────────────────────────────┘  │  │
│  │  SqlInventoryRepository (adapter)      │  │
│  │  StripePaymentGateway (adapter)        │  │
│  └────────────────────────────────────────┘  │
│  ASP.NET Controllers, Middleware              │
└──────────────────────────────────────────────┘
```

Beautiful. Principled. Well-factored.

Now open `Core/Services/OrderService.cs`:

```csharp
public async Task<OrderResult> PlaceOrder(OrderRequest request)
{
    var stock = await _inventory.CheckStock(request.Items);
    if (!stock.IsAvailable)
        return OrderResult.Failed("Out of stock");

    var price = _pricing.Calculate(request.Items, request.Coupon);

    var charge = await _payment.Charge(request.PaymentMethod, price.Total);
    if (!charge.Succeeded)
        return OrderResult.Failed("Payment failed");

    await _inventory.Reserve(request.Items);
    await _email.SendConfirmation(request.Customer, price);

    return OrderResult.Success(charge.TransactionId);
}
```

It's the same method. Character for character.

The same `await`s deciding sync-vs-async. The same early-return deciding error strategy. The same sequential execution. The same absent compensation. The same mock-hell tests. We moved the coupling to a different folder structure. **The coupling itself didn't change.**

> Clean Architecture tells you *where* to put the how. It doesn't tell you to separate it from the what.

The circles protect you from `Core` depending on SQL Server. They don't protect you from the business intent living in the same block of code that decides async strategy, error handling, and execution order. That coupling is *inside* the clean center, perfectly organized, perfectly principled — and perfectly fused.

### Round 2: CQRS + Event-Driven

_"The answer is to separate reads from writes and communicate through events."_

Fine. Let's go full CQRS with an event-driven saga.

```csharp
// Command
public record PlaceOrderCommand(List<Item> Items, PaymentMethod Payment, Customer Customer);

// Events
public record OrderValidated(Guid OrderId, List<Item> Items);
public record OrderPriced(Guid OrderId, decimal Total);
public record PaymentCharged(Guid OrderId, string TransactionId);
public record InventoryReserved(Guid OrderId);
public record OrderConfirmed(Guid OrderId);
public record OrderFailed(Guid OrderId, string Reason);

// Command Handler
public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand>
{
    public async Task Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var stock = await _inventory.CheckStock(command.Items);
        if (!stock.IsAvailable)
        {
            await _bus.Publish(new OrderFailed(orderId, "Out of stock"));
            return;
        }
        await _bus.Publish(new OrderValidated(orderId, command.Items));
    }
}
```

And then the saga / process manager:

```csharp
public class OrderSaga :
    IHandleMessages<OrderValidated>,
    IHandleMessages<OrderPriced>,
    IHandleMessages<PaymentCharged>,
    IHandleMessages<InventoryReserved>
{
    public async Task Handle(OrderValidated msg, IMessageHandlerContext ctx)
    {
        Data.Items = msg.Items;
        var price = _pricing.Calculate(msg.Items);
        await ctx.Publish(new OrderPriced(msg.OrderId, price.Total));
    }

    public async Task Handle(OrderPriced msg, IMessageHandlerContext ctx)
    {
        var charge = await _payment.Charge(Data.PaymentMethod, msg.Total);
        if (!charge.Succeeded)
        {
            await ctx.Publish(new OrderFailed(msg.OrderId, "Payment failed"));
            return;
        }
        await ctx.Publish(new PaymentCharged(msg.OrderId, charge.TransactionId));
    }

    // ... and so on for each event → next action
}
```

It's more code. Is it better?

Look at `Handle(OrderPriced msg)`. It decides: call the payment gateway directly (not via a queue). Check `charge.Succeeded` (specific error strategy). Publish the next event synchronously within the handler (execution order). Where's the retry? Where's the timeout? Where's the compensation? The saga handler *still fuses intent with process*. It decides retry policy, timeout, event routing, and compensation order — all in the handler body.

And the business intent — "place this order" — is now *scattered across* a command handler, four event handlers, and a saga state machine. To understand what "place an order" means for the business, you need to mentally reassemble the flow from six different methods in a class that also manages persistence, timeouts, and routing.

> CQRS doesn't eliminate the coupling. It distributes it.

The what-how fusion didn't disappear. It got cut into pieces and spread across event handlers. Each piece is smaller, yes. But in each piece, intent and process are still inseparable. And now you have a new problem: the overall intent is only visible if you can mentally reconstruct the entire saga flow. Good luck onboarding a new team member.

### Round 3: Microservices

_"The answer is to split into Inventory Service, Payment Service, Email Service."_

Each service owns its domain. Each has its own database. They communicate over HTTP or message queues. The Inventory Service doesn't know about payments. The Payment Service doesn't know about inventory.

```csharp
// Order Orchestrator Service
public class OrderOrchestrator
{
    private readonly HttpClient _inventoryApi;
    private readonly HttpClient _pricingApi;
    private readonly HttpClient _paymentApi;
    private readonly HttpClient _emailApi;

    public async Task<OrderResult> PlaceOrder(OrderRequest request)
    {
        var stockResponse = await _inventoryApi.PostAsJsonAsync("/check-stock", request.Items);
        stockResponse.EnsureSuccessStatusCode();
        var stock = await stockResponse.Content.ReadFromJsonAsync<StockResult>();

        if (!stock.IsAvailable)
            return OrderResult.Failed("Out of stock");

        var priceResponse = await _pricingApi.PostAsJsonAsync("/calculate", new { request.Items, request.Coupon });
        priceResponse.EnsureSuccessStatusCode();
        var price = await priceResponse.Content.ReadFromJsonAsync<PriceResult>();

        // ... you get the idea
    }
}
```

Same five steps. Same sequential decisions. Same error handling baked in. But now we also have:

- **Network failures**: What happens when the Payment Service is up but the Inventory Service is down mid-flow?
- **Eventual consistency**: The charge went through, but the inventory update hasn't propagated yet.
- **Retry storms**: Your circuit breaker retries the payment call three times. The Payment Service's own retry logic retries it twice internally. That's six calls.
- **Deployment coordination**: Changing the order flow means deploying changes to the Orchestrator, the Payment Service, and the Inventory Service — in the right order.
- **Distributed tracing**: Where's the log entry? In which of five services? With which correlation ID?

The orchestrator still decides sequencing, error strategy, and compensation — just like the original `PlaceOrder`. The business intent is still coupled with the infrastructure decisions. Now the infrastructure decisions are *also* coupled with network topology.

> Microservices don't separate intent from process. They add a network boundary on top of the still-fused coupling.

### Round 4: MediatR / Vertical Slices

_"The answer is to organize by feature, not by layer. Each slice owns everything."_

Jimmy Bogard's Vertical Slice Architecture. One file per feature. The `PlaceOrderHandler` owns the controller action, the business logic, the data access — everything for that feature in one place.

```csharp
public class PlaceOrder
{
    public record Command(List<Item> Items, PaymentMethod Payment, Customer Customer)
        : IRequest<OrderResult>;

    public class Handler : IRequestHandler<Command, OrderResult>
    {
        private readonly IInventoryRepository _inventory;
        private readonly IPricingService _pricing;
        private readonly IPaymentGateway _payment;
        private readonly IEmailService _email;

        public async Task<OrderResult> Handle(Command request, CancellationToken ct)
        {
            var stock = await _inventory.CheckStock(request.Items);
            if (!stock.IsAvailable)
                return OrderResult.Failed("Out of stock");

            var price = _pricing.Calculate(request.Items, request.Coupon);

            var charge = await _payment.Charge(request.Payment, price.Total);
            if (!charge.Succeeded)
                return OrderResult.Failed("Payment failed");

            await _inventory.Reserve(request.Items);
            await _email.SendConfirmation(request.Customer, price);

            return OrderResult.Success(charge.TransactionId);
        }
    }
}
```

Clean. Focused. One file per feature. Everything is right here. The MediatR pipeline gives you cross-cutting behaviors (validation, logging) that apply to all handlers via decorators.

And the handler body? Character for character, it's *still the same method.* Same `await`s. Same error strategy. Same missing compensation. Same mock-hell tests.

Vertical slices are about **organizing** code — grouping related things together instead of spreading them across layers. That's a real improvement in navigability. But the concern we're tracking — what vs how, intent vs process — is *within* the handler. The slice makes it easier to *find* the fused code. It doesn't *unfuse* it.

> Vertical slices are about organizing code, not about separating concerns. The concern — what vs how — is still fused in the handler.

---

## The Diagnosis

Let's step back and see what just happened.

| Architecture | What it separates | What it doesn't |
|---|---|---|
| **Clean / Hexagonal** | Core from infrastructure (project references) | Intent from process (within the core) |
| **CQRS + Events** | Reads from writes; steps into handlers | Intent from process (within each handler) |
| **Microservices** | Teams; deployment units; databases | Intent from process (within the orchestrator) |
| **Vertical Slices** | Features from each other | Intent from process (within each slice) |

Four architectures. Four proud PR approvals. **The same dirty secret in all of them**: the business logic — the *intent* — is inseparable from the infrastructure — the *process*.

The coupling isn't between layers, services, or modules. It's between *what* and *how*, and it cuts through every architecture pattern because **none of them even name it**.

This is the coupling nobody talks about. Not because it's hidden — it's in every method body you've ever written. But because every architecture pattern focuses on *structural* separation (layers, services, events, slices) and takes for granted that the code *within* each structural unit will fuse intent with process.

**The dirty secret is in every architecture. None of them even name it.**

---

## What If?

What if our code could express *just* the intent — what we want to happen — and leave the how to be decided separately?

Not separated into different layers or services. Not split across event handlers. Actually, genuinely *absent* from the code that describes the business logic.

What if the separation layer wasn't between controllers and services, or between commands and events, but between *description and execution*?

What would that even look like?

That's the next post.

---

> **Companion code**: The complete working implementation for this series is available in three languages:
> - [**C#**](/code/intent-vs-process/csharp/) — the primary language of the series, with full test suite (45 tests)
> - [**F#**](/code/intent-vs-process/fsharp/) — computation expressions make the patterns more concise (27 tests)
> - [**Haskell**](/code/intent-vs-process/haskell/) — the native habitat of these patterns, where type classes *are* Tagless Final and GADTs *are* the Free Monad (29 tests)
>
> The blog code is simplified for pedagogy. The companion code compiles, runs, and passes all tests.

---

> **Next**: [The Algebra of Intent](/2026/03/05/02-the-algebra-of-intent.html) — where we find that C# developers have been *almost* doing this for years with interfaces and DI, and we show what it looks like to do it properly.

---

*This is Part 1 of the series **"Your Clean Architecture Has a Dirty Secret."** The [full series](/tags/software-architecture/) explores separating intent from process using techniques from functional programming — Tagless Final, Free Monads, and the mathematical foundations that make them trustworthy.*
