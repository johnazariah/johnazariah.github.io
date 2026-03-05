module OptimizerTest (tests) where

import Test.Tasty
import Test.Tasty.HUnit

import IntentVsProcess.Domain
import IntentVsProcess.Free
import IntentVsProcess.Free.Optimizer

import qualified Data.Text as T

-- ─── Fixtures ──────────────────────────────────────────────────────

testItems :: [Item]
testItems =
  [ Item "SKU-001" "Widget" 2
  , Item "SKU-002" "Gadget" 1
  ]

testCustomer :: Customer
testCustomer = Customer "Alice" "alice@example.com"

happyRequest :: OrderRequest
happyRequest = OrderRequest testItems testCustomer CreditCard Nothing

-- ─── Tests ─────────────────────────────────────────────────────────

tests :: TestTree
tests = testGroup "Optimizer / Execution Plan (Post 3)"
  [ testCase "counts database calls" $ do
      let plan = analyze (placeOrder happyRequest)
      planDbCalls plan @?= 2  -- CheckStock + ReserveInventory

  , testCase "counts payment calls" $ do
      let plan = analyze (placeOrder happyRequest)
      planPaymentCalls plan @?= 1

  , testCase "counts email calls" $ do
      let plan = analyze (placeOrder happyRequest)
      planEmailCalls plan @?= 1

  , testCase "estimates nonzero latency" $ do
      let plan = analyze (placeOrder happyRequest)
      assertBool "latency > 0" $ planEstimatedMs plan > 0

  , testCase "estimates nonzero cost" $ do
      let plan = analyze (placeOrder happyRequest)
      assertBool "cost > 0" $ planEstimatedCost plan > 0

  , testCase "lists all steps" $ do
      let plan = analyze (placeOrder happyRequest)
      length (planSteps plan) @?= 5
      assertBool "mentions stock" $ any (T.isInfixOf "Check stock") (planSteps plan)
      assertBool "mentions charge" $ any (T.isInfixOf "Charge") (planSteps plan)
      assertBool "mentions reserve" $ any (T.isInfixOf "Reserve") (planSteps plan)
      assertBool "mentions confirm" $ any (T.isInfixOf "Confirm") (planSteps plan)
  ]
