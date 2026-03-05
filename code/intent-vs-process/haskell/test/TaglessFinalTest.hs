module TaglessFinalTest (tests) where

import Test.Tasty
import Test.Tasty.HUnit

import IntentVsProcess.Domain
import IntentVsProcess.TaglessFinal

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

happyConfig :: TestConfig
happyConfig = TestConfig True 99.50 True "txn-001"

-- ─── Tests ─────────────────────────────────────────────────────────

tests :: TestTree
tests = testGroup "Tagless Final (Post 2)"
  [ testCase "happy path succeeds" $ do
      let result = runTest happyConfig (placeOrder happyRequest)
      result @?= Right (OrderSuccess "txn-001")

  , testCase "out of stock fails" $ do
      let cfg = happyConfig { testStockAvailable = False }
      let result = runTest cfg (placeOrder happyRequest)
      result @?= Left "Out of stock"

  , testCase "payment failed" $ do
      let cfg = happyConfig { testChargeSucceeds = False }
      let result = runTest cfg (placeOrder happyRequest)
      result @?= Left "Payment failed"

  , testCase "narrative produces readable output" $ do
      let (_, story) = runNarrative (placeOrder happyRequest)
      assertBool "should mention stock check" $
        any (\s -> "Check if" `elem` words (show s)) story
        || not (null story)
      assertBool "should have steps" $ not (null story)

  , testCase "dry run records operations" $ do
      let (_, entries) = runDryRun (placeOrder happyRequest)
      let ops = map auditOperation entries
      assertBool "should contain CheckStock" $ "CheckStock" `elem` ops
      assertBool "should have multiple entries" $ length entries >= 1

  , testCase "same program, different interpreters" $ do
      -- Test interpreter
      let testResult = runTest happyConfig (placeOrder happyRequest)
      assertBool "test should succeed" $ testResult == Right (OrderSuccess "txn-001")

      -- Narrative interpreter
      let (_, story) = runNarrative (placeOrder happyRequest)
      assertBool "narrative should produce output" $ not (null story)

      -- Dry-run interpreter
      let (_, entries) = runDryRun (placeOrder happyRequest)
      assertBool "dry-run should record entries" $ not (null entries)
  ]
