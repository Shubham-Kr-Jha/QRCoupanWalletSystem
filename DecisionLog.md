Decision Log - QR Coupon Redemption & Wallet System

1. Architecture
- Chosen: .NET 8 Web API with EF Core and SQL Server.
- Rationale: Familiar stack for enterprise backend. Clean layering with Controllers, Services, Data, Models.

2. Authentication
- JWT used for simplicity. Token contains user id and email.

3. Coupon redemption correctness
- Use DB transactions (Serializable isolation) to avoid double redemption and race conditions.
- Mark coupon with RedemptionIdempotencyKey and add Transaction.ExternalId to support idempotency.
- Before applying wallet update, check if a transaction with same idempotency key exists to avoid duplicates.

4. Partial failures & Reconciliation
- Partial failure scenario (wallet updated but coupon not marked) is addressed by reconciliation endpoint.
- Reconcile scans transactions referencing coupon redemption (by Note) and fixes coupon.redeemed and wallet balances by recomputing wallet total from transactions.
- This is simple but effective for the assignment. In production, background job and more robust linking would be used.

5. Data integrity
- Wallet balance updated inside DB transaction and recomputed during reconciliation.
- Transactions include ExternalId for traceability.
- No negative balance logic implemented (business rules should specify if debit allowed).

6. Idempotency
- Clients must send an idempotency key when redeeming. Server checks both coupon.RedemptionIdempotencyKey and existing transactions by ExternalId.

7. Concurrency
- Serializable isolation is used for redeem operation to ensure only one concurrent redeemer succeeds.

8. Logging & Errors
- Basic logging added in services.

9. Simplifications
- Password stored as BCrypt hash.
- No roles implemented for admin; admin endpoints are open for the assignment but can be protected.

10. Next steps (not implemented due to scope)
- Background worker for periodic reconciliation.
- More robust idempotency (dedicated table), event sourcing or CQRS for scaling.
- Integration tests and migration scripts.


End of Decision Log
