using IntentVsProcess.Domain;

namespace IntentVsProcess.TaglessFinal.Interpreters;

// ─── Post 2: Narrative interpreter ────────────────────────────────────
// Produces a human-readable narrative of what the program *would* do.
// Useful for documentation, debugging, onboarding, and compliance.

public class NarrativeInterpreter : IOrderAlgebra<string>
{
    public string CheckStock(List<Item> items) =>
        $"Check if {items.Count} item(s) are in stock.";

    public string CalculatePrice(List<Item> items, Coupon? coupon) =>
        coupon != null
            ? $"Calculate price for {items.Count} item(s) with coupon '{coupon.Code}'."
            : $"Calculate price for {items.Count} item(s).";

    public string ChargePayment(PaymentMethod method, decimal amount) =>
        $"Charge {amount:C} via {method}.";

    public string ReserveInventory(List<Item> items) =>
        $"Reserve {items.Count} item(s) in inventory.";

    public string SendConfirmation(Customer customer, PriceResult price) =>
        $"Send confirmation email to {customer.Email} for {price.Total:C}.";

    public string Then<T>(string first, Func<T, string> next) =>
        first + "\n" + next(default!);

    public string Done(OrderResult result) =>
        result switch
        {
            OrderResult.Success s => $"Order complete (Transaction: {s.TransactionId})",
            OrderResult.Failure f => $"Order failed: {f.Reason}",
            _ => "Order complete."
        };

    public string Guard(Func<bool> predicate, Func<string> onSuccess, string failureReason)
    {
        // Always continue narrating — we want to see all steps.
        // The predicate may throw if intermediate values are default(T).
        try
        {
            return predicate() ? onSuccess() : $"[GUARD FAILED: {failureReason}]";
        }
        catch
        {
            return onSuccess();
        }
    }
}
