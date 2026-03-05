namespace IntentVsProcess

// ═══════════════════════════════════════════════════════════════════════
// Domain — shared types across all approaches
// ═══════════════════════════════════════════════════════════════════════

type Item       = { Sku: string; Name: string; Quantity: int }
type Customer   = { Name: string; Email: string }
type Coupon     = { Code: string; DiscountPercent: decimal }

type PaymentMethod = CreditCard | DebitCard | PayPal

type OrderRequest =
    { Items: Item list; Customer: Customer
      PaymentMethod: PaymentMethod; Coupon: Coupon option }

[<Struct>] type StockResult       = { IsAvailable: bool }
[<Struct>] type PriceResult       = { Total: decimal; Subtotal: decimal; Discount: decimal }
[<Struct>] type ChargeResult      = { Succeeded: bool; TransactionId: string option }
[<Struct>] type ReservationResult = { ReservationId: string option }

type AuditEntry = { Operation: string; Details: string }

type OrderResult =
    | Success of transactionId: string
    | Failure of reason: string
    member this.Succeeded = match this with Success _ -> true | _ -> false

exception OrderFailed of string
