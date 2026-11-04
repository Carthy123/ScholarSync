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

1. Open **Attendance**.
2. Select the class and date.
3. Mark each student as present, absent, or late.
4. Save attendance.

Use **Reports → Attendance** to filter and review attendance records.

## Grades and report cards

1. Open **Grades**.
2. Select a student, subject, term, and academic year.
3. Enter the score and save.
4. Open **Reports** to select a student and view or print the report card.

Saving the same student, subject, term, and academic year again updates the existing result instead of creating a duplicate.

The report card uses the school name, crest, colors, academic year, and contact details saved in School Settings.

## Fees

Administrators first create fee structures under **Fees → Fee Structures**. Payments are recorded under **Fees**.

1. Select the student, term, academic year, amount, and payment date.
2. Add an optional note.
3. Save the payment.
4. Select **Receipt** to view or print the receipt.

Use the balance section to compare expected fees, payments, and outstanding amounts.

The system rejects an exact repeated payment and prevents a receipt number from being reused. Separate installment payments remain allowed when their date, amount, or receipt number differs.

## Login security

Signed-in sessions expire after 30 minutes of inactivity and extend while the account is actively used. Successful logins, failed login attempts, and sign-outs appear in Audit History. Passwords are never written to the audit log.

After five unsuccessful attempts, an account is temporarily locked for 10 minutes. This protects the account from repeated password-guessing attempts. A successful login resets the failed-attempt counter.

Administrators can open **Audit History** to review login activity, see currently locked accounts, and select **Unlock now** when a verified staff member needs immediate access.

Use the search box or action filter on Audit History to narrow the activity list by username, action, area, or details.

On the **Students** page, apply any search or class/year filter and select **Export CSV**. The download contains the matching student and contact details. Each export is recorded in Audit History with the administrator, time, filters, and number of records. Archived students are excluded unless an administrator selects **Show archived**.

The dashboard’s **Upcoming activities** panel shows the next calendar events. The **Needs attention** panel links directly to incomplete attendance, unassigned students, outstanding fee balances, and locked accounts that require review.

The dashboard analytics section summarizes the last seven days of attendance, current-year fee collection, average grade performance, passing results, and today’s attendance completion. Percentages are calculated from the records visible to the signed-in user.

## Navigation and printing

Use the dashboard menu to move between modules. Back buttons are available on detailed pages. For printed reports and receipts, select the relevant **Print** button and choose the printer or PDF option in the browser dialog.

## School calendar

Open **School Calendar** to view the monthly schedule. Administrators can select **Add activity**, choose a type such as Exam, Excursion, Midterm, Vacation, or Holiday, set the dates, and add optional notes and location. Activities can be removed from the calendar, and these changes appear in Audit History.
