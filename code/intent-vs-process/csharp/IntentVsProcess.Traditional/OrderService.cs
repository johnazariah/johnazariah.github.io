using IntentVsProcess.Domain;

namespace IntentVsProcess.Traditional;

// ─── Post 1: The traditional OrderService ─────────────────────────────
// This is the "code everyone approves." SOLID, DI, interface segregation.
// But the *what* (intent) and *how* (process) are inseparable.
//
// See the blog post for why this is a problem:
// - Testing requires mocking 4 dependencies
// - Changing infrastructure rewrites the business logic
// - Cross-cutting concerns (logging, retry, tracing) invade the method
// - Compensation / saga logic doubles the method size

public class OrderService(
    IInventoryService inventory,
    IPricingService pricing,
    IPaymentGateway payment,
    IEmailService email)
{
    private readonly IInventoryService _inventory = inventory;
    private readonly IPricingService _pricing = pricing;
    private readonly IPaymentGateway _payment = payment;
    private readonly IEmailService _email = email;

    /// <summary>
    /// The five-step order flow — validate, price, charge, reserve, notify.
    /// Clear intent. But also decides: sync/async, error strategy, execution
    /// order, protocol, observability, failure semantics. Knows too much.
    /// </summary>
    public async Task<OrderResult> PlaceOrder(OrderRequest request)
    {
        // 1. Validate — check inventory
        var stock = await _inventory.CheckStockAsync(request.Items);
        if (!stock.IsAvailable)
            return OrderResult.Failed("Out of stock");

        // 2. Price — calculate the total
        var price = _pricing.Calculate(request.Items, request.Coupon);

        // 3. Charge — take payment
        var charge = await _payment.ChargeAsync(request.PaymentMethod, price.Total);
        if (!charge.Succeeded)
            return OrderResult.Failed("Payment failed");

        // 4. Reserve — hold the items
        await _inventory.ReserveAsync(request.Items);

        // 5. Notify — send a confirmation
        await _email.SendConfirmationAsync(request.Customer, price);

        return OrderResult.Ok(charge.TransactionId!);
    }

    /// <summary>
    /// PlaceOrder with compensation — showing how saga logic
    /// doubles the method size. Same five steps, now with try/catch/refund.
    /// The business intent didn't change. The code doubled.
    /// </summary>
    public async Task<OrderResult> PlaceOrderWithCompensation(OrderRequest request)
    {
        var stock = await _inventory.CheckStockAsync(request.Items);
        if (!stock.IsAvailable)
            return OrderResult.Failed("Out of stock");

        var price = _pricing.Calculate(request.Items, request.Coupon);

        var charge = await _payment.ChargeAsync(request.PaymentMethod, price.Total);
        if (!charge.Succeeded)
            return OrderResult.Failed("Payment failed");

        try
        {
            await _inventory.ReserveAsync(request.Items);
        }
        catch
        {
            // Compensation: refund the charge
            await _payment.RefundAsync(charge.TransactionId!);
            throw;
        }

        await _email.SendConfirmationAsync(request.Customer, price);

        return OrderResult.Ok(charge.TransactionId!);
    }
}
