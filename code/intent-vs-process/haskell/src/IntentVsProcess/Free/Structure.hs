-- | Structural analysis — test the SHAPE of the program without running it.
--
-- "This is the testing equivalent of SQL EXPLAIN — verify the plan
--  without executing it."
module IntentVsProcess.Free.Structure
  ( -- * Flattening
    StepName (..)
  , flatten
  , stepNames
  , countSteps
    -- * Assertions
  , appearsBefore
  , contains
  ) where

import IntentVsProcess.Free
import IntentVsProcess.Domain (StockResult(..), PriceResult(..), ChargeResult(..), ReservationResult(..))

import Data.Text (Text)
import Data.List (elemIndex)

-- | A step name tag for structural comparison.
data StepName
  = SCheckStock
  | SCalculatePrice
  | SChargePayment
  | SReserveInventory
  | SSendConfirmation
  | SRefundPayment
  | SReleaseInventory
  deriving stock (Eq, Show)

-- | Get the name of a step.
stepName :: OrderStepF next -> StepName
stepName (CheckStockF _ _)       = SCheckStock
stepName (CalculatePriceF _ _ _) = SCalculatePrice
stepName (ChargePaymentF _ _ _)  = SChargePayment
stepName (ReserveInventoryF _ _) = SReserveInventory
stepName (SendConfirmationF {})  = SSendConfirmation
stepName (RefundPaymentF _ _)    = SRefundPayment
stepName (ReleaseInventoryF _ _) = SReleaseInventory

-- | Walk the program AST and collect step names in order.
-- Uses default dummy results to feed through continuations.
flatten :: OrderProgram a -> [StepName]
flatten (Pure _)    = []
flatten (Fail _)    = []
flatten (Free step) = stepName step : flatten (advanceDefault step)

-- | Feed a default dummy result into a step's continuation to get the next program.
advanceDefault :: OrderStepF (OrderProgram a) -> OrderProgram a
advanceDefault (CheckStockF _ k)         = k (StockResult True)
advanceDefault (CalculatePriceF _ _ k)   = k (PriceResult 99.50 99.50 0)
advanceDefault (ChargePaymentF _ _ k)    = k (ChargeResult True (Just "txn-test"))
advanceDefault (ReserveInventoryF _ k)   = k (ReservationResult (Just "res-test"))
advanceDefault (SendConfirmationF _ _ n) = n
advanceDefault (RefundPaymentF _ n)      = n
advanceDefault (ReleaseInventoryF _ n)   = n

-- | Get step names as strings.
stepNames :: OrderProgram a -> [Text]
stepNames = map nameToText . flatten
  where
    nameToText SCheckStock       = "CheckStock"
    nameToText SCalculatePrice   = "CalculatePrice"
    nameToText SChargePayment    = "ChargePayment"
    nameToText SReserveInventory = "ReserveInventory"
    nameToText SSendConfirmation = "SendConfirmation"
    nameToText SRefundPayment    = "RefundPayment"
    nameToText SReleaseInventory = "ReleaseInventory"

-- | Count the number of steps.
countSteps :: OrderProgram a -> Int
countSteps = length . flatten

-- | Does step A appear before step B?
appearsBefore :: StepName -> StepName -> OrderProgram a -> Bool
appearsBefore a b prog =
  let steps = flatten prog
  in case (elemIndex a steps, elemIndex b steps) of
       (Just ia, Just ib) -> ia < ib
       _                  -> False

-- | Does the program contain a step with this name?
contains :: StepName -> OrderProgram a -> Bool
contains name prog = name `elem` flatten prog
