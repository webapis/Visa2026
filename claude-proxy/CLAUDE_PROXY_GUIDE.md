# Claude AI Proxy Configuration Guide

This guide explains how to use the automation scripts to route **Claude Code (CLI)** and the **VS Code Claude extension** through the Psiphon VPN. This is necessary if your Internet Service Provider (ISP) restricts access to Anthropic's services.

## 1. Prerequisites

1.  **Psiphon VPN**: Ensure Psiphon is installed and connected.
2.  **Identify the Port**:
    *   Open Psiphon.
    *   Navigate to **Settings** > **Local Proxy Ports**.
    *   Note the **HTTP/HTTPS** port number (e.g., `8080`, `52143`).

## 2. Using the Setup Script

The `Setup-Proxy.ps1` script automates the configuration of your terminal environment and VS Code settings.

### Execution
1.  Open PowerShell.
2.  Navigate to the repository root: `C:\Users\IT\source\repos\Visa2026\`
3.  Run the script:
    ```powershell
    .\Setup-Proxy.ps1
    ```
4.  **Enter Port**: When prompted, type the port number from Psiphon.
5.  **Permanent Setup**: You will be asked if you want to set these variables permanently for your Windows User.
    *   Select **Y** if you want Claude to work every time you open a terminal.
    *   Select **N** if you only want it for the current session.

### What it does:
*   **Connection Validation**: Verifies that Psiphon is actually listening on the provided port before applying settings.
*   **Environment Variables**: Sets `HTTP_PROXY` and `HTTPS_PROXY` so the `claude` CLI tool knows where to route traffic.
*   **SSL Bypass**: Disables Node.js SSL strict verification (`NODE_TLS_REJECT_UNAUTHORIZED=0`). This is required because Psiphon's tunnel acts as a local intermediary.
*   **VS Code Integration**: Updates your global `settings.json` to use the proxy and creates a `.bak` backup of your original settings.

## 3. Using the Revert Script

If you stop using Psiphon or move to an unrestricted network, use `Revert-Proxy.ps1` to clean up your system configurations.

### Execution
1.  Open PowerShell.
2.  Run the script:
    ```powershell
    .\Revert-Proxy.ps1
    ```

### What it does:
*   Clears proxy environment variables from the current session.
*   Deletes permanent Windows User environment variables (`HTTP_PROXY`, `HTTPS_PROXY`, `NODE_TLS_REJECT_UNAUTHORIZED`).
*   Removes the `http.proxy` and `http.proxyStrictSSL` entries from VS Code `settings.json`.

## 4. Troubleshooting & Security

*   **Security Note**: Disabling SSL verification (`NODE_TLS_REJECT_UNAUTHORIZED=0`) reduces protection against man-in-the-middle attacks. Only use this configuration when Psiphon is active and required.
*   **VS Code Restart**: If VS Code does not pick up the changes immediately, restart the editor.
*   **Port Changes**: Psiphon occasionally changes its local port on restart. If Claude stops working, check the port in Psiphon and run `Setup-Proxy.ps1` again with the new number.
*   **Terminal Refresh**: If you set variables permanently, existing terminal windows will not see them. Open a new PowerShell window to see the effect.

---
*Created for the Visa2026 Project environment.*
