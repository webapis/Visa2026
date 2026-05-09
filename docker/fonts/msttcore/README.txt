Times New Roman in Docker images
=================================

The Blazor app image requires Times New Roman to be registered in fontconfig
(used by DevExpress reports). The Dockerfile fails the build if "Times New Roman"
does not appear in `fc-list` after font setup.

Two ways to satisfy this:

1) Network build (default)
   - The image installs Ubuntu package `ttf-mscorefonts-installer` with the
     Core Fonts EULA pre-accepted. This downloads fonts during the image build.

2) Offline / restricted network
   - Before `docker build`, copy licensed TrueType/OpenType files for Times New
     Roman into this folder (same directory as this README). Typical Core Fonts
     names include times.ttf, timesbd.ttf, timesi.ttf, timesbi.ttf (names vary
     by source). The build copies `*.ttf`, `*.TTF`, `*.ttc`, `*.TTC` from here
     into the image when at least one such file is present; otherwise it uses
     the apt package above.

Do not commit font binaries unless your organization is allowed to redistribute
them. Binary patterns are listed in the repo root `.gitignore`.
