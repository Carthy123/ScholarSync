# ScholarSync User Guide

This guide explains the daily use of ScholarSync. Administrators manage the school setup and users. Teachers work with the classes assigned to them.

## Signing in

1. Open the ScholarSync URL.
2. Enter your username and password.
3. Select **Login**.
4. Use **Forgot password** if password recovery has been configured.

Use **Change password** after signing in if you want to update your password. Select **Sign out** when finished.

## Dashboard

The dashboard provides a summary of the school. It includes student totals, class enrollment, attendance, recent grades, and fee information. Teachers see information for their assigned classes only.

## Students

### Bulk student import

Administrators can select **Learners**, then choose **Import students** on the student page to register multiple students at once.

1. Download the Excel/CSV template.
2. Open it in Excel and enter the student and guardian details. Use the exact class name and academic year already configured in ScholarSync; leave both blank when the learner is not yet assigned.
3. Save the file as CSV and upload it.
4. Review the preview. The system checks required names, dates, classes, duplicate rows, and existing active students.
5. Select **Import valid records** only when every row shows **Ready**.

The import is administrator-only, keeps archived records intact, and records the completed import in Audit History.

1. Open **Students**.
2. Select **Register student**.
3. Enter the student’s personal and guardian information.
4. Select the correct class and academic year.
5. Save the record.

Use the search and filters to find students. Open a student’s details page to view their current class, contacts, enrollment history, attendance, and grades.

Administrators should use **Archive** when a student leaves the school or a record should be removed from normal working lists. Archiving preserves the student’s history and removes the record from active dashboard totals. Administrators can select **Show archived** and use **Restore** when the record is needed again.

## Classes and promotion

Administrators can create classes and assign teachers. To promote students:

1. Open **Classes → Promote students**.
2. Select the source academic year and target academic year.
3. Review the proposed next class.
4. Confirm the promotion.

Previous enrollment records remain available. Students in the final class, Grade 9, are not assigned to another class automatically.

## Attendance

1. Open **School operations → Take attendance**.
2. Select the class and date.
3. Mark each student as present, absent, or late.
4. Save attendance.

The **School operations** page also provides a quick view of today’s attendance totals, upcoming calendar activities, and current school notices. The detailed attendance, calendar, bulletin, and notifications pages remain available from the buttons on that page.

Open **Results & grades → Attendance reports** to filter and review attendance records.

## Grades and report cards

1. Open **Grades**.
2. Select a student, subject, term, and academic year.
3. Enter the score and save.
4. Open **Results & grades → Student report cards** to select a student and view or print the report card.

Administrators can select **Manage subjects** on the Grades page to maintain the subject list. Add a subject and choose the school stage where it is offered. Active subjects appear in the grade-entry list, while archived subjects remain available for historical records and can be activated again later. The system also prevents a subject from being recorded against a class stage where it has not been configured.

Use **Classes → Assign subjects** to choose the exact subjects offered by each class. Select a class, tick the subjects it teaches, and save. Grade Entry then displays and accepts only the subjects assigned to that class.

Administrators can adjust the grading boundaries from **School Settings → Grading scale**. Enter the minimum score for A, B, C, and D; scores below the D boundary receive F. The boundaries must descend from A to D, and the updated scale is used in grade lists, dashboards, exports, student profiles, and report cards.

The dashboard’s **Grade distribution** panel counts how many recorded results fall into each grade. It follows the selected academic year and class filters, so administrators can review the whole school or a specific class.

Detailed term and subject analysis remains available from the academic reports pages, keeping the home dashboard focused on the school’s most important daily information.

Open **Grades → Academic reports** for detailed results. Filter by academic year, class, term, or subject to see result counts, averages, A–F totals, pass rates, and exportable CSV summaries.

Saving the same student, subject, term, and academic year again updates the existing result instead of creating a duplicate.

The report card uses the school name, crest, colors, academic year, and contact details saved in School Settings.

## Student health and welfare

Authorized staff can record medical conditions, allergies, blood group, medication or care instructions, doctor or clinic contacts, and welfare notes while registering or editing a student. These details appear in the protected **Health & welfare** section of the student profile and are not included in general CSV exports. Administrators can hide the entire section from **School Settings → Optional student information** when the school does not collect these details.

## Fees

Open **Fees & payments** to use the **Finance Hub**. Administrators can select **Fee structures** to set expected fees by class, term, and academic year. The hub records payments, shows student balances, prints receipts, and provides payment and balance exports.

1. Select the student, term, academic year, amount, and payment date.
2. Add an optional note.
3. Save the payment.
4. Select **Receipt** to view or print the receipt.

Use the balance section to compare expected fees, payments, and outstanding amounts.

The system rejects an exact repeated payment and prevents a receipt number from being reused. Separate installment payments remain allowed when their date, amount, or receipt number differs.

## Login security

Signed-in sessions expire after 30 minutes of inactivity and extend while the account is actively used. Successful logins, failed login attempts, and sign-outs appear in Audit History. Passwords are never written to the audit log.

After five unsuccessful attempts, an account is temporarily locked for 10 minutes. This protects the account from repeated password-guessing attempts. A successful login resets the failed-attempt counter.

Administrators can open **School Settings → Audit history** to review login activity, see currently locked accounts, and select **Unlock now** when a verified staff member needs immediate access.

Use the search box or action filter on Audit history to narrow the activity list by username, action, area, or details.

On the **Students** page, apply any search or class/year filter and select **Export CSV**. The download contains the matching student and contact details. Each export is recorded in Audit History with the administrator, time, filters, and number of records. Archived students are excluded unless an administrator selects **Show archived**.

The **Attendance**, **Grades**, **Fees**, and **School Calendar** pages also provide CSV export buttons. Fee exports include both payment records and the selected student balances. Exports are restricted to records visible to the signed-in teacher or administrator and are recorded in Audit History.

Open **School Settings → Backup and restore**. The system shows whether a recent managed backup exists, records backup history with size and creation time, and keeps only the 10 newest managed copies in the private application data folder. Use **Verify backup** regularly to create and validate a retained copy.

The dashboard’s **Upcoming activities** panel shows the next calendar events. The **Needs attention** panel links directly to incomplete attendance, unassigned students, outstanding fee balances, and locked accounts that require review.

The dashboard analytics section summarizes the last seven days of attendance, current-year fee collection, average grade performance, passing results, and today’s attendance completion. Percentages are calculated from the records visible to the signed-in user.

Open **School bulletin → Open notifications** to review reminders in one place. Select a notification to go directly to the related student, attendance, fees, calendar, or account-security page.

Open **Communications → Guardian directory** to search family contacts by guardian name, phone, email, or student name. Each guardian card shows connected students, their classes, contact information, and emergency-contact details. Teachers only see guardians connected to their assigned classes.

Open **Guardian communications** to select a student, choose In-app, Email, SMS, or WhatsApp, and create a message. In-app messages are recorded immediately. Email is delivered when SMTP is configured in application settings. SMS and WhatsApp messages are stored as pending until the school connects a provider. Communication status and staff activity are recorded for audit purposes.

Select **Message a class** to send one communication to all active students in a selected class. ScholarSync creates a separate record for each guardian, validates the available contact details, and records each delivery result individually. Bulk sends are limited to 200 students per operation for safety.

Administrators can open **Message templates** to create reusable messages. Templates can be assigned to a category and channel, then selected when sending an individual or class communication.

The dashboard keeps these related actions under the single **Communications** menu item. Open that page to access individual messaging, class messaging, templates, and communication history.

Student records include guardian communication preferences. Staff can record the preferred channel, mark contact details as verified, or record an opt-out request. Individual messages are blocked for opted-out guardians, and bulk messages are skipped when they do not match the guardian’s preference.

For Gmail delivery, the administrator must configure Gmail SMTP with a dedicated App Password. The normal Gmail password must not be used or stored in the project files. SMS and WhatsApp delivery uses Twilio and requires provider settings, approved senders, guardian phone numbers in international format, and a public HTTPS status-callback URL. The communication history stores the provider message ID and updates asynchronously to Sent, Delivered, Read, Failed, or Undelivered when Twilio reports a new status.

Teacher accounts see a **My teaching workload** section on the dashboard. It shows each assigned class, student count, attendance marked for today, and grade entries recorded for the current academic year.

Select **View** beside a student to open the Student Profile Timeline. The section links at the top move between the overview, attendance, class history, grades, fees, and activity sections. The action buttons provide quick access to edit the student, record a grade, record a fee, or view the report.

Administrators can open **School Bulletin** to publish operational notices. Choose a category, priority, audience, expiry date, and whether the notice should be pinned. Teachers and administrators can acknowledge notices addressed to them, and all bulletin activity is recorded in Audit History.

Use **Search school records** in the dashboard sidebar to search across the school workspace. Search results show the record type and a direct link. Teachers only receive results from their assigned classes.

Administrators can open **School Settings → Setup wizard** to configure the school name, academic year, contact details, welcome message, crest, and colors. The readiness section shows whether accounts, classes, and branding are ready, with links to create the first backup and manage each area.

## Navigation and printing

## Dashboard configuration

Administrators can open **School Settings → Dashboard sections** to choose which optional dashboard panels and quick-action shortcuts are visible. This lets each school keep the home screen focused on the work it uses most. Select or clear the sections and save the settings; the dashboard updates for signed-in staff without requiring a code change.

The **Dashboard preset** can quickly apply a **Full dashboard**, **Operations focus**, or **Academic focus** layout. Choose **Custom selection** when you want to control every section individually.

The **Teacher access** settings let administrators allow or restrict teacher access to **Fees & Payments** and **Reports**. Administrators always retain access, and restricted teachers receive an access-denied response even if they try to open the page directly.

The same area controls whether teachers can **register and edit students**. When disabled, teachers can still view students assigned to their classes, but create and edit pages are unavailable to them.

Use the dashboard menu to move between modules. Back buttons are available on detailed pages. For printed reports and receipts, select the relevant **Print** button and choose the printer or PDF option in the browser dialog.

## School calendar

Open **School Calendar** to view the monthly schedule. Administrators can select **Add activity**, choose a type such as Exam, Excursion, Midterm, Vacation, or Holiday, set the dates, and add optional notes and location. Activities can be removed from the calendar, and these changes appear in Audit History.
