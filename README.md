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
5. Configure SMTP in user secrets or environment settings before testing password reset emails.

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
