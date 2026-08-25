# PharmaCare AI setup

PharmaCare's AI integration uses the OpenAI Responses API through the central `IAIService` / `OpenAIService` layer.

## 1. Keep the API key local

Do **not** commit an API key to GitHub. `appsettings.Local.json` is already ignored by `.gitignore` and is loaded after `appsettings.json`.

Create or update `PharmaCare/appsettings.Local.json`:

```json
{
  "AISettings": {
    "Enabled": true,
    "ApiKey": "YOUR_OPENAI_API_KEY",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-5.6-luna",
    "MaxOutputTokens": 700,
    "TimeoutSeconds": 30
  }
}
```

You can also use ASP.NET Core user-secrets instead of a local JSON file.

## 2. What the AI layer currently knows

The AI controller supplies only customer-safe context:

- Active storefront products
- Product name, category, description, price and live stock
- Whether a product requires a prescription
- Public storefront product path
- The signed-in customer's own recent order statuses and prescription reservations
- The current page path/title

It deliberately does **not** supply SKU, barcode, internal IDs, admin-only notes, passwords, API keys, or other customers' data.

## 3. Healthcare safety boundary

PharmaCare AI is a website/catalog assistant, not a clinician. It must not diagnose, choose medicines for symptoms, prescribe treatment, determine dosage, recommend medication changes, or make personalized interaction/contraindication decisions.

## 4. Endpoint

- `GET /AI/Status` - reports whether AI is configured.
- `POST /AI/Chat` - customer AI endpoint. The POST is anti-forgery protected and session rate-limited.

## 5. Planned site-wide AI features

The central service is intentionally reusable. The next UI/application layers can use it for:

1. Global customer AI assistant on every storefront page.
2. Product-aware Q&A grounded in the live catalog.
3. Customer order/reservation assistant using only that customer's records.
4. Admin dashboard summaries for orders, support and inventory.
5. AI-assisted product copy drafting with human review.
6. Support-message classification and response drafting.
7. Natural-language admin reporting over deterministic database aggregates.

All actions that change orders, inventory, users or reservations should remain deterministic application actions with explicit authorization rather than free-form model actions.
