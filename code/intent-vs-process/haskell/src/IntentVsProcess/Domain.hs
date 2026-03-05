-- | Domain types shared across all approaches.
--
-- These map directly to the C# types in IntentVsProcess.Domain:
-- Item, Customer, Coupon, PaymentMethod, OrderRequest,
-- and the result types (StockResult, PriceResult, etc.)
module IntentVsProcess.Domain
  ( -- * Core domain types
    Item (..)
  , Customer (..)
  , Coupon (..)
  , PaymentMethod (..)
  , OrderRequest (..)
    -- * Result types
  , StockResult (..)
  , PriceResult (..)
  , ChargeResult (..)
  , ReservationResult (..)
  , OrderResult (..)
  , isSuccess
    -- * Utility
  , AuditEntry (..)
  ) where

import Data.Text (Text)

-- | An item in the order.
data Item = Item
  { itemSku      :: !Text
  , itemName     :: !Text
  , itemQuantity :: !Int
  } deriving stock (Eq, Show)

-- | A customer placing an order.
data Customer = Customer
  { customerName  :: !Text
  , customerEmail :: !Text
  } deriving stock (Eq, Show)

-- | A discount coupon.
data Coupon = Coupon
  { couponCode            :: !Text
  , couponDiscountPercent :: !Double
  } deriving stock (Eq, Show)

-- | Payment method.
data PaymentMethod = CreditCard | DebitCard | PayPal
  deriving stock (Eq, Show)

-- | Everything needed to place an order.
data OrderRequest = OrderRequest
  { orderItems         :: ![Item]
  , orderCustomer      :: !Customer
  , orderPaymentMethod :: !PaymentMethod
  , orderCoupon        :: !(Maybe Coupon)
  } deriving stock (Eq, Show)

-- ── Result types ────────────────────────────────────────────────

data StockResult = StockResult
  { stockIsAvailable :: !Bool
  } deriving stock (Eq, Show)

data PriceResult = PriceResult
  { priceTotal    :: !Double
  , priceSubtotal :: !Double
  , priceDiscount :: !Double
  } deriving stock (Eq, Show)

data ChargeResult = ChargeResult
  { chargeSucceeded     :: !Bool
  , chargeTransactionId :: !(Maybe Text)
  } deriving stock (Eq, Show)

data ReservationResult = ReservationResult
  { reservationId :: !(Maybe Text)
  } deriving stock (Eq, Show)

-- | The final result of an order.
data OrderResult
  = OrderSuccess !Text    -- ^ Transaction ID
  | OrderFailure !Text    -- ^ Reason
  deriving stock (Eq, Show)

isSuccess :: OrderResult -> Bool
isSuccess (OrderSuccess _) = True
isSuccess _                = False

-- | An audit log entry.
data AuditEntry = AuditEntry
  { auditOperation :: !Text
  , auditDetails   :: !Text
  } deriving stock (Eq, Show)
