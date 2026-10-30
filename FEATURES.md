# ScholarSync Features

ScholarSync is a school management platform designed for Ghanaian schools. It uses ASP.NET Core Razor Pages, Entity Framework Core, and SQLite.

## School management

- Dashboard statistics for students, classes, attendance, grades, and fees.
- School branding through name, crest, colors, welcome message, academic year, address, phone, and email.
- Ghanaian class structure from Creche, KG, and Grade 1–9.
- Central academic-year setting used by classes and fee workflows.

## Students and academic records

- Student registration and editing.
- Guardian, emergency contact, email, and address information.
- Search, filtering, pagination, details, and CSV export.
- Attendance entry and attendance reports.
- Grade entry using Ghana-focused subjects, including OWOP and History.
- Printable student report cards with school branding.
- Promotion to the next academic year while preserving historical enrollment records.

## Classes and teachers

- Class creation and class lists.
- Printable class lists.
- Teacher-to-class assignment.
- Teachers are restricted to their assigned classes and students.
- Administrators can manage all classes and records.

## Fees

- Fee structures by class, term, and academic year.
- Payment recording and receipt numbers.
- Student balance calculations showing expected, paid, and outstanding amounts.
- Printable branded payment receipts.

## Security and administration

- Admin and Teacher roles.
- Login and logout.
- Password change and reset workflows.
- Admin user management.
- Audit history for important actions.
- SQLite database backup through the application.
- Database compatibility checks that add new columns to existing installations.
