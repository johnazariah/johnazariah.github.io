module FreeMonadTest (tests) where

import Test.Tasty
import Test.Tasty.HUnit

import IntentVsProcess.Domain
import IntentVsProcess.Free
import IntentVsProcess.Free.Interpreter
import IntentVsProcess.Free.Structure

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

-- ─── Behavioral tests ─────────────────────────────────────────────

behavioralTests :: TestTree
behavioralTests = testGroup "Behavioral"
  [ testCase "happy path succeeds" $ do
      let result = runPure (placeOrder happyRequest)
      result @?= Right (OrderSuccess "txn-test-001")

  , testCase "out of stock fails" $ do
      let exec step = case step of
            CheckStockF _ k -> k (StockResult False)
            other           -> runPureDefault other
      let result = runPureWith exec (placeOrder happyRequest)
      result @?= Left "Out of stock"

  , testCase "payment failed" $ do
      let exec step = case step of
            ChargePaymentF _ _ k -> k (ChargeResult False Nothing)
            other                -> runPureDefault other
      let result = runPureWith exec (placeOrder happyRequest)
      result @?= Left "Payment failed"
  ]

-- ─── Structural tests ─────────────────────────────────────────────

structuralTests :: TestTree
structuralTests = testGroup "Structural"
  [ testCase "has five steps" $
      countSteps (placeOrder happyRequest) @?= 5

  , testCase "checks stock first" $ do
      let names = flatten (placeOrder happyRequest)
      head names @?= SCheckStock

  , testCase "checks stock before charging" $
      assertBool "stock before charge" $
        appearsBefore SCheckStock SChargePayment (placeOrder happyRequest)

  , testCase "always sends confirmation" $
      assertBool "contains SendConfirmation" $
        contains SSendConfirmation (placeOrder happyRequest)

  , testCase "steps in correct order" $
      stepNames (placeOrder happyRequest) @?=
        ["CheckStock", "CalculatePrice", "ChargePayment", "ReserveInventory", "SendConfirmation"]
  ]

-- ─── LINQ / do-notation equivalents ───────────────────────────────

doNotationTests :: TestTree
doNotationTests = testGroup "Do-notation"
  [ testCase "bind chains programs" $ do
      let program :: Free OrderStepF Int
          program = do
            x <- Pure 10
            y <- Pure 20
            pure (x + y)
      case program of
        Pure n  -> n @?= 30
        _       -> assertFailure "expected Pure"

  , testCase "guard short-circuits on false" $ do
      let program :: Free OrderStepF Int
          program = do
            let x = 5 :: Int
            guard' (x > 10) "too small"
            pure x
      case program of
        Fail e  -> e @?= "too small"
        _       -> assertFailure "expected Fail"

  , testCase "guard continues on true" $ do
      let program :: Free OrderStepF Int
          program = do
            let x = 15 :: Int
            guard' (x > 10) "too small"
            pure x
      case program of
        Pure n  -> n @?= 15
        _       -> assertFailure "expected Pure"
  ]

-- ─── All tests ─────────────────────────────────────────────────────

tests :: TestTree
tests = testGroup "Free Monad (Post 3)"
  [ behavioralTests
  , structuralTests
  , doNotationTests
  ]
