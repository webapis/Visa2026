# Linux VM — hardware and OS (system administrator)

Visa2026 team installs application software after handoff.  
Developers: [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md)

---

## Operating system

| Item | Requirement |
|------|-------------|
| **OS** | **Ubuntu 22.04 LTS** or **Ubuntu 24.04 LTS** (64-bit) |
| **Edition** | Server (default Ubuntu Server install is fine) |
| **IP** | Static LAN address (document and hand over to deploy team) |
| **Time** | NTP / time sync enabled |
| **Access** | Local sudo user for deploy team (credentials via secure channel) |

---

## Hardware — production + staging on one VM

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| **RAM** | 16 GB | 24–32 GB |
| **CPU** | 4 vCPU | 4–8 vCPU |
| **Disk** | 150 GB free | 250 GB+ free |

## Hardware — production only (single environment)

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| **RAM** | 8 GB | 16 GB |
| **CPU** | 2 vCPU | 4 vCPU |
| **Disk** | 100 GB free | 200 GB+ free |

Confirm with the Visa2026 team whether one VM runs **both** production and staging or **production only**.

---

## Handoff

Provide: VM **IP**, **OS version**, **RAM/CPU/disk** as provisioned, **sudo** access.
