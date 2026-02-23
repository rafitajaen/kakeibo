# Kakeibo - Business Constraints & Limits

## Transaction Limits

- **Amount**: 0.01 to 999,999,999.99 (per transaction)
- **Description**: Max 500 characters
- **Date**: Cannot be more than 1 year in the future

## Wallet Limits

- **Per user**: Unlimited (soft warning at 50 wallets)
- **Shared wallet members**: 2-20 members
- **Wallet name**: Max 100 characters

## Category Limits

- **System categories**: 12 (non-deletable)
- **Custom categories per user**: Unlimited (soft warning at 100)
- **Category name**: Max 50 characters

## Budget & Goal Limits

- **Active budgets per user**: Unlimited
- **Active goals per user**: Unlimited
- **Budget period**: 1 day to 5 years
- **Goal deadline**: Optional, max 10 years in future

## Recurring Pattern Limits

- **Active patterns per user**: Unlimited (soft warning at 100)
- **Frequency**: Daily, weekly, biweekly, monthly, yearly, custom
- **Pattern duration**: Max 10 years

## Collaboration Limits

- **Invitation expiry**: 7 days (configurable)
- **Pending invitations per wallet**: Max 50
- **Split types**: Equal, Percentage (must total 100%), Custom (must match amount ± $0.01)

## API Rate Limits

- **Authenticated requests**: 1000/hour per user
- **Unauthenticated requests**: 100/hour per IP
- **Transaction recording**: 100/minute per user (burst protection)

## Data Retention

- **Soft-deleted transactions**: Recoverable for 30 days, then permanent
- **Archived wallets**: Indefinite (can unarchive)
- **User account deletion**: 30-day grace period, then permanent (GDPR)
- **Audit logs**: Indefinite retention (immutable)

## Pagination

- **Transaction lists**: 50 per page
- **Wallet lists**: 50 per page
- **Category lists**: 100 per page (all at once for small lists)
- **Audit logs**: 100 per page

## Timezone Handling

- **Storage**: All timestamps in UTC (NodaTime `Instant`)
- **Display**: Converted to user's timezone preference
- **Shared wallets**: Each user sees times in their own timezone
- **Recurring "day" boundary**: Uses wallet creator's timezone as canonical

## Currency

- **MVP**: Single currency per user (selected at registration)
- **Supported**: USD, EUR, GBP, JPY, CAD, AUD, CHF, CNY, INR, BRL, MXN, etc.
- **Future (Phase 2)**: Multi-currency wallets with manual exchange rate entry
