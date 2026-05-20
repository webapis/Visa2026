# Sample photos for `employee-photo-roster` preview

PNG files here are loaded by `tools/PreviewWordReports` preset **`employee-photo-roster`** (no Blazor / no database).

| File | Display name in preview |
|------|-------------------------|
| `ali_zengin.png` | Ali Zengin |
| `adnan_tufan.png` | Adnan Tufan |
| `alik_murat_kurt.png` | Alik Murat Kurt |
| `alper_unluer.png` | Alper Unluer |

Replace or add files and update `EmployeePhotoRosterSampleRows()` in `Program.cs` if you need more demo rows.

```powershell
dotnet run --project tools/PreviewWordReports -- employee-photo-roster
```

Output: `tools/PreviewWordReports/bin/Debug/net8.0/out/employee_photo_roster_preview.docx` (open in Word).
