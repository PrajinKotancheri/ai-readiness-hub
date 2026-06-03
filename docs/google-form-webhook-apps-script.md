# Google Form Webhook Apps Script

The AI Readiness Consultant Hub can send a client-specific Google Form link and receive a submitted-response notification through a Google Apps Script webhook.

## Flow

1. Configure `/Settings/ReadinessForm` with the default Google Form URL, Client Reference Entry ID, email templates, and webhook secret.
2. The consultant generates a client-specific form link in the client workspace.
3. The generated link pre-fills the Google Form question that stores the client token.
4. The client submits the Google Form.
5. A Google Apps Script trigger posts the response to the app webhook.
6. The app matches the response by `token`, stores the Google response id, marks the assessment completed, and logs activity.

## Requirements

- The Google Form must include a question named `Client Reference ID`.
- Configure that question's entry ID in `/Settings/ReadinessForm`, for example `entry.123456789`.
- Google Form responses should be linked to a Google Sheet.
- Apps Script should run on form submit from the linked Google Sheet.
- The Apps Script posts JSON to:

```text
https://ai-readiness-hub.onrender.com/api/form-response
```

## Sample Apps Script

```javascript
function onFormSubmit(e) {
  const WEBHOOK_URL = "https://ai-readiness-hub.onrender.com/api/form-response";
  const SECRET = "CHANGE_ME";
  const CLIENT_REFERENCE_QUESTION = "Client Reference ID";

  const namedValues = e.namedValues;
  const clientTokenValues = namedValues[CLIENT_REFERENCE_QUESTION] || [];
  const clientToken = clientTokenValues.length > 0 ? clientTokenValues[0] : "";

  const payload = {
    token: clientToken,
    responseId: Utilities.getUuid(),
    secret: SECRET,
  };

  const options = {
    method: "post",
    contentType: "application/json",
    payload: JSON.stringify(payload),
    muteHttpExceptions: true
  };

  const response = UrlFetchApp.fetch(WEBHOOK_URL, options);
  Logger.log(response.getResponseCode());
  Logger.log(response.getContentText());
}
```

## Setup Steps

1. Open the Google Sheet connected to the Google Form.
2. Go to `Extensions > Apps Script`.
3. Paste the script.
4. Replace `WEBHOOK_URL` and `SECRET`.
5. Add a trigger:
   - function: `onFormSubmit`
   - event source: `From spreadsheet`
   - event type: `On form submit`
6. Submit a test form response.
7. Confirm the app marks the readiness form response as received in the dashboard or client workspace.
