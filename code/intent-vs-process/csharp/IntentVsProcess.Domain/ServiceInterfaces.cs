namespace IntentVsProcess.Domain;

// ─── Traditional service interfaces (Post 1) ─────────────────────────
// These are the four interfaces that the traditional OrderService depends on.
// Each abstracts over a single external service — but the *composition*
// of these services is baked into the OrderService method body.

public interface IInventoryService
{
    Task<StockResult> CheckStockAsync(List<Item> items);
    Task<ReservationResult> ReserveAsync(List<Item> items);
}

public interface IPricingService
{
    PriceResult Calculate(List<Item> items, Coupon? coupon);
}

public interface IPaymentGateway
{
    Task<ChargeResult> ChargeAsync(PaymentMethod method, decimal amount);
    Task RefundAsync(string transactionId);
}

public interface IEmailService
{
    Task SendConfirmationAsync(Customer customer, PriceResult price);
}
