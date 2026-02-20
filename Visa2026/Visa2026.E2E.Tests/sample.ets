﻿; This script automates the login process and navigates to the Tasks view.

; Fill the login form
*FillForm
 User Name = Admin
*Action Log In

; Navigate to the Tasks module after logging in
*Navigate Tasks