# syntax=docker/dockerfile:1
ARG BASE_IMAGE=mcr.microsoft.com/mssql/server:2025-latest

FROM ${BASE_IMAGE}

# Minimal wrapper image so the project can pin/customize SQL Server later
# (e.g., add tools, custom entrypoint, init scripts). For now it's just a tagged base.
LABEL org.opencontainers.image.title="visa2026-sqlserver"
LABEL org.opencontainers.image.description="Visa2026 local SQL Server image (wrapper around official Microsoft image)."

