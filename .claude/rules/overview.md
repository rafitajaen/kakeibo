# Kakeibo Overview

## 1. Introduction & Identity

**Application Name**: Kakeibo (家計簿)

**Tagline**: Mindful money management for personal budgeting and collaborative expenses.

**Origin**: Kakeibo is inspired by the traditional Japanese household budgeting method created over a century ago by Hani Motoko, Japan's first female journalist. The word "kakeibo" (家計簿) translates to "household financial ledger" and represents a philosophy of conscious spending through reflection and planning.

**Access Model**: Kakeibo is a personal finance platform with open registration. Users operate within isolated financial environments while maintaining the ability to collaborate with others through shared contexts. A super admin can control new registrations, block users, or manage accounts as needed.

---

## Table of Contents

1. [Introduction & Identity](#1-introduction--identity)
2. [Core Philosophy](#2-core-philosophy)
   - [Conscious Spending](#conscious-spending)
   - [Reflection Through Categorization](#reflection-through-categorization)
   - [Savings Through Awareness](#savings-through-awareness)
   - [Modern Adaptation](#modern-adaptation)
   - [Balance Between Individual and Collective](#balance-between-individual-and-collective)
3. [Dual Purpose](#3-dual-purpose)
   - [Personal Finance Tracking](#personal-finance-tracking)
   - [Shared Expense Management](#shared-expense-management)
   - [Integration](#integration)
4. [User Model](#4-user-model)
   - [Who Uses Kakeibo](#who-uses-kakeibo)
   - [Isolation Architecture](#isolation-architecture)
   - [Collaboration Model](#collaboration-model)
   - [Permission Model](#permission-model)
5. [Core Functionality](#5-core-functionality)
   - [A. Financial Tracking](#a-financial-tracking)
   - [B. Budgeting & Planning](#b-budgeting--planning)
   - [C. Collaboration](#c-collaboration)
   - [D. Insights & Awareness](#d-insights--awareness)
   - [E. Data Management](#e-data-management)
6. [Key Concepts](#6-key-concepts)
   - [User](#user)
   - [Wallet](#wallet)
   - [Transaction](#transaction)
   - [Category](#category)
   - [Split](#split)
   - [Recurring Pattern](#recurring-pattern)
   - [Budget](#budget)
   - [Savings Goal](#savings-goal)
   - [Invitation](#invitation)
   - [Debt](#debt)
   - [Settlement](#settlement)
   - [Activity](#activity)
7. [Main Flows](#7-main-flows)
   - [Flow 1: Getting Started](#flow-1-getting-started)
   - [Flow 2: Daily Tracking](#flow-2-daily-tracking)
   - [Flow 3: Budgeting Cycle](#flow-3-budgeting-cycle)
   - [Flow 4: Shared Expense Management](#flow-4-shared-expense-management)
   - [Flow 5: Recurring Management](#flow-5-recurring-management)
   - [Flow 6: Savings Progress](#flow-6-savings-progress)
   - [Flow 7: Collaboration Journey](#flow-7-collaboration-journey)
8. [Objectives & Goals](#8-objectives--goals)
   - [Financial Awareness](#financial-awareness)
   - [Simplicity](#simplicity)
   - [Flexibility](#flexibility)
   - [Transparency](#transparency)
   - [Automation](#automation)
   - [Accessibility](#accessibility)
   - [Privacy](#privacy)
   - [Sustainability](#sustainability)
9. [Appendix: Design Principles](#appendix-design-principles)

---

## 2. Core Philosophy

Kakeibo embodies three fundamental principles adapted from the traditional Japanese method:

### Conscious Spending
Every transaction is an opportunity for awareness. By recording and categorizing each financial event, users develop a deeper understanding of their spending patterns and make more intentional choices about where their money goes.

### Reflection Through Categorization
The traditional Kakeibo method organizes expenses into four essential categories: Survival, Culture, Optional, and Extra. This platform extends this philosophy with a system of twelve standard categories that cover the full spectrum of modern life, while allowing unlimited custom categories for personal nuance.

### Savings Through Awareness
Financial health begins with seeing clearly. By tracking income, expenses, and progress toward goals in one unified view, users naturally identify opportunities to save and grow their wealth.

### Modern Adaptation
While honoring traditional principles, Kakeibo adapts to contemporary needs:
- **Digital Convenience**: Instant recording replaces manual ledger-keeping
- **Collaborative Context**: Extends individual practice to shared financial responsibilities
- **Automation Support**: Recurring patterns reduce repetitive work
- **Intelligent Forecasting**: Future visibility helps prevent budget overruns

### Balance Between Individual and Collective
Kakeibo recognizes that modern financial life exists in two simultaneous dimensions: personal autonomy and shared responsibility. The platform treats both as equally important, allowing users to manage their individual finances while participating in collaborative expense pools without friction or complexity.

---

## 3. Dual Purpose

Kakeibo serves two distinct yet interconnected purposes with equal emphasis:

### Personal Finance Tracking
The foundation of financial awareness is individual control. Users manage their personal finances through:

- **Individual Wallets**: Personal financial containers that hold money and track balances
- **Transaction Recording**: Capturing every income, expense, and transfer
- **Budget Management**: Setting limits and monitoring spending against them
- **Savings Goals**: Defining targets and tracking progress toward them
- **Recurring Patterns**: Automating predictable transactions
- **Financial Insights**: Understanding patterns through balance history and budget comparison

Personal tracking enables the core Kakeibo practice: conscious awareness of where money comes from and where it goes.

### Shared Expense Management
Modern life involves collaborative financial responsibilities—roommates splitting rent, friends sharing vacation costs, families managing household expenses. Kakeibo provides:

- **Shared Wallets**: Collaborative financial spaces where multiple users participate equally
- **Expense Splitting**: Flexible division of costs (equal, percentage-based, or custom amounts)
- **Debt Tracking**: Transparent view of who owes whom, inspired by Splitwise
- **Settlement System**: Recording when debts are paid without complex reconciliation
- **Invitation Mechanism**: Secure access control for shared financial contexts
- **Equal Participation**: All members have the same rights and visibility

Shared expense management removes the awkwardness and complexity from collaborative spending.

### Integration
The power of Kakeibo lies in the seamless integration of these two dimensions:

- A single transaction can be both a personal expense and a shared cost
- Budgets can span personal and shared wallets
- Savings goals can track progress across all financial contexts
- The user sees a unified financial picture while maintaining clear boundaries between personal and shared money

---

## 4. User Model

### Who Uses Kakeibo

**Primary User**: Any individual seeking conscious control over their finances, from students tracking allowances to professionals managing complex budgets to retirees monitoring retirement savings.

**Collaborative Users**: Groups of people with shared financial responsibilities:
- Roommates splitting household expenses
- Couples managing joint costs
- Families tracking shared budgets
- Friends coordinating group purchases or trips
- Small communities managing collective resources

### Isolation Architecture
Each user operates within a completely isolated financial environment. One user cannot see, access, or affect another user's personal financial data unless explicitly invited to a shared context.

This isolation ensures:
- **Privacy**: Personal finances remain private
- **Security**: Financial data is protected from unauthorized access
- **Independence**: Each user's experience is unaffected by others
- **Simplicity**: Straightforward permission model focused on user isolation and collaborative sharing

### Collaboration Model
When users share financial responsibilities, they create shared wallets that exist as separate spaces within each participating user's environment. Shared contexts have these characteristics:

**Equal Rights**: All participants have identical permissions—no "owner" or "administrator" hierarchy exists in shared wallets. Everyone can:
- View all transactions
- Record new transactions
- Edit or delete any transaction
- Invite new members
- View debts and settlements

**Symmetric Visibility**: All participants see the same information in a shared wallet. When one user records a transaction, all members see it immediately.

**Independent Departure**: Any member can leave a shared wallet at any time. The wallet continues to exist for remaining members.

**Invitation-Based**: New members join shared wallets only through explicit invitation. The invitation mechanism ensures:
- Security (only intended people gain access)
- Consent (invitees must accept to participate)
- Traceability (activity logs record who joined when)

### Permission Model
The permission model is intentionally simple:

**Personal Contexts**: Full control over own wallets, transactions, budgets, goals, categories, and recurring patterns.

**Shared Contexts**: Equal rights for all members. No hierarchy, no role-based restrictions, no administrative privileges.

**Invitation Authority**: Any member of a shared wallet can invite new members. This reflects the equal-participation philosophy—shared financial responsibility implies shared invitation authority.

---

## 5. Core Functionality

Kakeibo provides capabilities organized across five functional domains:

### A. Financial Tracking

**Transaction Recording**
The fundamental action in Kakeibo is recording transactions—financial events that change wallet balances.

Three transaction types exist:
- **Income**: Money entering a wallet
- **Expense**: Money leaving a wallet
- **Transfer**: Money moving between two wallets (same user or different users)

Each transaction captures:
- Amount (how much money moved)
- Date and time (when it happened)
- Category (what kind of transaction)
- Description (what it was for)
- Wallet(s) involved
- Split configuration (for shared expenses)

**Wallet Organization**
Wallets are containers that hold money and organize transactions. Each wallet has:
- Current balance
- Transaction history
- Ownership (personal or shared)
- Member list (for shared wallets)

Wallets can be:
- **Personal**: Owned by one user, representing personal accounts (checking, savings, cash, etc.)
- **Shared**: Owned by multiple users, representing collaborative financial spaces

**Category Classification**
Every transaction belongs to a category that answers "what kind of spending is this?" Categories organize financial activity into meaningful groups for analysis and budgeting.

Twelve system categories provide comprehensive coverage:
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

Users can create unlimited custom categories for personal classification needs.

**Balance Tracking**
Kakeibo maintains accurate balance information:
- Current balance (wallet's money right now)
- Historical balances (wallet's money at any past point)
- Projected balances (wallet's money in the future based on recurring patterns)

Balance visibility enables users to:
- Know exactly how much money they have
- See how balances changed over time
- Anticipate future financial positions

### B. Budgeting & Planning

**Budget Creation**
Budgets set spending limits for categories over specific time periods. Each budget defines:
- Category being limited
- Time period (month, year, custom range)
- Limit amount
- Wallet(s) being monitored

Budgets can span personal wallets, shared wallets, or combinations.

**Budget Monitoring**
Active budgets track spending in real-time:
- Current spending (how much spent so far)
- Remaining budget (how much left)
- Percentage used
- Daily average spending
- Projected overage warning

**Recurring Transactions**
Many transactions follow predictable patterns—rent due monthly, salary deposited biweekly, subscription charged yearly. Recurring patterns automate these transactions.

Each recurring pattern defines:
- Transaction details (amount, category, description)
- Wallet(s) involved
- Recurrence rule (frequency and timing)
- Date range (when it starts and ends, if ever)

Recurring patterns:
- Generate transactions automatically
- Provide forecast visibility
- Reduce manual recording work
- Improve budget accuracy through predictability

**Savings Goals**
Goals represent financial targets users want to reach. Each goal has:
- Target amount (how much to save)
- Deadline (when to reach it, optional)
- Current progress (how much saved so far)
- Associated wallet (optional specific wallet tracking)

Goals can track:
- Total saved across all wallets
- Balance growth in specific wallet
- Manual progress updates

### C. Collaboration

**Shared Expense Pools**
Shared wallets create collaborative financial spaces where multiple users participate equally. These pools:
- Have their own balance
- Contain transactions visible to all members
- Track who spent what
- Calculate debts automatically

**Split Configuration**
When recording shared expenses, splits determine how the cost divides among members. Three split types exist:

- **Equal Split**: Divides cost evenly among all members
- **Percentage Split**: Divides cost by specified percentages (must total 100%)
- **Custom Split**: Specifies exact amount for each member

Split configuration happens at the transaction level—each shared expense can have its own split logic.

**Debt Tracking**
Kakeibo automatically calculates debts based on shared wallet activity:
- Who paid for what
- How expenses split among members
- Net balances (who owes whom)

Debt tracking follows the Splitwise model:
- Continuous calculation (debts update with every transaction)
- Simplified balances (minimum number of debts shown)
- Clear visibility (everyone sees the same debt state)

**Debt Settlement**
When members pay each other to settle debts, settlements are recorded as special transactions that:
- Reduce or eliminate debts
- Don't affect wallet balances (money moved outside Kakeibo)
- Create audit trail
- Update debt calculations immediately

**Invitation System**
Collaboration requires secure access control. The invitation system:
- Creates invitation codes or links
- Specifies which shared wallet the invitation grants access to
- Sets expiration (invitations don't last forever)
- Tracks acceptance status
- Records who invited whom

Invitations ensure:
- Only intended people join
- Invitees explicitly consent
- Activity is traceable

### D. Insights & Awareness

**Balance Monitoring**
Users see financial position at a glance:
- Total balance (all wallets combined)
- Individual wallet balances
- Personal vs. shared money distinction
- Balance trends over time

**Budget Comparison**
Budget insights show:
- Spending vs. limit for each budget
- Categories most over/under budget
- Historical budget performance
- Forecast alerts (projected overruns)

**Savings Progress**
Goal tracking displays:
- Percentage complete
- Amount remaining
- Time remaining (if deadline set)
- Projected completion date (based on savings rate)

**Activity Logging**
Every action in Kakeibo creates an activity record:
- Who did what
- When it happened
- What changed

Activity logs provide:
- Audit trail for shared wallets
- Personal history review
- Accountability in collaborative contexts

**Notification System**
Users receive notifications about:
- Budget warnings (nearing or exceeding limits)
- Invitation activity (new invitations, acceptances)
- Shared wallet changes (new transactions, member changes)
- Goal milestones (percentage thresholds reached)

Notifications keep users informed without requiring constant checking.

### E. Data Management

**Import Capability**
Users can bulk-import transactions from:
- Bank statement files (CSV, OFX)
- Other financial applications (export formats)
- Manual CSV files

Import features:
- Map imported fields to Kakeibo structure
- Preview before committing
- Detect duplicates
- Auto-categorize when possible

**Export Capability**
Users can export their data:
- Transactions (filtered or complete)
- Budgets and budget performance
- Goals and progress
- Balance history

Export formats support:
- Archival (complete data backup)
- Analysis (spreadsheet-compatible)
- Migration (to other systems)

**Data Integrity**
Behind the scenes, Kakeibo ensures:
- Balances always match transaction history
- Debts always match shared wallet activity
- Deleted data doesn't break references
- Changes propagate consistently

---

## 6. Key Concepts

These are the fundamental building blocks of Kakeibo. Understanding these concepts is essential to understanding how the system works.

### User
An individual person with an account. Each user has:
- Unique identity
- Isolated financial environment
- Personal wallets
- Participation in zero or more shared wallets
- Complete privacy from other users (except in shared contexts)

### Wallet
A financial container that holds money and organizes transactions. Think of a wallet as representing a bank account, cash envelope, or shared expense pool. Characteristics:
- Has a name (e.g., "Checking Account", "Vacation Fund", "Apartment Expenses")
- Maintains a balance (how much money it currently holds)
- Stores transaction history
- Belongs to one user (personal wallet) or multiple users (shared wallet)
- Can be archived (hidden from daily view but data preserved)

### Transaction
A financial event that changes one or more wallet balances. Three types:

**Income**: Money enters a wallet
- Increases wallet balance
- Examples: salary deposit, gift received, refund

**Expense**: Money leaves a wallet
- Decreases wallet balance
- Examples: purchase, bill payment, ATM withdrawal

**Transfer**: Money moves between wallets
- Decreases source wallet balance
- Increases destination wallet balance
- Examples: moving money from checking to savings, splitting payment with friend

Every transaction has:
- Amount (how much money)
- Date and time (when it occurred)
- Category (what type of transaction)
- Description (what it was for)
- Wallet(s) affected
- Split configuration (for shared expenses)
- Creator (who recorded it)

### Category
A classification label that answers "what kind of transaction is this?" Categories group similar transactions for:
- Budget organization
- Spending analysis
- Pattern recognition
- Reporting

Two category types:
- **System categories**: Twelve built-in categories covering common expense types (housing, transportation, food, etc.)
- **Custom categories**: User-created categories for personal classification needs

Each transaction has ONE category. In shared wallets, all members see the same category (set by the transaction creator). This ensures budgets and reports work consistently across all wallet types.

### Split
The mechanism for dividing shared expenses among wallet members. Splits answer "how should this cost be divided?"

Three split types:

**Equal Split**: Divides total cost evenly
- Example: $60 dinner split equally among 3 people = $20 each

**Percentage Split**: Divides by specified percentages
- Example: $1000 rent split 60%/40% = $600/$400

**Custom Split**: Specifies exact amount per person
- Example: $75 shopping split $45/$30 (based on items purchased)

Splits determine:
- How much each member should pay
- Who owes whom
- Debt calculations

### Recurring Pattern
A template that generates transactions automatically on a schedule. Recurring patterns handle predictable transactions like:
- Monthly rent
- Biweekly salary
- Annual subscriptions
- Weekly grocery budget

Each pattern defines:
- Transaction details (amount, category, description, wallet)
- Schedule (how often: daily, weekly, monthly, yearly, custom)
- Start date (when pattern begins)
- End date (when pattern stops, or runs indefinitely)

Benefits:
- Reduces manual recording
- Improves forecast accuracy
- Ensures nothing forgotten
- Enables proactive budgeting

### Budget
A spending limit for a category over a time period. Budgets help answer "am I spending too much on X?"

Each budget specifies:
- Category being limited
- Time period (when the limit applies)
- Limit amount (maximum to spend)
- Wallet(s) being monitored

Budgets track:
- Current spending (how much spent so far)
- Remaining budget (how much left)
- Status (on track, warning, exceeded)

Budgets can monitor:
- Single personal wallet
- Single shared wallet
- Multiple wallets combined
- All wallets

### Savings Goal
A target representing financial progress toward a desired outcome. Goals answer "am I making progress toward X?"

Each goal has:
- Name (what you're saving for)
- Target amount (how much needed)
- Deadline (when you want to reach it, optional)
- Linked wallet (optional specific wallet to track)

Goals track:
- Current progress (how much saved)
- Percentage complete
- Amount remaining
- Projected completion date

Goal tracking modes:
- **Wallet-linked**: Tracks balance growth in specific wallet
- **Cross-wallet**: Tracks total across all wallets
- **Manual**: User updates progress manually

### Invitation
An access grant that allows someone to join a shared wallet. Invitations control who can participate in collaborative financial contexts.

Each invitation specifies:
- Which shared wallet it grants access to
- Expiration date (invitations don't last forever)
- Invitation code or link
- Creator (who sent it)

Invitation lifecycle:
1. **Created**: Member generates invitation for their shared wallet
2. **Pending**: Invitation exists but not yet accepted
3. **Accepted**: Invitee used invitation to join wallet
4. **Expired**: Time limit passed before acceptance
5. **Revoked**: Creator canceled invitation before acceptance

### Debt
An amount owed between two users based on shared wallet activity. Debts arise from:
- One person paying for shared expenses
- Unequal splits
- Accumulated imbalances over time

Debt characteristics:
- **Automatic**: Calculated from transaction history, not manually entered
- **Simplified**: System minimizes number of debts (A owes B $20, B owes C $20 → A owes C $20)
- **Symmetric**: Both parties see the same debt information
- **Persistent**: Exists until settled

Debts are not transactions—they are calculated states based on who paid what and how expenses split.

### Settlement
A record that someone paid someone else outside of Kakeibo to settle a debt. Settlements:
- Don't affect wallet balances (money moved externally)
- Reduce or eliminate debts
- Create audit trail
- Update debt calculations

Example: Alice and Bob share an apartment wallet. Transactions show Bob owes Alice $100. Bob gives Alice $100 cash. Alice records a settlement of $100 from Bob. The debt becomes zero.

### Activity
A record of something that happened in Kakeibo. Activities provide audit trails and history. Common activities:
- Transaction recorded/edited/deleted
- Budget created/updated
- Goal reached
- Member joined/left shared wallet
- Invitation sent/accepted

Activities capture:
- What happened
- Who did it
- When it occurred
- What changed

In shared wallets, all members see all activities, providing transparency and accountability.

---

## 7. Main Flows

These narrative descriptions show how Kakeibo works in practice. Each flow represents a common user journey.

### Flow 1: Getting Started

**Context**: A new user wants to begin tracking their finances with Kakeibo.

**Steps**:

1. **Registration**: User creates an account with email and password. Their financial environment is created—isolated from all other users.

2. **First Wallet Creation**: User creates their first personal wallet, typically representing their primary bank account (e.g., "Checking Account"). They enter the current balance.

3. **First Transaction**: User records their first expense—perhaps coffee purchased this morning. They:
   - Select the wallet (Checking Account)
   - Choose transaction type (Expense)
   - Enter amount ($4.50)
   - Pick category (Food & Dining)
   - Add description ("Morning coffee at Cafe Luna")
   - Confirm

4. **Balance Update**: The wallet balance decreases by $4.50. User sees their updated financial position.

5. **Additional Wallets**: User creates more wallets as needed (savings account, cash wallet, credit card). Each wallet tracks its own balance.

6. **Establishing Baseline**: Over the first few days, user records transactions consistently, building a picture of their spending patterns.

**Outcome**: User has functional financial tracking system with accurate balances and growing transaction history.

---

### Flow 2: Daily Tracking

**Context**: An established user goes about their day, recording transactions as they occur.

**Morning**:
- Grocery shopping: Records $65 expense in Checking Account, category Food & Dining
- Gas station: Records $40 expense in Checking Account, category Transportation

**Midday**:
- Receives paycheck: Records $2,000 income in Checking Account, category Salary (custom category)
- Transfers to savings: Records $500 transfer from Checking Account to Savings Account, category Savings & Investments

**Evening**:
- Dinner with friends: Records $45 expense in Checking Account, category Food & Dining
- Quick balance check: Opens Kakeibo, sees current balances for all wallets, confirms financial position

**Outcome**: Complete visibility into daily financial activity. User knows exactly where money came from and where it went.

---

### Flow 3: Budgeting Cycle

**Context**: User wants to control spending on dining out, which has been excessive.

**Steps**:

1. **Budget Creation**: User creates a budget:
   - Category: Food & Dining
   - Period: Current month
   - Limit: $400
   - Wallets: Checking Account

2. **Daily Monitoring**: Throughout the month, user checks budget status:
   - Week 1: $95 spent, $305 remaining (24% used) ✓ On track
   - Week 2: $210 spent, $190 remaining (53% used) ⚠ Warning (ahead of pace)
   - Week 3: $340 spent, $60 remaining (85% used) ⚠ Warning (nearly exceeded)

3. **Behavioral Adjustment**: Seeing the warnings, user reduces dining out frequency in final week.

4. **Month End**: Budget period ends:
   - Total spent: $385
   - Limit: $400
   - Result: Under budget by $15 ✓

5. **Reflection**: User reviews which dining expenses were worthwhile (celebratory dinner with family) vs. regrettable (impulsive fast food). This awareness informs next month's approach.

6. **Next Month**: User creates a new budget for the new month, possibly adjusting the limit based on last month's experience.

**Outcome**: Increased spending awareness leads to behavioral change and better financial control.

---

### Flow 4: Shared Expense Management

**Context**: Alice and Bob become roommates and need to manage apartment expenses together.

**Steps**:

1. **Shared Wallet Creation**: Alice creates a shared wallet named "Apartment Expenses".

2. **Inviting Roommate**: Alice generates an invitation for the wallet and sends it to Bob (via email, message, or QR code).

3. **Accepting Invitation**: Bob receives the invitation, clicks the link, and accepts. He now has access to "Apartment Expenses" wallet in his Kakeibo.

4. **Recording Shared Expenses**:
   - Alice pays $1,200 rent on her credit card. She records it in Kakeibo:
     - Wallet: Apartment Expenses
     - Type: Expense
     - Amount: $1,200
     - Category: Housing
     - Split: Equal (both pay $600)
   - Bob buys $150 worth of groceries for the apartment. He records it:
     - Wallet: Apartment Expenses
     - Type: Expense
     - Amount: $150
     - Category: Food & Dining
     - Split: Equal (both pay $75)

5. **Debt Calculation**: Kakeibo automatically calculates debts:
   - Alice paid $1,200, should pay $600 → Others owe her $600
   - Bob paid $150, should pay $75 → Others owe him $75
   - Net: Bob owes Alice $525

6. **Debt Visibility**: Both Alice and Bob see in Kakeibo that Bob owes Alice $525. The debt is calculated automatically from the transaction history.

7. **Settlement**: Bob transfers $525 to Alice's bank account (outside Kakeibo). Alice records the settlement in Kakeibo:
   - From: Bob
   - Amount: $525

8. **Debt Cleared**: The debt between Alice and Bob becomes $0. The settlement is recorded in the activity history.

**Outcome**: Shared expenses are tracked transparently, debts calculated automatically, and settlements recorded for accountability. No awkward conversations about who owes what.

---

### Flow 5: Recurring Management

**Context**: User has predictable monthly expenses and wants to reduce manual recording work.

**Steps**:

1. **Identifying Patterns**: User reviews transaction history and identifies recurring transactions:
   - Rent: $1,200 on 1st of every month
   - Spotify subscription: $9.99 on 15th of every month
   - Gym membership: $45 on 10th of every month
   - Paycheck: $2,000 on 1st and 15th of every month (biweekly)

2. **Creating Recurring Patterns**:
   - User creates a recurring pattern for each:
     - Rent: $1,200 expense, category Housing, monthly on 1st
     - Spotify: $9.99 expense, category Subscriptions & Bills, monthly on 15th
     - Gym: $45 expense, category Health & Wellness, monthly on 10th
     - Paycheck: $2,000 income, category Salary, biweekly

3. **Automatic Generation**: On each occurrence date, Kakeibo automatically creates the transaction. User receives notification.

4. **Review**: User reviews auto-generated transactions:
   - Most are correct and require no action
   - Occasionally amounts vary (e.g., utility bill fluctuates) → user edits the amount
   - Occasionally transaction doesn't occur (e.g., gym closed for holiday) → user deletes it

5. **Forecast Visibility**: User can see projected future transactions based on recurring patterns:
   - Next 30 days: 12 recurring transactions expected
   - Next 90 days: 36 recurring transactions expected
   - Projected balance in 30 days: $3,450 (based on current balance + expected income - expected expenses)

6. **Budget Accuracy**: Budgets account for recurring expenses, providing more accurate "remaining budget" calculations that consider upcoming predictable costs.

**Outcome**: Manual recording work reduced by ~60%. Financial forecast visibility improved. User can focus on recording variable transactions while recurring ones handle themselves.

---

### Flow 6: Savings Progress

**Context**: User wants to save $5,000 for a vacation by the end of the year.

**Steps**:

1. **Goal Creation**: User creates a savings goal:
   - Name: "Europe Vacation"
   - Target: $5,000
   - Deadline: December 31st (9 months away)
   - Linked wallet: Vacation Fund (a dedicated savings wallet)

2. **Initial Contribution**: User transfers $500 from checking to Vacation Fund. Goal progress: 10% complete.

3. **Regular Saving**: Each month, user transfers money to Vacation Fund:
   - Month 2: +$600 → $1,100 total (22% complete)
   - Month 3: +$600 → $1,700 total (34% complete)
   - Month 4: +$700 → $2,400 total (48% complete)

4. **Milestone Notifications**: At 25%, 50%, and 75% progress, user receives congratulatory notifications.

5. **Progress Monitoring**: User checks goal status regularly:
   - Current: $2,400 / $5,000
   - Remaining: $2,600
   - Time remaining: 5 months
   - Pace: On track (needs $520/month average)

6. **Adjustment**: During month 5, user receives an unexpected bonus. They contribute an extra $1,000:
   - Total: $3,400 (68% complete)
   - Updated pace: Only needs $320/month now

7. **Goal Achievement**: By month 9, user reaches $5,000. Goal shows 100% complete. User receives achievement notification.

8. **Post-Achievement**: User keeps the Vacation Fund wallet active. When booking flights and hotels, they record expenses from this wallet, tracking how vacation funds are spent.

**Outcome**: Clear visibility into savings progress motivates consistent contributions. Tangible target and deadline create accountability. Dedicated wallet prevents "savings leakage."

---

### Flow 7: Collaboration Journey

**Context**: Three friends (Carol, David, and Emma) plan a weekend trip and need to manage shared expenses.

**Steps**:

1. **Pre-Trip Setup**:
   - Carol creates a shared wallet: "Weekend Trip - Lake Tahoe"
   - Carol invites David and Emma
   - Both accept invitations
   - All three now have access to the wallet

2. **Initial Contributions**:
   - Each person contributes $300 to cover estimated expenses
   - Carol: Records $300 income to shared wallet from her personal wallet (transfer)
   - David: Records $300 income to shared wallet from his personal wallet (transfer)
   - Emma: Records $300 income to shared wallet from her personal wallet (transfer)
   - Shared wallet balance: $900

3. **During Trip - Unequal Spending**:
   - Carol books hotel: $450 on her credit card
     - Records in shared wallet: $450 expense, split equally ($150 each)
   - David pays for gas: $60
     - Records in shared wallet: $60 expense, split equally ($20 each)
   - Emma buys groceries: $90
     - Records in shared wallet: $90 expense, split equally ($30 each)
   - Carol pays for dinner: $120
     - Records in shared wallet: $120 expense, split equally ($40 each)

4. **Real-Time Debt Visibility**:
   - All three can see running debt totals in Kakeibo:
     - Carol paid $570, should pay $240 → Others owe her $330
     - David paid $60, should pay $240 → He owes $180
     - Emma paid $90, should pay $240 → She owes $150
   - Simplified: David owes Carol $180, Emma owes Carol $150

5. **Post-Trip Settlement**:
   - David sends Carol $180 via payment app
   - Carol records settlement in Kakeibo
   - Emma sends Carol $150 via payment app
   - Carol records settlement in Kakeibo
   - All debts cleared

6. **Wallet Retention**: Group keeps the wallet active for:
   - Reference (remembering what the trip cost)
   - Future trips (can reuse the wallet)
   - Activity history (who paid for what)

**Outcome**: Complex shared expenses managed without spreadsheets, manual calculations, or awkward money conversations. Everyone sees the same information. Debts calculated automatically. Settlements recorded for transparency.

---

## 8. Objectives & Goals

Kakeibo aims to achieve the following outcomes:

### Financial Awareness
The primary objective is conscious spending through systematic recording and reflection. Users who consistently track transactions in Kakeibo develop:
- **Pattern Recognition**: Understanding where money actually goes (often different from assumptions)
- **Spending Consciousness**: The act of recording creates a moment of reflection before purchasing
- **Category Insights**: Seeing aggregated spending by category reveals priorities and opportunities
- **Balance Reality**: Always knowing exact financial position removes anxiety and enables confidence

Awareness precedes change. Kakeibo makes financial reality visible.

### Simplicity
Tracking expenses should not be a chore. Kakeibo reduces friction through:
- **Fast Recording**: Minimal clicks to record a transaction
- **Smart Defaults**: System learns common patterns and suggests likely values
- **Calculator Interface**: Enter amounts naturally without formatting
- **Mobile Optimization**: Record transactions immediately when they occur, not later from memory
- **Clean Interface**: Focus on essential information, hide complexity

If recording is tedious, users stop recording. Simplicity ensures consistency.

### Flexibility
Financial life varies widely between individuals. Kakeibo adapts through:
- **Unlimited Wallets**: Represent any financial account or envelope system
- **Custom Categories**: Personal classification beyond system defaults
- **Wallet Combination**: Budgets and goals can span multiple wallets or focus on one
- **Split Options**: Three split types handle different sharing scenarios
- **Manual Overrides**: Automated systems (recurring, splits) can always be adjusted for exceptions

One size does not fit all. Flexibility ensures Kakeibo works for diverse needs.

### Transparency
In collaborative contexts, trust requires visibility. Kakeibo provides:
- **Symmetric Information**: All shared wallet members see identical information
- **Activity Logging**: Every action recorded with who, what, when
- **Automatic Calculation**: Debts computed from facts, not opinions
- **Equal Rights**: No hidden administrative powers or asymmetric access
- **Clear Debts**: Simplified balances remove confusion about who owes whom

Transparency prevents conflicts and enables accountability.

### Automation
Manual work should be minimized where patterns exist. Kakeibo automates:
- **Recurring Transactions**: Predictable expenses and income generated automatically
- **Debt Calculation**: Continuous computation from transaction history
- **Budget Tracking**: Real-time spending comparison against limits
- **Balance Projection**: Future balances forecasted from recurring patterns
- **Categorization Suggestions**: Learn from past choices to suggest categories

Automation frees mental energy for decisions that matter.

### Accessibility
Financial tools should not require financial expertise. Kakeibo ensures:
- **Intuitive Concepts**: Wallets, transactions, budgets map to familiar real-world concepts
- **No Accounting Knowledge**: Don't need to understand debits/credits or double-entry
- **Plain Language**: Avoid financial jargon
- **Visual Clarity**: Balance trends, budget progress, goal achievement shown graphically
- **Equal Participation**: Shared contexts require no "power user" to manage complexity

Everyone should be able to manage their finances with confidence.

### Privacy
Personal finances are sensitive. Kakeibo protects:
- **Isolation**: Users cannot see others' personal financial data
- **Controlled Sharing**: Collaboration requires explicit invitation and acceptance
- **Departure Freedom**: Can leave shared wallets without affecting personal data
- **Data Ownership**: Users control their data (export, delete)

Privacy builds trust. Trust enables honest financial tracking.

### Sustainability
Behavioral change requires sustainability. Kakeibo promotes:
- **Habit Formation**: Small daily actions (recording transactions) build lasting awareness
- **Positive Reinforcement**: Goal milestones and budget successes provide encouragement
- **Flexible Cadence**: Can engage daily, weekly, or monthly—whatever fits lifestyle
- **Low Pressure**: No judgment, no shame—just information for better decisions

The goal is not perfect tracking but consistent awareness that leads to better financial choices over time.

---

## Appendix: Design Principles

These principles guide all decisions about Kakeibo:

**Principle 1: Clarity over Cleverness**
Simple, obvious solutions are better than clever, complex ones. Users should never wonder how something works.

**Principle 2: Accuracy is Non-Negotiable**
Balances, debts, and calculations must always be correct. Financial tools that make math errors are worse than useless.

**Principle 3: Privacy by Default, Sharing by Choice**
Everything is private unless explicitly shared. Collaboration requires affirmative consent.

**Principle 4: Equal Respect in Shared Contexts**
No hierarchies in collaborative spaces. All participants have equal rights and visibility.

**Principle 5: Automate the Repetitive, Surface the Exceptional**
Handle predictable patterns automatically. Draw attention to anomalies and deviations.

**Principle 6: Recording is Reflection**
The act of recording a transaction has value beyond the data captured—it creates a moment of financial awareness.

**Principle 7: Show the Past, Track the Present, Predict the Future**
Financial tools should help users understand where they've been (history), where they are (current state), and where they're heading (forecast).

**Principle 8: Trust Through Transparency**
Collaborative financial management works only when all parties have complete visibility and confidence in the system's fairness.

---

*Kakeibo is a personal finance platform balancing individual tracking with collaborative expense management. The platform honors traditional Japanese budgeting wisdom while adapting to contemporary digital life and collaborative financial responsibilities.*
