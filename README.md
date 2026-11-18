# ScholarSync

ScholarSync is a Ghanaian school management system built with ASP.NET Core Razor Pages, Entity Framework Core, and SQLite.

## Included features

- Student registration, search, filtering, pagination, and CSV export
- Ghanaian school classes from Creche through Grade 9
- Attendance and attendance reports
- Grades with Ghana-focused subjects, including OWOP and History
- Student report cards with school branding and crest upload
- Admin and Teacher accounts with role-based access
- Password reset by email and change-password support
- Audit history, database backup, and academic-year promotion
- Teacher-to-class assignment
- Guardian and emergency contact information
- Fee payment recording and dashboard totals
- Printable class lists

## Documentation

- [Features](FEATURES.md) — complete implementation summary
- [User Guide](USER_GUIDE.md) — daily instructions for administrators and teachers
- [Administrator Setup](ADMIN_SETUP.md) — school onboarding and configuration
- [Deployment Guide](DEPLOYMENT.md) — local, demo, and production hosting
- [Troubleshooting](TROUBLESHOOTING.md) — common errors and solutions

## Automated tests

Run the foundational tests locally with:

```powershell
dotnet test ScholarSync.Tests/ScholarSync.Tests.csproj
```

GitHub Actions runs these tests automatically for pushes and pull requests.

## Run locally

1. Install the .NET 8 SDK.
2. Open the project folder in Visual Studio.
3. Run the project with the `http` profile.
4. On the first run, open `/Account/Setup` and create the first Admin account.
5. Configure SMTP in user secrets or environment settings before testing password reset or guardian emails. For Gmail, use `smtp.gmail.com`, port `587`, SSL enabled, the Gmail address as both `Smtp__Username` and `Smtp__FromAddress`, and a Google App Password as `Smtp__Password`. Google requires 2-Step Verification before an App Password can be created. See [Google's App Password guidance](https://support.google.com/mail/answer/185833).

For a clean school demonstration, run the `demo` launch profile. It uses `ScholarSync.Demo.db`, so demonstration records remain separate from the normal local database.

The SQLite database is created as `ScholarSync.db` and is intentionally excluded from Git.

## Production configuration

Do not commit passwords, SMTP credentials, database files, or publish profiles. Configure these through the hosting provider’s application settings:

- `ConnectionStrings__DefaultConnection`
- `Smtp__Host`
- `Smtp__Port`
- `Smtp__Username`
- `Smtp__Password`
- `Smtp__FromAddress`
- `Smtp__EnableSsl`

For local PowerShell testing, keep the password out of files and set it as a user secret or environment variable, for example:

```powershell
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:Username" "your-school-gmail@gmail.com"
dotnet user-secrets set "Smtp:FromAddress" "your-school-gmail@gmail.com"
dotnet user-secrets set "Smtp:Password" "your-16-character-app-password"
dotnet user-secrets set "Smtp:EnableSsl" "true"
```

Do not use the normal Gmail account password. Generate a dedicated App Password after enabling 2-Step Verification, and revoke it if the application no longer needs access.

### SMS and WhatsApp with Twilio

ScholarSync uses Twilio for both SMS and WhatsApp delivery. Configure these values as user secrets or hosting environment settings:

- `Twilio__AccountSid`
- `Twilio__AuthToken`
- `Twilio__SmsFrom`
- `Twilio__WhatsAppFrom`

The SMS sender must be a Twilio-approved sender. The WhatsApp sender must be configured as `whatsapp:+number` in Twilio; the application adds the `whatsapp:` prefix when it calls the API. Guardian phone numbers must use international format, such as `+233241234567`. Twilio returns a provider message ID, which ScholarSync stores with the communication status. See [Twilio's SMS guide](https://www.twilio.com/docs/messaging/tutorials/how-to-send-sms-messages) and [Twilio's WhatsApp guide](https://www.twilio.com/docs/whatsapp/quickstart).

For a hosted deployment, use persistent storage for the SQLite database and uploaded school crest files, or move the application to a managed database and object storage before production use.

## GitHub Actions

- `ci.yml` builds and publishes an artifact for pushes and pull requests.
- `deploy-azure.yml` prepares an Azure App Service package and deploys version tags such as `v1.0.0`.

The Azure workflow requires these GitHub environment secrets:

- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`

Configure production application settings in Azure before creating a release tag.

## Demonstration checklist

- Change the school name, colors, academic year, and crest.
- Create the school’s Admin and Teacher accounts.
- Add the school’s classes and assign teachers.
- Confirm SMTP settings if password reset will be demonstrated.
- Create a database backup before entering demonstration data.
- Test login, student registration, attendance, grades, reports, fees, and printing.
