# Render Deployment

This app is prepared for Render deployment with Docker and PostgreSQL.

## Deploy

1. Commit and push the repository to GitHub.
2. Create a Render PostgreSQL database.
3. Create a Render Web Service from the GitHub repository.
4. Select Docker runtime.
5. Add environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:10000
DatabaseProvider=Postgres
ConnectionStrings__DefaultConnection=<Render internal PostgreSQL connection string>
```

The app also accepts `ConnectionStrings__PostgresConnection`, but `ConnectionStrings__DefaultConnection` is recommended for Render.

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
https://YOUR-RENDER-APP.onrender.com/api/google-forms/assessment-response
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
- Missing PostgreSQL connection string: set `ConnectionStrings__DefaultConnection` to the Render internal database URL.
- Migrations not applied: check startup logs for `Applying migrations and seed data...`.
- Database provider still SQLite: set `DatabaseProvider=Postgres`.
- Webhook URL still localhost: update Apps Script to `https://YOUR-RENDER-APP.onrender.com/api/google-forms/assessment-response`.
- Missing email configuration: set `SMTP_PASSWORD`, `Smtp__FromEmail`, and optionally `Smtp__FromName`.
- Google Form client token not submitted: confirm the form contains a `Client Reference ID` question and the generated link pre-fills it.
- Wrong Client Reference Entry ID: inspect the Google Form prefilled link and update `/Settings/ReadinessForm`.
