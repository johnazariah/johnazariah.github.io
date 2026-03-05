namespace IntentVsProcess.Domain;

// ─── Core domain types ────────────────────────────────────────────────
// These are shared across all approaches (Traditional, Tagless Final, Free Monad)

/// <summary>An item in an order.</summary>
public record Item(string Sku, string Name, int Quantity);

/// <summary>A customer placing an order.</summary>
public record Customer(string Name, string Email);

/// <summary>A discount coupon.</summary>
public record Coupon(string Code, decimal DiscountPercent);

/// <summary>How the customer is paying.</summary>
public enum PaymentMethod { CreditCard, DebitCard, PayPal }

/// <summary>An incoming order request — the input to PlaceOrder.</summary>
public record OrderRequest(
    List<Item> Items,
    Customer Customer,
    PaymentMethod PaymentMethod,
    Coupon? Coupon = null);

/// <summary>
/// Unit type — represents "no meaningful return value."
/// Used where C# would normally use void, but we need an actual value.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
    public override string ToString() => "()";
}

/// <summary>An audit log entry for dry-run / compliance interpreters.</summary>
public record AuditEntry(string Operation, string Details);
