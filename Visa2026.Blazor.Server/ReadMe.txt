Project Description

This project implements an ASP.NET Core Blazor Server application (https://docs.microsoft.com/en-us/aspnet/core/blazor/).
The root project folder contains the BlazorApplication.cs file with the class that inherits
BlazorApplication. This class allows you to view and customize application components: referenced modules,
security settings, data connection. Additionally, the root folder contains
Application Model difference files (XAFML files) that keep settings
specific to the current application. Difference files can be customized in code
or in the Model Editor.
The appsettings.json file contains database connection, logging, and theme settings (https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration).


Relevant Documentation

Application Solution Components
https://docs.devexpress.com/eXpressAppFramework/112569

Debugging, Testing and Error Handling
https://docs.devexpress.com/eXpressAppFramework/112572

XafApplication Class
https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.XafApplication

Application Model (UI Settings Storage)
https://docs.devexpress.com/eXpressAppFramework/112579

Model Editor
https://docs.devexpress.com/eXpressAppFramework/112582

Blazor host model files (important)
------------------------------------
- Model.xafml — layout / navigation / view structure (edit in Model Editor in en-US / Default).
- Model.tr-TR.xafml, Model.tk-TM.xafml, Model.ru-RU.xafml — translated captions (regenerate with tools/GenerateModelLocalization; do not hand-edit for routine UI text).

When appsettings lists multiple Languages, the Model Editor may create empty Model_{culture}.xafml files (underscore). Those are not used by this project and are gitignored; a pre-build step deletes them. Never add Model_*.xafml to the csproj as Content — use Model.{culture}.xafml only (see Content Include at top of Visa2026.Blazor.Server.csproj).

For translated strings, edit UiStrings*.json and run GenerateModelLocalization. For layout-only changes, keep Model Editor language on Default/en-US so only Model.xafml changes.

ASP.NET Core Blazor UI
https://docs.devexpress.com/eXpressAppFramework/401675/overview/supported-ui-platforms#aspnet-core-blazor-ui

Frequently Asked Questions - Blazor UI (FAQ)
https://docs.devexpress.com/eXpressAppFramework/403277/support-qa-troubleshooting/frequently-asked-questions-blazor-faq

Backend WebApi Service
https://docs.devexpress.com/eXpressAppFramework/403394/backend-web-api-service

XAF Community Extensions
https://www.devexpress.com/products/net/application_framework/#extensions