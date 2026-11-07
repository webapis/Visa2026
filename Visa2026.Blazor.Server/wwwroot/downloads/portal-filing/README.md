# Portal Filing worker downloads

Admin download folder for the **Playwright worker** Windows zip (filling PC / RDP desktop).

Zips are **not** committed. Produce them with:

```powershell
.\scripts\local\Publish-PortalFilingWorker.ps1 -CopyToBlazorDownloads
```

Then (after IIS/Docker publish of Blazor) an admin can fetch:

`/downloads/portal-filing/Visa2026.PortalFiling.Worker-win-x64-latest.zip`

A signed-in Admin-only download UI can wrap this path later. Do not put secrets in the zip.