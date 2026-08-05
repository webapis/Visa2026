---
title: Sign in to Visa2026
slug: getting-started/login
locale: en
tier: 0
guideStatus: review
lastReviewed: "2026-08-05"
roles: [Visa Officer]
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: login-sign-in.mp4
videoSource: recordings/passport-create.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/REPORT_DASHBOARD.md
  - docs/USAGE_LICENSE_LOGIN_BANNER.md
---

# Sign in to Visa2026

This guide shows how visa officers open Visa2026 and sign in. When you finish, you should see the **Report Dashboard** home page.

!!! tip "Screenshots"
    Images below are from the **English** application UI (version **2026.08**). If your office uses Turkish, Turkmen, or Russian, the labels are translated but the steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../assets/videos/v2026.08/en/login-sign-in.mp4"
  title="Sign in to Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | From |
|----------|------|
| **Web address** (URL) for Visa2026 | Your IT team or supervisor |
| **User name** and **password** | Your supervisor or system administrator |

Use a supported browser (Microsoft Edge or Google Chrome). Keep your password private — do not share it with colleagues.

## Step 1 — Open the sign-in page

1. Open your browser.
2. Go to the Visa2026 address your IT team gave you (for example `https://visa.your-company.local/`).
3. Wait until the sign-in page loads.

You should see the application title **Visa Management** and a sign-in form with **User Name** and **Password**.

![Sign-in page with User Name, Password, and Log In](../../assets/screenshots/v2026.08/en/login-step-01-logon.png)

!!! note "Trial notice (optional)"
    Some installations show a short **trial license** notice at the top of the sign-in page. It is informational only — you can still sign in when the notice is shown.

## Step 2 — Enter your credentials

1. Click in the **User Name** field and type the user name you were given.
2. Click in the **Password** field and type your password.
3. Check that **Caps Lock** is off if the password fails.

The sign-in form may show the message: *Enter your user name and password in the boxes below.*

## Step 3 — Sign in

1. Select **Log In**.
2. Wait a few seconds while the application loads your workspace.

If the user name or password is wrong, the form stays on the sign-in page. Try again or contact your administrator — do not guess repeatedly.

## Step 4 — Confirm you reached the home page

After a successful sign-in, Visa2026 opens the **Report Dashboard**. This is the officer home page with charts and summary cards for visa-related work.

You should see:

- **Report Dashboard** in the navigation or page title area
- The main **navigation menu** on the left (menus depend on your assigned role)
- The application header with your user menu and notification bell

![Report Dashboard after sign-in](../../assets/screenshots/v2026.08/en/login-step-02-report-dashboard.png)

!!! success "You are signed in"
    If you see **Report Dashboard** and can open items in the left menu (for example **Employees**), sign-in succeeded.

## Sign out

When you finish your session:

1. Open your **user menu** in the application header (top of the page).
2. Select **Log Off** (or the equivalent sign-out action shown in your language).
3. Close the browser tab if you are on a shared computer.

Always sign out on shared PCs so the next person cannot use your account.

## Common problems

| Problem | What to do |
|---------|------------|
| Page does not load | Check the URL with IT; confirm VPN or network access if required |
| **Log In** does nothing / error message | Verify user name and password; ask administrator to reset password |
| Blank page after sign-in | Wait 30 seconds and refresh once; if still blank, report to IT with the time and your user name |
| Expected menu missing | Your role may not include that area — ask your supervisor, do not use another officer's account |

## What to read next

- [Main navigation](navigation.md) — left menu, lists, detail forms, and header tools
- [Register a new employee](../guides/employee/register.md) — create an employee from the **Employees** list
