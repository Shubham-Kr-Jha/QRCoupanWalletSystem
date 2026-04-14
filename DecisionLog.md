Decision Log — QR Coupon Redemption & Wallet System

Purpose
This document records assumptions, key design decisions, edge cases handled, known limitations, and trade-offs. It maps each point to code locations so reviewers can verify behavior quickly.

Assumptions
- Single currency and credit-only coupons: `Models/Coupon.Amount`, `Models/Wallet.Balance`.
- Coupons are unique by `Coupon.Code` (enforced by EF unique index).
- Clients may supply an idempotency key; the server will generate one when absent.
- Users have a simple role string `User.Role` with values "User" or "Admin".

Implemented behavior (where to inspect)
- Authentication & roles: JWT tokens include role claim. See `Services/AuthService.cs` and `Controllers/AuthController.cs`.
- Redemption: `Controllers/CouponController.cs` -> `Services/CouponService.RedeemCoupon` performs an atomic redeem inside a serializable DB transaction (creates Transaction, updates Wallet.Balance, marks Coupon).
- Idempotency: recorded in `Transaction.ExternalId` and `Coupon.RedemptionIdempotencyKey`. Logic in `CouponService.RedeemCoupon` prevents duplicate processing when the same key is reused.
- Reconciliation: `POST /api/admin/reconcile` -> `CouponService.ReconcileAsync()` repairs inconsistencies and recomputes wallet balances. See `Controllers/AdminController.cs`, `Services/CouponService.cs`.
- Soft-delete & filters: `IsDeleted`/`DeletedAt` fields and EF global query filters in `Data/AppDbContext.cs`.
- Production improvements: decimal precision set with `HasPrecision(18,2)` and `Transaction.CouponId` FK linking configured in `Data/AppDbContext.cs` and applied via migrations.

Edge cases handled
- Duplicate requests: If client reuses the same idempotency key, the system detects existing `Transaction.ExternalId` or `Coupon.RedemptionIdempotencyKey` and returns idempotent success without double-credit.
- Concurrent requests: The redeem operation uses a serializable DB transaction so concurrent attempts cannot both succeed.
- Partial failures: The reconciliation endpoint repairs cases where transactions exist but coupons are not marked redeemed or wallet balances are inconsistent.
- Expired/invalid coupons: `RedeemCoupon` verifies campaign window and coupon status before applying.
- Duplicate registration: `AuthService.Register` enforces unique email addresses.

Trade-offs
- Prioritized correctness, traceability, and simplicity: ACID transactions + idempotency + reconciliation. Left out complex architectures (CQRS, event sourcing) to keep scope focused and reviewable.

Quick verification (endpoints)
- Register: `POST /api/auth/register`
- Login: `POST /api/auth/login` (returns JWT with role claim)
- Redeem: `POST /api/coupon/redeem` body `{ "code": "COUPONCODE" }` (server generates idempotency key if not provided)
- Admin: `POST /api/admin/campaigns`, `POST /api/admin/campaigns/{id}/generate`, `POST /api/admin/reconcile` (Admin role required)

Migrations
- `Migrations/` includes `InitialCreate`, `AddUserRole`, `LinkTransactionToCouponAndPrecision`.





