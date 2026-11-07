# Portal Filing worker — install (sample)

For the **filling PC** (local keyboard or **RDP** into that desktop). Not installed into IIS/Docker.

## Package contents

- `Visa2026.PortalFiling.Worker.exe` (+ runtime files)
- `install\Install-PortalFilingWorker.ps1`
- `appsettings.Local.json.example`
- `portal-filing-version.txt`

## Install

```powershell
# From the extracted zip folder:
powershell -ExecutionPolicy Bypass -File .\install\Install-PortalFilingWorker.ps1 `
  -InstallRoot C:\PortalFiling `
  -Visa2026BaseUrl https://your-visa2026-host/
```

Default layout:

```text
C:\PortalFiling\
  worker\            # exe + config
  edge-profile\      # portal login cookie (do not copy to other PCs casually)
  screenshots\
```

## After install

1. Edit `C:\PortalFiling\worker\appsettings.Local.json` (API user; never commit).
2. Sign into `rgstr.migration.gov.tm` once with Edge using the install profile (or run the worker and complete login when prompted later).
3. `C:\PortalFiling\worker\Visa2026.PortalFiling.Worker.exe` — scaffold mode exits after config OK until RPA is implemented.

## RDP note

Remote Desktop is only how an admin watches/unlocks the filling PC. The worker runs **on that PC**, not inside the RDP client and not inside the Visa2026 container.