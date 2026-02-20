﻿; This script automates the login process and navigates to the Tasks view.
﻿﻿; This script automates the login process and navigates to the Tasks view.

; Fill the login form
*FillForm
 User Name = Admin

; DEBUG: Take a screenshot before clicking Log In to verify the form is filled.
*ScreenShot BeforeLogin

*Action Log In

; DEBUG: Pause execution and attach the C# debugger to inspect the state after login.
*Execute Visa2026.E2E.Tests.TestDebugger.Break

; Navigate to the Tasks module after logging in
*Navigate Tasks