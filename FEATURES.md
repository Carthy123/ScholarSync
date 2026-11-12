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
- Safe student archiving and administrator restoration instead of permanent deletion.
- Attendance entry and attendance reports.
- Grade entry using Ghana-focused subjects, including OWOP and History.
- Grade records separated by subject, term, and academic year, with duplicate updates instead of duplicate entries.
- Printable student report cards with school branding.
- Promotion to the next academic year while preserving historical enrollment records.

## Classes and teachers

## School calendar

- Monthly school calendar for exams, excursions, midterms, vacations, holidays, and other activities.
- Administrators can add and remove activities; staff can view the shared calendar.
- Calendar changes are recorded in Audit History.

- Class creation and class lists.
- Printable class lists.
- Teacher-to-class assignment.
- Teachers are restricted to their assigned classes and students.
- Administrators can manage all classes and records.

## Fees

- Fee structures by class, term, and academic year.
- Payment recording and receipt numbers.
- Student balance calculations showing expected, paid, and outstanding amounts.
- Duplicate payment and receipt-number protection while still allowing legitimate installments.
- Printable branded payment receipts.

## Security and administration

- Admin and Teacher roles.
- Login and logout.
- Temporary account lockout after repeated failed login attempts.
- Password change and reset workflows.
- Admin user management.
- Audit history for important actions.
- Security monitoring with locked-account visibility and administrator unlock controls.
- Audit history search and action filtering.
- Filtered student CSV exports containing student, guardian, emergency-contact, class, and archive details, with export activity logged.
- CSV exports for attendance, grades, fee payments, fee balances, and calendar activities, with role filtering and audit logging.
- Managed backup history with latest-backup status and automatic retention of the 10 newest private backup copies.
- Dashboard alerts for upcoming school activities, incomplete attendance, unassigned students, outstanding fees, and locked accounts.
- Dashboard analytics for seven-day attendance, enrollment, fee collection, grade performance, and attendance completion.
- Notification center combining upcoming activities, incomplete attendance, unassigned students, fee balances, and locked accounts.
- Parent and Guardian Directory grouped by family contact with student, class, and emergency-contact details.
- Teacher workload dashboard showing assigned classes, class sizes, attendance completion, and grade-entry progress.
- Student Profile Timeline combining personal details, class history, attendance, grades, fees, and student activity with section navigation.
- School Operations Bulletin with priority, category, audience targeting, pinning, expiry, acknowledgement, dashboard display, and audit logging.
- Global Search Center for students, guardians, classes, grades, fees, calendar activities, and bulletins with role-aware results.
- 30-minute sliding login sessions with HTTP-only cookies.
- Audit entries for successful logins, failed login attempts, and sign-outs; passwords are never recorded.
- SQLite database backup through the application.
- Database compatibility checks that add new columns to existing installations.
