# PharmaCare review resolution — 2026-09-19

This document maps the full review checklist to the current `admin-dashboard-upgrade` implementation.

## UX / visual

- **#1 Currency** — resolved. Customer-facing marketplace, cart, checkout, orders, pharmacy portal and operations screens use the centralized `CurrencyFormatter` with **JOD**.
- **#2 Arabic / RTL** — resolved for the active marketplace experience. A persistent EN/AR switch sets `lang` and `dir`, the marketplace shell and core customer marketplace flows are localized, and RTL-specific layout rules are included.
- **#3 Ghost cart toast** — resolved. Toasts are hidden by default and only shown when explicitly activated.
- **#4 Footer whitespace** — resolved in active marketplace pages by the flex page shell and tightened responsive section spacing.
- **#5 Hero duplication** — resolved. Hero messaging and the pharmacy section now serve different purposes.
- **#6 City selector** — resolved. Marketplace cities are loaded from verified pharmacy data, seeded across Amman/Irbid/Zarqa/Salt, and optional browser geolocation can sort by real Haversine distance.
- **#7 Pharmacy cards** — retained and expanded with distance, rating, ETA, fee and open state.
- **#8 Repetitive popular data** — resolved. Home results are grouped by product and seeded across a broader catalog/pharmacy set.
- **#9 Performance** — improved with server-side EF filtering/projection, `AsNoTracking`, reduced legacy N+1 paths, static caching and response compression. Further production profiling should be based on deployed telemetry rather than screenshots.

## Architecture

- **#10 Duplicate storefront** — resolved. `Marketplace` is canonical. Legacy `FrontEnd` commerce routes are compatibility redirects and its compatibility search now reads marketplace inventory/pricing rather than the retired single-store model.
- **#11 Schema split** — resolved. EF migrations are the schema source of truth. Runtime schema bootstrappers were retired and obsolete SQL bootstrapper files removed.
- **#12 Startup bootstrapping** — resolved. Development applies migrations once and only seeds empty catalog/marketplace data.
- **#13 Authorization** — resolved for protected operational surfaces via centralized `SessionAuthorizeAttribute`; global MVC antiforgery validation is enabled.
- **#14 Stale session role** — resolved. `SessionAccountValidationMiddleware` revalidates active users and synchronizes role/name each request.
- **#15 Old/new commerce overlap** — contained. Marketplace is the only active customer commerce path; legacy order/cart entities remain only for historical/admin compatibility while migration to marketplace data is completed safely.
- **#16 Duplicate Order DbSet** — resolved. One `DbSet<Order> Orders` remains with explicit legacy table mapping.
- **#17 AI model name** — no code change required. `gpt-5.6-luna` is a valid OpenAI Responses API model ID in the current API catalog.
- **#18 master database** — resolved. Default connection targets `PharmaCareDb`.
- **#19 Proxy-aware rate limiting** — resolved. Forwarded headers are enabled before rate limiting/session processing.

## Security

- **#20 AI CSRF / docs mismatch** — resolved. Unsafe MVC requests are protected globally through `AutoValidateAntiforgeryTokenAttribute`; AJAX sends the token header.
- **#21 Missing POST antiforgery** — resolved for MVC state-changing endpoints; compatibility cart/support endpoints also include explicit validation where appropriate.
- **#22 Silent catches** — materially reduced on active flows and replaced with structured `ILogger` logging + generic customer messages. Intentional best-effort notification/email catches remain non-transactional so SMTP failure cannot roll back valid orders.
- **#23 Account enumeration** — resolved. Registration and password recovery return generic responses.
- **#24 Email change verification** — resolved. Email uniqueness is checked and changed addresses require new verification before the next login.
- **#25 Password policy** — resolved. Minimum 10 characters with uppercase, lowercase, digit and special character.
- **#26 Session cookies** — resolved. Production uses `SameSite=Strict` and `SecurePolicy=Always`; development remains localhost-friendly.
- **#27 Existing positive controls** — retained: secrets stay out of Git, BCrypt is used, security headers remain enabled.

## Code quality

- **#28 SHA256 comment** — resolved; documentation now states BCrypt.
- **#29 Repeated role checks** — reduced with centralized authorization and session-validation helpers. Shared storefront shell state is centralized where practical.
- **#30 Historical migration names** — intentionally **not renamed**. Renaming already-applied migrations would corrupt migration history. New migration names are descriptive; the marketplace schema is consolidated in `20260919210000_ConsolidateMarketplaceSchema`.
- **#31 Pending migration work** — resolved in Git: consolidated marketplace schema and snapshot are committed and CI builds the branch.
- **#32 Feature branch vs main** — addressed through PR #1. The PR is the controlled promotion path to `main`; merge only after the final CI run is green.

## Product improvements delivered

- Master product catalog + pharmacy-specific price/stock.
- Product-first marketplace search and pharmacy comparison.
- Smart Cart across pharmacies.
- Jordan city + geolocation/distance support.
- Secure prescription upload/review workflow.
- Marketplace order timeline and status history.
- Customer addresses and notifications.
- Pharmacy operations portal and inventory controls.
- Driver delivery portal.
- Marketplace API endpoints prepared for a future mobile app.
- JOD, English/Arabic RTL storefront shell, responsive design, dark mode.
- OpenAI assistant remains optional and fails independently from the commerce platform.

## Final acceptance gate

Before merging to `main`:
1. GitHub Actions Release build must be green.
2. Run locally against `PharmaCareDb`.
3. Smoke-test Marketplace → Compare → Cart → Checkout → Order tracking, Prescription upload/review, Pharmacy Portal and Driver Portal.
