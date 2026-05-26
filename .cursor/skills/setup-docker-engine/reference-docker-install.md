# Docker Engine install — Ubuntu (on-prem Linux)

**Canonical:** [Install Docker Engine on Ubuntu](https://docs.docker.com/engine/install/ubuntu/)

**Skill:** [SKILL.md](./SKILL.md) · **Commands:** [reference.md](./reference.md)

## Supported Ubuntu (Docker official)

- Ubuntu 22.04 LTS (Jammy)
- Ubuntu 24.04 LTS (Noble)

Visa2026 on-prem targets **22.04** or **24.04** on company LAN VMs.

## Install method

Use Docker’s **`apt` repository** (recommended) — full commands in [SKILL.md Step 1](./SKILL.md#step-1--install-docker-engine-ubuntu).

After install:

- `sudo systemctl enable --now docker`
- Optional: [Linux postinstall](https://docs.docker.com/engine/install/linux-postinstall/) for non-root `docker` group

## Verify

```bash
docker --version
docker compose version
docker run --rm hello-world
```

## Not used on Linux prod server

| Item | Why |
|------|-----|
| **Docker Desktop** | Dev workstations; [not for Linux server prod](https://docs.docker.com/desktop/setup/install/windows-install/) |
| **WSL / scripts/legacy/on-prem-windows** | Legacy Windows Server path only |
| **Windows `dockerd` binaries** | Windows containers only — [binaries doc](https://docs.docker.com/engine/install/binaries/) |

## Legacy: WSL install on Windows Server

If maintaining an old host, see archived `scripts/legacy/on-prem-windows/install-docker-engine.sh` and [ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md). **New deploys:** Ubuntu + this skill.
