using IntentVsProcess.Domain;

namespace IntentVsProcess.TaglessFinal.Interpreters;

// ─── Post 2: Dry-run / audit interpreter ──────────────────────────────
// Records what *would* happen without executing anything.
// Useful for compliance, auditing, and execution plan review.

public class DryRunInterpreter : IOrderAlgebra<List<AuditEntry>>
{
    public List<AuditEntry> CheckStock(List<Item> items) =>
        [new("CheckStock", $"{items.Count} item(s): {string.Join(", ", items.Select(i => i.Sku))}")];

    public List<AuditEntry> CalculatePrice(List<Item> items, Coupon? coupon) =>
        [new("CalculatePrice", coupon != null
            ? $"{items.Count} item(s) with coupon '{coupon.Code}'"
            : $"{items.Count} item(s)")];

    public List<AuditEntry> ChargePayment(PaymentMethod method, decimal amount) =>
        [new("ChargePayment", $"{amount:C} via {method}")];

    public List<AuditEntry> ReserveInventory(List<Item> items) =>
        [new("ReserveInventory", $"{items.Count} item(s)")];

    public List<AuditEntry> SendConfirmation(Customer customer, PriceResult price) =>
        [new("SendConfirmation", $"to {customer.Email} for {price.Total:C}")];

    public List<AuditEntry> Then<T>(List<AuditEntry> first, Func<T, List<AuditEntry>> next) =>
        first.Concat(next(default!)).ToList();

    public List<AuditEntry> Done(OrderResult result) =>
        [new("Done", result.ToString()!)];

    public List<AuditEntry> Guard(
        Func<bool> predicate,
        Func<List<AuditEntry>> onSuccess,
        string failureReason)
    {
        try
        {
            return predicate()
                ? onSuccess()
                : [new("Guard", $"FAILED: {failureReason}")];
        }
        catch
        {
            return onSuccess();
        }
    }
}
