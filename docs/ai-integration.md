# AI integration foundation

Phase 2 adds a configurable AI provider layer and AI Workspace refinement flow. The platform remains an AI-assisted consultant workflow: generated output stays draft until a consultant edits, saves, and approves it.

## Configuration

Safe defaults live in `appsettings.json`. Secrets do not.

Supported keys:

```bash
AI__Provider=Mock
AI__Model=gpt-5-nano
AI__Temperature=0.2
AI__MaxOutputTokens=1800
AI__MaxInputCharactersPerRequest=30000
AI__EnableExternalResearch=false
AI__RequireApprovalBeforeChaining=false
```

`OPENAI_API_KEY` is read from environment variables or user secrets only. Do not commit it to git, appsettings, docs, logs, or the database.

## Mock mode

Mock mode is the local default and requires no API key.

```bash
export AI__Provider=Mock
```

## OpenAI mode

OpenAI mode is optional. Configure it only in your local shell or user secrets:

```bash
export AI__Provider=OpenAI
export AI__Model=gpt-5-nano
```

Configure the OpenAI credential through your local shell or user secrets process. Do not store it in repository files.

If `AI__Provider=OpenAI` and `OPENAI_API_KEY` is missing, the app logs and shows:

```text
OpenAI provider is enabled but OPENAI_API_KEY is not configured.
```

The app does not crash.

## Local PostgreSQL run commands

```bash
docker compose -f docker-compose.local.yml up -d
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://127.0.0.1:5091"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ai_readiness_hub_local;Username=ai_readiness;Password=ai_readiness_dev_password"
export AI__Provider=Mock
dotnet ef database update
dotnet run --no-launch-profile
```

## Prompt Inventory

AI operations first look for an active `PromptDefinition` with matching `PromptName`. If the active prompt is missing or still contains placeholder text, the service uses a safe built-in default prompt.

Supported prompt variables include:

- `{{ClientProfile}}`
- `{{AssessmentSummary}}`
- `{{AssessmentAnswers}}`
- `{{KnowledgeGaps}}`
- `{{ApprovedCompanySummary}}`
- `{{ApprovedIndustryAnalysis}}`
- `{{ApprovedCompetitorAnalysis}}`
- `{{ApprovedSWOT}}`
- `{{ApprovedUseCases}}`
- `{{ApprovedRoadmap}}`
- `{{DocumentsSummary}}`
- `{{TranscriptSummary}}`
- `{{ConsultantNotes}}`
- `{{SourceList}}`
- `{{CurrentDraft}}`
- `{{ConsultantFeedback}}`
- `{{PreviousMessages}}`

## AI Workspace

Supported generated outputs can open an AI Workspace. The workspace stores:

- AI workspace sessions
- consultant and assistant messages
- draft revision history
- approved final revisions

Approving from the workspace updates the original draft output. Later workflow stages should use approved outputs where available.
