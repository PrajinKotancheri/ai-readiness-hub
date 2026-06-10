# Stakeholder workflow v2 foundation

This document tracks the Phase 1 foundation for the stakeholder-driven AI Readiness Consultant Hub workflow.

## Implemented in Phase 1

- Workspace timeline normalized to the 16-stage stakeholder workflow:
  - Client Created
  - Assessment Sent
  - Assessment Completed
  - Knowledge Gap Analysis
  - Follow-up Discovery
  - Evidence Collected
  - Company Summary
  - Readiness Score
  - Industry Analysis
  - Competitor Analysis
  - SWOT Analysis
  - Use Case Identification
  - Use Case Scoring
  - Roadmap Generation
  - Strategic Report
  - Final Review / Approved
- Knowledge Gap Analysis data model, lazy workspace tab, source attribution, manual edit, answered, approved, and discovery-agenda support.
- Prompt Inventory settings page with seeded prompt placeholders and CSV export.
- Source Attribution foundation via `AIOutputSources`.
- Draft / approve lifecycle foundation for AI outputs, report sections, and knowledge gaps.
- Strategic report template foundation with the stakeholder report section structure.
- Readiness score foundation updated to the strategic dimensions and adoption labels:
  - Observer
  - Cautious Adopter
  - Leader

## Intentionally not implemented yet

- Real OpenAI calls.
- Full AI workspace chat.
- Full multi-person delegation.
- Full curated use-case library.
- Web research automation.
- Document AI ingestion.

## Local verification

Build and JavaScript syntax checks:

```bash
dotnet build --no-restore
node --check wwwroot/js/site.js
```

Migration generation requires a PostgreSQL connection string. For design-time migration generation only, a harmless local placeholder can be supplied:

```bash
env 'ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=ai_readiness_hub_local;Username=ai_readiness;Password=ai_readiness_dev_password' dotnet ef migrations add StakeholderWorkflowV2Foundation
```

To run the app or apply migrations locally, start the local Docker PostgreSQL service and configure the shell to use the host port `5433`. This avoids conflicts with an existing macOS PostgreSQL service on port `5432`.

```bash
docker compose -f docker-compose.local.yml up -d
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://127.0.0.1:5091"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ai_readiness_hub_local;Username=ai_readiness;Password=ai_readiness_dev_password"
dotnet ef database update
dotnet run --no-launch-profile
```

Do not commit real database passwords, API keys, SMTP passwords, webhook secrets, or local-only appsettings files.
