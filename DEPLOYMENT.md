# Deployment Guide

## Local development

Requirements:

- .NET 8 SDK
- Visual Studio or the .NET CLI

Run from the project folder:

```powershell
dotnet run
```

The normal local profile uses the configured HTTPS/HTTP launch settings. The `demo` profile runs a separate demonstration database using `ScholarSync.Demo.db`.

## Temporary demonstration URL

For a short demonstration from a local computer, a tunnel service such as Cloudflare Tunnel can expose the running application. Start the application first, then point the tunnel to the actual local port. This is suitable for testing only and should not be treated as permanent hosting.

## Production settings

Do not commit passwords, SMTP credentials, database files, or publish profiles. Configure these values through the hosting provider’s environment settings:

- `ConnectionStrings__DefaultConnection`
- `Smtp__Host`
- `Smtp__Port`
- `Smtp__Username`
- `Smtp__Password`
- `Smtp__FromAddress`
- `Smtp__EnableSsl`

SQLite requires persistent storage. The database file and uploaded crest files must be stored on persistent disk. For multiple schools or higher traffic, move from SQLite to a managed database and use managed object storage for uploads.

## Azure App Service

The repository contains an Azure deployment workflow. Configure the required GitHub environment values before using it:

- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`

Configure production application settings in Azure, confirm persistent storage, and test the deployed URL before sharing it with a school.

## Release checklist

- Build succeeds with no warnings or errors.
- Production connection settings are configured securely.
- Admin account exists and uses a strong password.
- School branding and academic year are configured.
- Classes and teacher assignments are reviewed.
- Database backup and restore procedures are understood.
- Login, password reset, student registration, attendance, grades, fees, printing, and reports are tested.
