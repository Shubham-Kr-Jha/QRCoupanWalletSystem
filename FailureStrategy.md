Failure Strategy — QR Coupon Redemption & Wallet System

This document explains expected system behavior and recovery for three critical failure scenarios: DB failure, duplicate requests, and partial transactions. It complements `DecisionLog.md` and maps behavior to the concrete code.

1) DB fails (connection loss, timeouts, server down)
- What happens now
  - EF Core operations throw (SqlException / DbUpdateException). In `CouponService.RedeemCoupon` exceptions are caught, the DB transaction is rolled back (`txn.RollbackAsync()`), an error is logged, and the method returns false to the caller.
  - Controllers will return 500 unless they handle the error explicitly.
- Detection
  - Logs from service catch blocks and authentication debug logs will show the error details.


2) Duplicate request arrives (client retry / network retry)
- What happens now
  - If the client provides the same idempotency key: `RedeemCoupon` detects an existing `Transaction.ExternalId` or `Coupon.RedemptionIdempotencyKey` and returns idempotent success without double-applying.
  - If the client does not reuse the idempotency key (e.g., uses a new key): the second request is treated as a new attempt; because the coupon is already marked `Redeemed` within a serializable transaction, the second attempt will not apply another credit and will return failure.
- Why this is safe
  - Idempotency keys + in-transaction coupon marking prevent double-credit.


3) Partial transaction occurs (wallet updated but coupon not marked, or transaction created but wallet not updated)
- How it can happen
  - Partial states are rare due to DB transactions; they can occur via manual DB edits, earlier buggy code, or external actions that bypass transactional guarantees.
- Detection
  - `CouponService.ReconcileAsync` scans transactions and detects mismatches: transaction exists but coupon not marked redeemed, or wallet balance differs from sum of transactions.
- Recovery
  - Reconciliation marks the coupon redeemed (sets `RedemptionIdempotencyKey`) and recomputes the wallet balance from transactions (SUM) to correct inconsistencies. Changes are saved to the DB.
