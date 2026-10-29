# Troubleshooting

## SQLite says a column does not exist

Stop any running copy of the application and start it again. ScholarSync checks the database at startup and adds supported missing columns. If the error continues, confirm that the application is using the intended database file and that the database is not read-only.

## SQLite says the database is being used by another process

Close the running application, stop debugging in Visual Studio, and close any database browser or backup process using the `.db` file. Start the application again and retry the backup.

## SQLite does not support a decimal Sum query

The application calculates fee totals in memory after loading the required records. Make sure the latest project version is running and rebuild the application if an older error appears.

## No classes appear when registering a student

Create classes for the selected academic year first. Confirm that the class records are not duplicated and that the student registration page is using the same academic year.

## A teacher cannot see a student or class

Teachers can only access classes assigned to them. An administrator should open the teacher assignment page and assign the correct class for the correct academic year.

## Password reset email does not arrive

Confirm the SMTP settings, sender address, recipient email, port, SSL setting, and hosting provider restrictions. For local development, check the application output and SMTP provider logs.

## The crest does not appear

Confirm that the file is PNG, JPG, JPEG, or WEBP and smaller than 2 MB. Re-upload it from School Settings and confirm that the `wwwroot/uploads` folder is writable and persistent on the host.

## The dashboard looks different after changing colors

Refresh the browser and clear cached styles if necessary. Confirm that the saved primary and accent colors are valid six-digit hexadecimal values.

## The HTTPS server stopped in Visual Studio

Stop the application, close any process using its port, and run the project again. If necessary, use the HTTP launch profile to confirm that the application itself is working.

## Getting diagnostic details

For a useful error report, record the page, action, time, exact message, browser, launch profile, and whether the issue occurs with a fresh demonstration database. Do not share passwords, SMTP credentials, or database files publicly.
