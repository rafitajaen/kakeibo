# Kakeibo Platform

> Technology-agnostic business domain reference for the Kakeibo platform. This document describes the modular architecture, module responsibilities, and inter-module communication patterns.

---

## Table of Contents

1. [About Kakeibo Platform](#1-about-kakeibo-platform)
2. [Platform Overview](#2-platform-overview)
3. [Applications](#3-applications)
4. [Core Modules](#4-core-modules)
5. [Business Modules](#5-business-modules)
6. [Module Dependency Matrix](#6-module-dependency-matrix)
7. [Inter-Module Communication](#7-inter-module-communication)
8. [Key Flows](#8-key-flows)
9. [Service Dependency Diagram](#9-service-dependency-diagram)

---

## 1. About Kakeibo Platform

Kakeibo is a personal finance and shared expense management platform inspired by the traditional Japanese household budgeting method created by Hani Motoko in 1904. The word "kakeibo" (家計簿) translates to "household financial ledger" and represents a philosophy of conscious spending through reflection and planning.

**Access Model**: Kakeibo is a personal finance platform with open registration. Users operate within isolated financial environments while maintaining the ability to collaborate with others through shared contexts. A super admin can control new registrations, block users, or manage accounts as needed.

**Core Philosophy**: Kakeibo embodies three fundamental principles:
- **Conscious Spending**: Every transaction is an opportunity for awareness
- **Reflection Through Categorization**: Understanding spending patterns through systematic classification
- **Savings Through Awareness**: Financial health begins with seeing clearly

**Modern Adaptation**: While honoring traditional principles, Kakeibo adapts to contemporary needs with digital convenience, collaborative contexts, automation support, and intelligent forecasting.

---

## 2. Platform Overview

### 2.1 Module Catalog

| # | Module | Type | Description |
|---|--------|------|-------------|
| 1 | Identity | Core | Authentication, user accounts, sessions, password recovery |
| 2 | Notifications | Core | Multi-channel notifications (email, push, in-app), templates, preferences |
| 3 | Auditing | Core | Activity logs, audit trail, immutable event recording |
| 4 | Wallets | Business | Personal and shared wallet management, balance tracking, invitations, splits, debts, settlements |
| 5 | Transactions | Business | Income, expense, transfer recording, categorization (12 system + unlimited custom) |
| 6 | Budgets | Business | Spending limits, budget monitoring, alerts |
| 7 | Goals | Business | Savings targets, progress tracking, milestones |
| 8 | Recurring | Business | Pattern management, automatic transaction generation |

**Total: 8 modules** (3 core platform modules + 5 business modules).

> **Consolidation from original 10 modules:**
> - `Collaboration` merged into `Wallets` (collaboration features only exist for shared wallets)
> - `Categories` merged into `Transactions` (categories only exist to classify transactions)

### 2.2 Module Optionality

Not every user needs all features. The table below classifies modules by optionality tier:

| Tier | Module | Required when... |
|------|--------|------------------|
| **1 — Platform Core** | Identity | Always |
| **1 — Platform Core** | Notifications | Always |
| **1 — Platform Core** | Auditing | Always |
| **2 — Financial Core** | Wallets | Always (personal + shared wallets, collaboration features) |
| **2 — Financial Core** | Transactions | Always (recording + categorization) |
| **3 — Planning** | Budgets | User wants spending limits and monitoring |
| **3 — Planning** | Goals | User wants savings targets and progress tracking |
| **3 — Planning** | Recurring | User wants automated transaction generation |

> **Tier 1–2 modules** are required for basic functionality. **Tier 3 modules** are optional enhancements that can be enabled per user or per deployment.

### 2.3 Cross-Cutting Requirements

- **Full audit trail**: Every action logged (who, what, when)
- **Multi-language support (i18n)**: From day 1
- **Privacy by default, sharing by choice**: Complete user isolation except in shared contexts
- **Balance accuracy**: Non-negotiable — balances, debts, and calculations must always be correct

### 2.4 User Model

**User Types**:

| Type | Description |
|------|-------------|
| Individual | User managing personal finances only |
| Collaborator | User participating in one or more shared wallets |

**Isolation Architecture**: Each user operates within a completely isolated financial environment. One user cannot see, access, or affect another user's personal financial data unless explicitly invited to a shared context.

This isolation ensures:
- **Privacy**: Personal finances remain private
- **Security**: Financial data is protected from unauthorized access
- **Independence**: Each user's experience is unaffected by others
- **Simplicity**: Straightforward permission model focused on user isolation and collaborative sharing

**Collaboration Model**: When users share financial responsibilities, they create shared wallets that exist as separate spaces within each participating user's environment. Shared contexts have these characteristics:

- **Equal Rights**: All participants have identical permissions—no "owner" or "administrator" hierarchy exists
- **Symmetric Visibility**: All participants see the same information in a shared wallet
- **Independent Departure**: Any member can leave a shared wallet at any time
- **Invitation-Based**: New members join shared wallets only through explicit invitation

**Permission Model**: The permission model is intentionally simple:

- **Personal Contexts**: Full control over own wallets, transactions, budgets, goals, categories, and recurring patterns
- **Shared Contexts**: Equal rights for all members. No hierarchy, no role-based restrictions, no administrative privileges
- **Invitation Authority**: Any member of a shared wallet can invite new members

### 2.5 Communication Patterns

| Channel | Use |
|---------|-----|
| In-app notifications | Real-time alerts for budget warnings, invitations, milestones |
| Email | Optional — transactional confirmations and reminders |
| Push notifications | Mobile app alerts (budget warnings, goal milestones, invitation activity) |

### 2.6 Design Decisions

Key architectural decisions made during platform design:

**Categorization in Shared Wallets**: Transaction-owned (single category per transaction). All users in shared wallet see same category. Rejected alternative: user-specific category views (too complex, breaks budgets/reports).

**Multi-Currency**: Single-currency MVP. User selects currency at registration. All wallets use that currency. Multi-currency support deferred to Phase 2.

**Email Service**: Separate microservice (Bun + Hono + React Email) for independent template development.

**Storage**: RustFS alpha.83 (S3-compatible). Known limitation: SSE broken (see tech-stack.md). Risk accepted for MVP.

**Background Processing**: Outbox Pattern (event reliability) + Hangfire (scheduled jobs). Both maintained for different use cases.

**Households**: Deferred to post-MVP. Shared wallets sufficient for MVP.

**Backups**: Not implemented in MVP (user choice). Risk accepted - no backup strategy (see infrastructure.md).

---

## 3. Applications

### 3.1 Web App (PWA/SPA)

**Technology**: Vue.js (PWA-capable)
**Access**: Browser-based, installable as Progressive Web App
**Features**: Full platform access for all user types

| Area | Features |
|------|----------|
| Dashboard | Balance overview, recent transactions, budget status, goal progress |
| Wallets | Create, view, archive personal and shared wallets |
| Transactions | Record income, expense, transfer; view history; categorize |
| Budgets | Create budgets, monitor spending, receive warnings |
| Goals | Create savings goals, track progress, view milestones |
| Recurring | Define patterns, forecast future transactions |
| Collaboration | Manage invitations, view debts, record settlements |
| Profile | User settings, notification preferences, privacy controls |

### 3.2 API

**Technology**: .NET
**Purpose**: Backend for web and mobile apps
**Architecture**: Modular monolith with vertical slices

The API is organized into modules that correspond 1:1 with the business modules described in this document. Each module is self-contained with its own database schema, domain entities, and endpoints. Modules communicate through integration events and module requests, not direct references.

### 3.3 Email Service

Independent microservice for email template rendering and delivery.

**Technology**: Bun + Hono + React Email
**Purpose**: Server-side rendering of transactional and notification email templates
**Architecture**: Standalone HTTP service consumed by the API

| Component | Description |
|-----------|-------------|
| Template engine | React Email components for type-safe, reusable email templates |
| Rendering API | HTTP endpoints for rendering templates with dynamic data |
| Multi-language | i18n support for email templates (matching platform languages) |
| Preview mode | Development endpoint for template visualization |

**Integration**: The API module calls the email service to render templates before sending them via the configured SMTP provider. This separation allows email templates to be developed and tested independently from the main API.

---

## 4. Core Modules

### 4.1 Identity

Centralized management of authentication and user accounts.

| Feature | Description |
|---------|-------------|
| User registration | Sign-up with email and password |
| Email verification | Required to activate account |
| OAuth login | Google, Apple |
| JWT tokens | Access token + refresh token |
| Session management | Control of active sessions per user |
| Password recovery | Secure flow with temporary token |

**Relationships**: All modules depend on Identity to validate user identity and permissions.

### 4.2 Notifications

Multi-channel delivery of communications to users.

| Feature | Description |
|---------|-------------|
| Multi-channel | Email, push, in-app |
| Templates | Predefined messages with dynamic variables |
| User preferences | Opt-in/opt-out per notification type |
| Grouping | Avoid saturation by grouping related notifications |
| Tracking | Delivery and read status |

**Notification Types**:

| Type | Examples |
|------|----------|
| Transactional | Invitation sent, invitation accepted, settlement recorded |
| Reminder | Budget nearing limit, goal deadline approaching |
| Alert | Budget exceeded, recurring transaction generated |
| Informational | Member joined shared wallet, goal milestone reached |

**Relationships**: All modules emit notifications.

### 4.3 Auditing

Immutable record of all actions on the platform.

| Feature | Description |
|---------|-------------|
| Automatic logging | Action capture without manual intervention |
| Full context | User, timestamp, action type, affected entity |
| Change diff | Before and after state for updates |
| Immutability | Records cannot be modified or deleted |
| Query and filtering | Search by user, date, action type |
| Export | Audit report generation |

**Event Types**: Authentication (login, logout), CRUD (create, update, delete), Transaction (income, expense, transfer, settlement), Collaboration (invitation, member join/leave).

**Relationships**: All modules emit audit events.

---

## 5. Business Modules

### 5.1 Wallets (includes Collaboration Features)

Management of financial containers (personal and shared) and collaborative expense features.

| Feature | Description |
|---------|-------------|
| Wallet creation | Personal or shared wallet types |
| Balance tracking | Current, historical, projected balances |
| Wallet types | Personal (single owner), Shared (multiple members) |
| Archiving | Hide from daily view while preserving data |
| Wallet metadata | Name, currency, current balance, member list (shared only) |
| Invitations | Create, send, accept, expire, revoke (shared wallet access) |
| Splits | Equal, Percentage, Custom division of expenses |
| Debt calculation | Automatic calculation from transactions + splits |
| Debt simplification | Minimize number of debts shown |
| Settlements | Record external payments (don't affect wallet balance) |

**Critical Invariants**:
- Wallet balance must always equal the sum of transaction impacts
- Personal wallets have exactly one owner
- Shared wallets have one or more members
- Debts are calculated automatically from transaction history, never manually set
- Settlement amount cannot exceed current debt between two members
- All shared wallet members see identical debt information (symmetric visibility)

**Integration Points**:
- Publishes `WalletCreatedEvent` → consumed by Auditing, Notifications
- Publishes `InvitationSentEvent` → consumed by Notifications
- Publishes `InvitationAcceptedEvent` → consumed by Auditing, Notifications
- Publishes `MemberJoinedEvent` → consumed by Auditing, Notifications
- Publishes `SettlementRecordedEvent` → consumed by Auditing, Notifications
- Listens to `TransactionRecordedEvent` from Transactions → recalculates debts
- Listens to `TransactionUpdatedEvent` from Transactions → recalculates debts
- Listens to `TransactionDeletedEvent` from Transactions → recalculates debts
- Handles `GetWalletMembersRequest` from other modules
- Handles `ValidateInvitationRequest` from other modules
- Sends `GetWalletBalanceRequest` to Transactions module (for balance display — balance is owned by Transactions)

### 5.2 Transactions (includes Categories)

Recording and tracking of financial events with classification system.

| Feature | Description |
|---------|-------------|
| Transaction types | Income, Expense, Transfer |
| Recording | Amount, date, description, category, wallet(s) |
| History | Chronological ledger per wallet |
| Balance updates | Automatic balance recalculation on save |
| Categorization | Every transaction belongs to exactly one category |
| Editing | Update any field (recalculates balance) |
| Deletion | Soft delete (preserves audit trail, reverses balance impact) |
| System categories | 12 built-in categories (Housing, Transportation, Food & Dining, etc.) |
| Custom categories | Unlimited user-created categories |
| Category management | Create, rename, archive custom categories |

**System Categories** (non-deletable):

1. Housing (rent, mortgage, utilities)
2. Transportation (fuel, maintenance, public transit)
3. Food & Dining (groceries, restaurants)
4. Health & Wellness (medical, fitness)
5. Entertainment & Leisure (hobbies, recreation)
6. Shopping & Personal (clothing, personal care)
7. Education (courses, books, supplies)
8. Subscriptions & Bills (streaming, memberships)
9. Savings & Investments (transfers to savings, investment contributions)
10. Debt & Loans (loan payments, interest)
11. Gifts & Donations (presents, charitable giving)
12. Other (miscellaneous)

**Critical Invariants**:
- Every transaction must have exactly one category
- Transfer transactions affect two wallets (source and destination)
- Balance impact is atomic — balance stored in `WalletBalance` entity updated in same `SaveChangesAsync()` call as the transaction (both wallets for transfers in a single DB transaction)
- Balance lives in the Transactions module (`transactions.wallet_balances` table), not in Wallets module

**Integration Points**:
- Publishes `TransactionRecordedEvent` → consumed by Wallets (debt calc), Budgets, Goals, Auditing
- Publishes `TransactionUpdatedEvent` → consumed by Wallets (debt calc), Budgets, Goals, Auditing
- Publishes `TransactionDeletedEvent` → consumed by Wallets (debt calc), Budgets, Goals, Auditing
- Handles `GetTransactionsInPeriodRequest` from Budgets module
- Handles `GetCategoryByIdRequest` from other modules
- Handles `GetWalletBalanceRequest` from Goals and Wallets modules (balance owned by Transactions)

### 5.3 Budgets

Spending limit management and monitoring.

| Feature | Description |
|---------|-------------|
| Budget creation | Category, time period, limit amount, wallet(s) to monitor |
| Wallet monitoring | Single wallet, multiple wallets, or all wallets |
| Spending tracking | Current spending vs. limit |
| Percentage used | Visual progress toward limit |
| Alerts | Warnings when nearing or exceeding limit |
| Projected overage | Forecast based on daily average spending |
| Historical performance | Past budget periods and results |

**Flexibility**: Budgets can monitor:
- Single personal wallet
- Single shared wallet
- Multiple wallets combined (e.g., "Food & Dining across all sources")
- All wallets

**Budget Status Calculation**:
- **On track**: Spending ≤ expected pace for time period
- **Warning**: Spending > expected pace but < limit
- **Exceeded**: Spending ≥ limit

**Integration Points**:
- Listens to `TransactionRecordedEvent` from Transactions → updates spending
- Publishes `BudgetExceededEvent` → consumed by Notifications
- Publishes `BudgetWarningEvent` → consumed by Notifications
- Sends `GetTransactionsInPeriodRequest` to Transactions module

### 5.4 Goals

Savings target tracking and progress monitoring.

| Feature | Description |
|---------|-------------|
| Goal creation | Name, target amount, deadline (optional), linked wallet (optional) |
| Tracking modes | Wallet-linked, cross-wallet, manual |
| Progress monitoring | Current progress, percentage complete, time remaining |
| Milestones | Notifications at 25%, 50%, 75%, 100% |
| Projected completion | Forecast based on savings rate |

**Tracking Modes**:

| Mode | Description | Use Case |
|------|-------------|----------|
| Wallet-linked | Tracks balance growth in specific wallet | "Vacation Fund" savings account |
| Cross-wallet | Tracks total across all wallets | "Net worth growth" goal |
| Manual | User updates progress manually | "Pay off credit card" (external account) |

**Integration Points**:
- Listens to `TransactionRecordedEvent` from Transactions → updates progress (wallet-linked mode)
- Publishes `GoalMilestoneReachedEvent` → consumed by Notifications
- Publishes `GoalAchievedEvent` → consumed by Notifications
- Sends `GetWalletBalanceRequest` to Transactions module (balance is owned by Transactions)

### 5.5 Recurring

Automated transaction pattern management.

| Feature | Description |
|---------|-------------|
| Pattern creation | Transaction template + schedule |
| Recurrence rules | Daily, weekly, monthly, yearly, custom |
| Auto-generation | Background job creates transactions on schedule |
| Forecast visibility | Projected transactions for next 30/90 days |
| Projected balance | Balance forecast based on recurring patterns |
| Pattern editing | Update template or schedule for future occurrences |
| Pattern deletion | Stop future generation, preserves past transactions |

**Recurrence Rules**:

| Rule | Examples |
|------|----------|
| Daily | Every day, every weekday, every N days |
| Weekly | Every Monday, every 2 weeks on Friday |
| Monthly | 1st of month, last day of month, 15th and last day |
| Yearly | January 1st, December 25th |
| Custom | Complex patterns (e.g., biweekly on payday) |

**Auto-Generation Process**:

1. Background job runs daily (or more frequently)
2. Scans all active recurring patterns
3. For each pattern due today, creates a transaction
4. Marks pattern occurrence as "generated"
5. User receives notification of auto-generated transaction

**User Review**:
- Most auto-generated transactions are correct and require no action
- User can edit amounts that vary (e.g., utility bills)
- User can delete transactions that didn't occur (e.g., gym closed for holiday)

**Integration Points**:
- Publishes `RecurringTransactionGeneratedEvent` → consumed by Notifications
- Creates transactions via Transactions module (standard transaction recording flow)

---

## 6. Module Dependency Matrix

| Module | Depends on | Consumed by |
|--------|-----------|-------------|
| Identity | — | All modules |
| Notifications | Identity | All modules (emit notifications) |
| Auditing | Identity | All modules (emit audit events) |
| **Wallets** (includes Collaboration) | Identity | Transactions, Budgets, Goals, Recurring |
| **Transactions** (includes Categories) | Identity, Wallets | Wallets (debt calc), Budgets, Goals, Recurring (via auto-generation) |
| Budgets | Identity, Wallets, Transactions | Notifications |
| Goals | Identity, Wallets, Transactions | Notifications |
| Recurring | Identity, Wallets, Transactions | — |

**Dependency Flow** (simplified):

```
Identity (foundation)
    ↓
Wallets (includes Collaboration)
    ↓
Transactions (includes Categories)
    ↓
Budgets + Goals + Recurring
    ↓
Notifications (all modules emit)
Auditing (all modules log)
```

---

## 7. Inter-Module Communication

### 7.1 Design Principles

| Principle | Description |
|-----------|-------------|
| Decoupling | Modules are independent of each other |
| Event-driven | Modules communicate through integration events, not direct calls |
| Request/response | Synchronous queries via module request pattern |
| No direct references | Module A NEVER references Module B's project |

### 7.2 Event Catalog

Integration events published by each module and their subscribers:

| Emitting Module | Event | Payload | Subscribed Modules |
|----------------|-------|---------|-------------------|
| Identity | `UserRegisteredEvent` | UserId, Email, RegisteredAt | Auditing, Notifications |
| Identity | `UserLoggedInEvent` | UserId, IpAddress, UserAgent, LoggedInAt | Auditing |
| Identity | `UserLoggedOutEvent` | UserId, SessionId, LoggedOutAt | Auditing |
| **Wallets** | `WalletCreatedEvent` | WalletId, UserId, Name, Type, CreatedAt | Auditing, Notifications |
| **Wallets** | `WalletArchivedEvent` | WalletId, UserId, ArchivedAt | Auditing |
| **Wallets** | `InvitationSentEvent` | InvitationId, WalletId, InviterUserId, InviteeEmail, SentAt | Notifications |
| **Wallets** | `InvitationAcceptedEvent` | InvitationId, WalletId, UserId, AcceptedAt | Auditing, Notifications |
| **Wallets** | `MemberJoinedEvent` | WalletId, UserId, JoinedAt | Auditing, Notifications |
| **Wallets** | `MemberLeftEvent` | WalletId, UserId, LeftAt | Auditing, Notifications |
| **Wallets** | `SettlementRecordedEvent` | SettlementId, WalletId, FromUserId, ToUserId, Amount, RecordedAt | Auditing, Notifications |
| **Transactions** | `TransactionRecordedEvent` | TransactionId, WalletId, Type, Amount, CategoryId, Date, UserId | Wallets (debt calc), Budgets, Goals, Auditing |
| **Transactions** | `TransactionUpdatedEvent` | TransactionId, WalletId, OldValues, NewValues, UpdatedAt, UserId | Wallets (debt calc), Budgets, Goals, Auditing |
| **Transactions** | `TransactionDeletedEvent` | TransactionId, WalletId, DeletedAt, UserId | Wallets (debt calc), Budgets, Goals, Auditing |
| Budgets | `BudgetExceededEvent` | BudgetId, UserId, CategoryId, Limit, CurrentSpending | Notifications |
| Budgets | `BudgetWarningEvent` | BudgetId, UserId, CategoryId, Limit, CurrentSpending, PercentUsed | Notifications |
| Goals | `GoalMilestoneReachedEvent` | GoalId, UserId, MilestonePercent, CurrentProgress, TargetAmount | Notifications |
| Goals | `GoalAchievedEvent` | GoalId, UserId, TargetAmount, AchievedAt | Notifications |
| Recurring | `RecurringTransactionGeneratedEvent` | RecurringPatternId, TransactionId, UserId, GeneratedAt | Notifications |

### 7.3 Request/Response Patterns

Synchronous module requests for cross-module data queries:

| Requesting Module | Request | Handling Module | Response Type |
|------------------|---------|----------------|---------------|
| Any | `GetWalletMembersRequest(WalletId)` | **Wallets** | `List<UserId>` |
| Budgets | `GetTransactionsInPeriodRequest(WalletId, CategoryId, StartDate, EndDate)` | **Transactions** | `List<TransactionSummaryDto>` |
| Goals, Wallets | `GetWalletBalanceRequest(WalletId)` | **Transactions** | `decimal Balance` |
| Budgets | `GetCategoryByIdRequest(CategoryId)` | **Transactions** | `CategoryDto` |
| Any | `ValidateInvitationRequest(InvitationCode)` | **Wallets** | `InvitationStatus` |

### 7.4 Communication Strategy

**Use Integration Events when**:
- The caller does not need an immediate response
- Multiple modules need to react to the same event
- The action should be asynchronous and decoupled
- Example: `TransactionRecordedEvent` triggers debt recalculation (Collaboration), spending update (Budgets), and progress update (Goals)

**Use Module Requests when**:
- The caller needs data synchronously to proceed
- Only one module can provide the data
- The operation should block if the data is unavailable
- Example: Budgets needs transaction history from Transactions to calculate current spending

---

## 8. Key Flows

These narrative descriptions show how modules work together to fulfill user journeys.

### 8.1 Flow 1: Getting Started

**Modules involved**: Identity → Wallets → Transactions → Auditing

**Sequence**:

1. **Identity**: User registers account (email + password)
   - Publishes `UserRegisteredEvent`
   - Auditing logs registration
   - Notifications sends welcome email

2. **Wallets**: User creates first personal wallet ("Checking Account")
   - Publishes `WalletCreatedEvent`
   - Auditing logs wallet creation
   - Notifications confirms wallet created

3. **Transactions**: User records first expense (coffee, $4.50, Food & Dining)
   - Publishes `TransactionRecordedEvent`
   - Wallets updates balance (atomic)
   - Auditing logs transaction
   - Budgets updates spending (if budget exists)
   - Goals updates progress (if goal linked)

**Outcome**: User has functional financial tracking with accurate balances and growing transaction history.

---

### 8.2 Flow 2: Daily Tracking

**Modules involved**: Transactions → Categories → Wallets → Auditing

**Morning**:
- Transactions records grocery shopping ($65 expense, Food & Dining category)
- Transactions records gas ($40 expense, Transportation category)
- Wallets updates balances atomically for each transaction

**Midday**:
- Transactions records paycheck ($2,000 income, Salary custom category)
- Transactions records transfer to savings ($500 transfer from Checking to Savings)
- Wallets updates both wallet balances atomically

**Evening**:
- Transactions records dinner ($45 expense, Food & Dining category)
- User views dashboard: Wallets shows current balances for all wallets

**Outcome**: Complete visibility into daily financial activity. User knows exactly where money came from and where it went.

---

### 8.3 Flow 3: Budgeting Cycle

**Modules involved**: Budgets → Transactions → Notifications

**Sequence**:

1. **Budgets**: User creates budget for Food & Dining ($400/month, Checking Account)

2. **Budgets** listens to `TransactionRecordedEvent` from Transactions:
   - Week 1: $95 spent → 24% used → Status: On track
   - Week 2: $210 spent → 53% used → Status: Warning (ahead of pace)
   - Week 3: $340 spent → 85% used → Publishes `BudgetWarningEvent`

3. **Notifications** receives `BudgetWarningEvent` → sends alert to user

4. **Budgets** continues monitoring:
   - Week 4: $390 spent → 98% used → Status: Warning (nearly exceeded)

5. **Month end**: Budget period ends → $385 total spent ✓ Under budget by $15

**Outcome**: Increased spending awareness leads to behavioral change and better financial control.

---

### 8.4 Flow 4: Shared Expense Management

**Modules involved**: Wallets → Collaboration → Transactions → Notifications → Auditing

**Sequence**:

1. **Wallets**: Alice creates shared wallet ("Apartment Expenses")
   - Publishes `WalletCreatedEvent`

2. **Collaboration**: Alice generates invitation for Bob
   - Publishes `InvitationSentEvent`
   - Notifications sends invitation email to Bob

3. **Collaboration**: Bob accepts invitation
   - Publishes `InvitationAcceptedEvent`
   - Publishes `MemberJoinedEvent`
   - Wallets grants Bob access to shared wallet
   - Notifications confirms member joined (sent to Alice and Bob)

4. **Transactions**: Alice records rent expense ($1,200, Equal split)
   - Publishes `TransactionRecordedEvent`
   - Collaboration calculates debts: Bob owes Alice $600

5. **Transactions**: Bob records groceries ($150, Equal split)
   - Publishes `TransactionRecordedEvent`
   - Collaboration recalculates debts: Bob owes Alice $525 (net)

6. **Collaboration**: Alice records settlement from Bob ($525)
   - Publishes `SettlementRecordedEvent`
   - Debt cleared to $0
   - Notifications confirms settlement recorded

**Outcome**: Shared expenses tracked transparently, debts calculated automatically, settlements recorded for accountability.

---

### 8.5 Flow 5: Recurring Management

**Modules involved**: Recurring → Transactions → Notifications

**Sequence**:

1. **Recurring**: User creates patterns for:
   - Rent: $1,200 expense, Housing, monthly on 1st
   - Spotify: $9.99 expense, Subscriptions & Bills, monthly on 15th
   - Paycheck: $2,000 income, Salary, biweekly

2. **Recurring**: Background job runs daily
   - Scans active patterns
   - Generates transactions for patterns due today

3. **Recurring**: On occurrence date (e.g., 1st of month):
   - Creates transaction via Transactions module
   - Publishes `RecurringTransactionGeneratedEvent`
   - Notifications sends alert to user

4. **Transactions**: Auto-generated transaction recorded
   - Publishes `TransactionRecordedEvent`
   - Wallets updates balance
   - Budgets updates spending (if applicable)

5. **User review**: User sees auto-generated transaction
   - Most are correct → no action
   - Some vary → user edits amount (utility bill)
   - Some didn't occur → user deletes (gym holiday)

**Outcome**: Manual recording work reduced by ~60%. Financial forecast visibility improved.

---

### 8.6 Flow 6: Savings Progress

**Modules involved**: Goals → Wallets → Transactions → Notifications

**Sequence**:

1. **Goals**: User creates savings goal ("Europe Vacation", $5,000 target, 9 months, linked to "Vacation Fund" wallet)

2. **Transactions**: User transfers $500 from Checking to Vacation Fund
   - Publishes `TransactionRecordedEvent`
   - Goals updates progress: 10% complete

3. **Transactions**: Monthly contributions continue
   - Month 2: +$600 → 22% complete
   - Month 4: +$700 → 48% complete (crosses 25% milestone)
   - Goals publishes `GoalMilestoneReachedEvent` at 25%, 50%, 75%

4. **Notifications** receives milestone events → sends congratulatory alerts

5. **Goals**: Month 9 → $5,000 reached (100% complete)
   - Publishes `GoalAchievedEvent`
   - Notifications sends achievement notification

**Outcome**: Clear visibility into savings progress motivates consistent contributions. Dedicated wallet prevents "savings leakage."

---

### 8.7 Flow 7: Collaboration Journey (Complex Multi-Module)

**Modules involved**: Wallets → Collaboration → Transactions → Notifications → Auditing

**Context**: Three friends (Carol, David, Emma) plan a weekend trip.

**Sequence**:

1. **Wallets**: Carol creates shared wallet ("Weekend Trip - Lake Tahoe")
   - Publishes `WalletCreatedEvent`

2. **Collaboration**: Carol invites David and Emma
   - Publishes `InvitationSentEvent` (2x)
   - Notifications sends invitation emails

3. **Collaboration**: David and Emma accept invitations
   - Publishes `InvitationAcceptedEvent` (2x)
   - Publishes `MemberJoinedEvent` (2x)
   - Wallets grants access to both
   - Notifications confirms members joined (sent to all 3)

4. **Transactions**: Initial contributions (3x $300 transfers from personal wallets)
   - Publishes `TransactionRecordedEvent` (3x)
   - Shared wallet balance: $900

5. **Transactions**: During trip — unequal spending:
   - Carol pays hotel: $450 expense (Equal split: $150 each)
   - David pays gas: $60 expense (Equal split: $20 each)
   - Emma pays groceries: $90 expense (Equal split: $30 each)
   - Carol pays dinner: $120 expense (Equal split: $40 each)
   - Each transaction publishes `TransactionRecordedEvent`

6. **Collaboration** listens to all transaction events → recalculates debts:
   - Carol paid $570, should pay $240 → Others owe her $330
   - David paid $60, should pay $240 → He owes $180
   - Emma paid $90, should pay $240 → She owes $150
   - Simplified: David owes Carol $180, Emma owes Carol $150

7. **Collaboration**: Post-trip settlements:
   - David sends Carol $180 (external payment)
   - Carol records settlement → Publishes `SettlementRecordedEvent`
   - Emma sends Carol $150 (external payment)
   - Carol records settlement → Publishes `SettlementRecordedEvent`
   - All debts cleared

8. **Auditing**: Full audit trail visible to all members:
   - Who created wallet (Carol)
   - Who joined when (David, Emma)
   - Every transaction (who recorded, amount, split)
   - Every settlement (who paid whom)

**Outcome**: Complex shared expenses managed without spreadsheets, manual calculations, or awkward money conversations. Everyone sees the same information. Debts calculated automatically. Settlements recorded for transparency.

---

## 9. Service Dependency Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                         IDENTITY                              │
│               (all modules depend on this)                     │
└──────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌──────────────────────┐                    ┌──────────────┐
│      WALLETS         │                    │   AUDITING   │◄── All emit audit events
│ (incl Collaboration) │                    └──────────────┘
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│    TRANSACTIONS      │
│  (incl Categories)   │
└──────┬───────────────┘
       │
       ├───────────┬─────────────┐
       ▼           ▼             ▼
 ┌─────────┐  ┌────────┐  ┌──────────────┐
 │ BUDGETS │  │ GOALS  │  │  RECURRING   │
 └────┬────┘  └────┬───┘  └──────────────┘
      │            │
      └─────┬──────┘
            ▼
    ┌──────────────┐
    │NOTIFICATIONS │◄── All emit notifications
    └──────────────┘
```

**Dependency Notes**:

- **Identity**: Foundation layer — no dependencies on other modules
- **Wallets** (includes Collaboration): Second layer — depends only on Identity
- **Transactions** (includes Categories): Third layer — depends on Wallets
- **Budgets + Goals + Recurring**: Fourth layer — depend on Transactions
- **Notifications + Auditing**: Cross-cutting — consumed by all modules, depend only on Identity

**Deployment Note**: The diagram shows logical dependencies, not physical deployment boundaries. All modules are deployed together in a single modular monolith. Module boundaries are enforced through architecture tests, not separate processes.

---

*Kakeibo is a personal finance platform balancing individual tracking with collaborative expense management. The platform honors traditional Japanese budgeting wisdom while adapting to contemporary digital life and collaborative financial responsibilities.*
