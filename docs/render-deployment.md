# Render Deployment

This app is prepared for Render deployment with Docker and PostgreSQL. SQLite is not supported at runtime.

## Deploy

1. Commit and push the repository to GitHub.
2. Use the Supabase PostgreSQL connection string for this app.
3. Create a Render Web Service from the GitHub repository.
4. Select Docker runtime.
5. Add environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:10000
ConnectionStrings__DefaultConnection=Host=<supabase-host>;Port=6543;Database=postgres;Username=<supabase-user>;Password=<supabase-password>;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=60;Command Timeout=60
RunMigrationsOnStartup=true
```

Do not commit the Supabase password. Set the same connection string in Render environment variables.

If the Supabase password contains a semicolon `;` or other connection-string-breaking characters, reset it to a safer password or escape it correctly before adding it to Render.

## Data Protection / Antiforgery

Production persists ASP.NET Core Data Protection keys in the PostgreSQL database so antiforgery tokens survive Render redeploys and restarts.

No additional Render environment variable is required. The `DataProtectionKeys` table is created by EF migrations when `RunMigrationsOnStartup=true`.

Keep the PostgreSQL connection string private. The Data Protection key ring is sensitive runtime material and must remain in the production database, not source control.

## Email Settings

The AI Readiness Consultant Hub follows the same SendGrid-style configuration pattern as the AcademicCostPlanner project.

Required production environment variables:

```text
SMTP_PASSWORD=<SendGrid API key>
Smtp__FromEmail=<verified sender email>
Smtp__FromName=AI Readiness Consultant Hub
```

No SMTP password, API key, or secret should be committed to the repository. If required email settings are missing, sending fails with a precise missing-key message and the assessment is not marked as sent.

## Google Form Settings

Configure these values in the running app at `/Settings/ReadinessForm`:

- Default Google Form URL
- Client Reference Entry ID, for example `entry.123456789`
- Email subject template
- Email body template
- Webhook secret

The Google Apps Script webhook URL should use:

```text
https://ai-readiness-hub.onrender.com/api/google-forms/assessment-response
```

## Check Deployment

1. Deploy.
2. Check Render logs for migration and startup output.
3. Open:

```text
https://YOUR-APP.onrender.com/Dashboard
```

4. Configure `/Settings/ReadinessForm`.
5. Open a client workspace and generate a Google Form link.
6. Send a test assessment email or use manual import fallback.

## Troubleshooting

- Wrong DLL name: the Dockerfile starts `AI Readiness Hub.dll`; keep the project file and published DLL name aligned.
- App not binding: verify `ASPNETCORE_URLS=http://0.0.0.0:10000` and that Docker exposes port `10000`.
- Missing PostgreSQL connection string: set `ConnectionStrings__DefaultConnection` to the Supabase PostgreSQL connection string.
- Migrations not applied: check startup logs for `Applying database migrations...` and `Database migrations completed.`
- Database provider configured incorrectly: remove `DatabaseProvider` or set it to `Postgres`; the app supports PostgreSQL only.
- Antiforgery token could not be decrypted after deploy: confirm the `DataProtectionKeys` table exists and that `RunMigrationsOnStartup=true` ran successfully against the same PostgreSQL database used by the web service.
- Webhook URL still localhost: update Apps Script to `https://ai-readiness-hub.onrender.com/api/google-forms/assessment-response`.
- Missing email configuration: set `SMTP_PASSWORD`, `Smtp__FromEmail`, and optionally `Smtp__FromName`.
- Google Form client token not submitted: confirm the form contains a `Client Reference ID` question and the generated link pre-fills it.
- Wrong Client Reference Entry ID: inspect the Google Form prefilled link and update `/Settings/ReadinessForm`.
