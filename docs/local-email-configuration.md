# Local Email Configuration

AI Readiness Consultant Hub follows the same local SendGrid configuration pattern as the working AcademicCostPlanner web project.

## Working Pattern Copied From AcademicCostPlanner

AcademicCostPlanner uses ASP.NET Core user secrets on its web project:

```xml
<UserSecretsId>f85a144e-7e24-491c-b12f-564fbc26bb32</UserSecretsId>
```

AI Readiness Consultant Hub uses the same `UserSecretsId` so it can read the same local SendGrid settings without committing any secret values.

Required local user-secrets keys:

```text
SMTP_PASSWORD
Smtp:FromEmail
```

Optional:

```text
Smtp:FromName
```

The app reads the same canonical configuration keys as AcademicCostPlanner:

```csharp
configuration["SMTP_PASSWORD"]
configuration["Smtp:FromEmail"]
configuration["Smtp:FromName"]
```

## Verify Local Secrets Without Printing Values

```bash
dotnet user-secrets list --project "AI Readiness Hub.csproj"
```

Avoid sharing the output because it includes secret values.

To set or refresh values:

```bash
dotnet user-secrets set "SMTP_PASSWORD" "..." --project "AI Readiness Hub.csproj"
dotnet user-secrets set "Smtp:FromEmail" "you@example.com" --project "AI Readiness Hub.csproj"
dotnet user-secrets set "Smtp:FromName" "AI Readiness Consultant Hub" --project "AI Readiness Hub.csproj"
```

## Run Locally

Run in Development so user secrets are loaded:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5091
```

or use the existing launch profile:

```bash
dotnet run --launch-profile http
```

## Render / Production

Render still uses environment variables:

```bash
SMTP_PASSWORD="..."
Smtp__FromEmail="you@example.com"
Smtp__FromName="AI Readiness Consultant Hub"
```

ASP.NET Core maps `Smtp__FromEmail` to `Smtp:FromEmail` and `Smtp__FromName` to `Smtp:FromName`.

## Diagnostics

In Development, startup logs show whether each email key is present in:

- environment variables
- user secrets
- appsettings

The logs never print the SMTP password or sender values. If required configuration is missing, sending fails with a precise missing-key message and the assessment is not marked as sent.
