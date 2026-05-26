# Offline Docker Engine install (WSL Ubuntu on Windows Server)

Use when `Install-WslDockerEngine.ps1` hangs at `apt-get` / `curl` (no or slow internet from WSL).

## Overview

1. On a machine **with internet**, download Ubuntu `.deb` packages matching the **same Ubuntu version** as WSL on the server.
2. Copy the folder to the server (USB, RDP copy, `C:\WslDocker-Setup\debs`).
3. Inside WSL, `dpkg -i` the packages, then `docker run hello-world` (image can be loaded offline too).

## 1) Ubuntu version on the server

On the server (Administrator PowerShell):

```powershell
wsl -d Ubuntu -u root -- bash -c ". /etc/os-release; echo VERSION_ID=\$VERSION_ID CODENAME=\$VERSION_CODENAME"
```

Example: `VERSION_ID=22.04` `CODENAME=jammy` → use **jammy** below.

## 2) Download packages (internet-connected PC)

Use **WSL Ubuntu with the same CODENAME** (e.g. Ubuntu 22.04 on your dev PC), or any Linux VM.

```bash
sudo mkdir -p ~/docker-offline && cd ~/docker-offline

CODENAME=jammy   # <-- match server (jammy, noble, focal, ...)

sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg apt-rdepends

# Docker repo (online, one-time on prep machine)
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu ${CODENAME} stable" | sudo tee /etc/apt/sources.list.d/docker.list
sudo apt-get update

PKGS="docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin"
sudo apt-get install -y --download-only $PKGS

# Copy downloaded debs + dependencies
mkdir -p ./debs
cp /var/cache/apt/archives/*.deb ./debs/
# Also pull recursive dependencies (if --download-only missed some):
cd ./debs
for p in $PKGS; do
  apt-rdepends $p 2>/dev/null | grep -v "^ " | grep -v "^$" | xargs -r apt-get download 2>/dev/null || true
done
cd ..
```

Zip and transfer `debs` to the server, e.g. `C:\WslDocker-Setup\debs`.

**Direct browser download (no prep Linux):**  
https://download.docker.com/linux/ubuntu/dists/  
→ `{CODENAME}/pool/stable/amd64/` → download `containerd.io`, `docker-ce`, `docker-ce-cli`, `docker-buildx-plugin`, `docker-compose-plugin` (latest amd64). You may need extra dependency `.deb` files from Ubuntu archives if `dpkg` reports missing packages.

## 3) Install on server (WSL)

Administrator PowerShell:

```powershell
wsl -d Ubuntu -u root
```

Inside Ubuntu:

```bash
cd /mnt/c/WslDocker-Setup/debs
dpkg -i *.deb
apt-get -f install -y    # only if online for missing deps; skip if fully offline and dpkg OK
systemctl enable docker
systemctl start docker
docker --version
docker compose version
```

## 4) hello-world offline (optional)

On prep machine with internet:

```bash
docker pull hello-world
docker save hello-world -o hello-world.tar
```

Copy `hello-world.tar` to `C:\WslDocker-Setup\`, then on server WSL:

```bash
docker load -i /mnt/c/WslDocker-Setup/hello-world.tar
docker run --rm hello-world
```

## 5) Verify from Windows

```powershell
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

## If `dpkg` reports missing dependencies

Note the package names, download matching `.deb` from:

- https://download.docker.com/linux/ubuntu/dists/{CODENAME}/pool/stable/amd64/
- http://archive.ubuntu.com/ubuntu/pool/ (main/universe for Ubuntu deps)

Install dependency debs first, then run `dpkg -i` again.
