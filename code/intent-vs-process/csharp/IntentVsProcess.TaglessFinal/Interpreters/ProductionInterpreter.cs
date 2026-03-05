using IntentVsProcess.Domain;

namespace IntentVsProcess.TaglessFinal.Interpreters;

// ─── Post 2: Production interpreter ───────────────────────────────────
// Calls real services, produces Task<Eval>. This is the interpreter
// that runs in production — same PlaceOrder function, real I/O.
//
// Accepts the same service interfaces from Post 1, showing how
// Tagless Final wraps existing infrastructure without replacing it.

public class ProductionInterpreter(
    IInventoryService inventory,
    IPricingService pricing,
    IPaymentGateway payment,
    IEmailService email) : IOrderAlgebra<Task<Eval>>
{
    private readonly IInventoryService _inventory = inventory;
    private readonly IPricingService _pricing = pricing;
    private readonly IPaymentGateway _payment = payment;
    private readonly IEmailService _email = email;

    public async Task<Eval> CheckStock(List<Item> items)
    {
        var result = await _inventory.CheckStockAsync(items);
        return Eval.Of(result);
    }

    public Task<Eval> CalculatePrice(List<Item> items, Coupon? coupon) =>
        Task.FromResult(Eval.Of(_pricing.Calculate(items, coupon)));

    public async Task<Eval> ChargePayment(PaymentMethod method, decimal amount)
    {
        var result = await _payment.ChargeAsync(method, amount);
        return Eval.Of(result);
    }

    public async Task<Eval> ReserveInventory(List<Item> items)
    {
        var result = await _inventory.ReserveAsync(items);
        return Eval.Of(result);
    }

    public async Task<Eval> SendConfirmation(Customer customer, PriceResult price)
    {
        await _email.SendConfirmationAsync(customer, price);
        return Eval.Of(Unit.Value);
    }

    public async Task<Eval> Then<T>(Task<Eval> first, Func<T, Task<Eval>> next)
    {
        var result = await first;
        if (result.IsFailure) return result;
        return await next(result.Unwrap<T>());
    }

    public Task<Eval> Done(OrderResult result) =>
        Task.FromResult(Eval.Of(result));

    public async Task<Eval> Guard(
        Func<bool> predicate,
        Func<Task<Eval>> onSuccess,
        string failureReason) =>
        predicate() ? await onSuccess() : Eval.Failed(failureReason);
}
