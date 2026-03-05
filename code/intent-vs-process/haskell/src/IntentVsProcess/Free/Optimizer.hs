-- | Execution plan analysis — SQL EXPLAIN for your business logic.
--
-- Walk the AST and produce a report without executing anything.
-- "Show this to your boss. Show this to your compliance team."
module IntentVsProcess.Free.Optimizer
  ( ExecutionPlan (..)
  , analyze
  ) where

import IntentVsProcess.Domain
import IntentVsProcess.Free

import Data.Text (Text)
import qualified Data.Text as T

-- | An execution plan — what WOULD happen.
data ExecutionPlan = ExecutionPlan
  { planDbCalls         :: !Int
  , planPaymentCalls    :: !Int
  , planEmailCalls      :: !Int
  , planEstimatedMs     :: !Double
  , planEstimatedCost   :: !Double
  , planSteps           :: ![Text]
  } deriving stock (Eq, Show)

-- | Walk the AST and build an execution plan.
analyze :: OrderProgram a -> ExecutionPlan
analyze = go (ExecutionPlan 0 0 0 0 0 [])
  where
    go plan (Pure _)    = plan
    go plan (Fail _)    = plan
    go plan (Free step) =
      let (plan', next) = analyzeStep plan step
      in go plan' next

    analyzeStep plan step = case step of
      CheckStockF items k ->
        ( plan { planDbCalls = planDbCalls plan + 1
               , planEstimatedMs = planEstimatedMs plan + 50
               , planSteps = planSteps plan ++
                   ["DB: Check stock for " <> T.pack (show (length items)) <> " item(s)"]
               }
        , k (StockResult True)
        )
      CalculatePriceF items _ k ->
        ( plan { planEstimatedMs = planEstimatedMs plan + 10
               , planSteps = planSteps plan ++
                   ["Compute: Calculate price for " <> T.pack (show (length items)) <> " item(s)"]
               }
        , k (PriceResult 99.50 99.50 0)
        )
      ChargePaymentF method amt k ->
        ( plan { planPaymentCalls = planPaymentCalls plan + 1
               , planEstimatedMs = planEstimatedMs plan + 800
               , planEstimatedCost = planEstimatedCost plan + 0.03
               , planSteps = planSteps plan ++
                   ["API: Charge " <> T.pack (show amt) <> " via " <> T.pack (show method)]
               }
        , k (ChargeResult True (Just "txn-test-001"))
        )
      ReserveInventoryF items k ->
        ( plan { planDbCalls = planDbCalls plan + 1
               , planEstimatedMs = planEstimatedMs plan + 50
               , planSteps = planSteps plan ++
                   ["DB: Reserve " <> T.pack (show (length items)) <> " item(s)"]
               }
        , k (ReservationResult (Just "res-test-001"))
        )
      SendConfirmationF cust _ next ->
        ( plan { planEmailCalls = planEmailCalls plan + 1
               , planEstimatedMs = planEstimatedMs plan + 100
               , planEstimatedCost = planEstimatedCost plan + 0.001
               , planSteps = planSteps plan ++
                   ["Email: Confirm to " <> customerEmail cust]
               }
        , next
        )
      RefundPaymentF _ next ->
        ( plan { planSteps = planSteps plan ++ ["Compensation: Refund payment"] }
        , next
        )
      ReleaseInventoryF _ next ->
        ( plan { planSteps = planSteps plan ++ ["Compensation: Release inventory"] }
        , next
        )
