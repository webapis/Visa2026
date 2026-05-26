#!/bin/bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive
LOG=/var/log/visa-docker-install.log
echo "==> visa-docker-install $(date -Iseconds)" | tee "$LOG"

if command -v docker >/dev/null 2>&1; then
  echo "==> Docker already installed: $(docker --version)"
  systemctl enable docker
  systemctl start docker
  docker compose version
  docker run --rm hello-world
  exit 0
fi

echo "==> apt-get update (1/4) — may take several minutes on first run" | tee -a "$LOG"
apt-get update 2>&1 | tee -a "$LOG"
echo "==> apt-get install ca-certificates curl" | tee -a "$LOG"
apt-get install -y ca-certificates curl 2>&1 | tee -a "$LOG"

echo "==> Docker apt repository setup" | tee -a "$LOG"
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc

. /etc/os-release
tee /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: ${UBUNTU_CODENAME:-$VERSION_CODENAME}
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF
echo "==> wrote /etc/apt/sources.list.d/docker.sources" | tee -a "$LOG"

echo "==> apt-get update (2/4)" | tee -a "$LOG"
apt-get update 2>&1 | tee -a "$LOG"
echo "==> apt-get install docker packages (3/4) — largest step" | tee -a "$LOG"
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin 2>&1 | tee -a "$LOG"

echo "==> systemctl enable/start docker" | tee -a "$LOG"
systemctl enable docker 2>&1 | tee -a "$LOG"
systemctl start docker 2>&1 | tee -a "$LOG"

docker --version 2>&1 | tee -a "$LOG"
docker compose version 2>&1 | tee -a "$LOG"
echo "==> docker run hello-world (4/4)" | tee -a "$LOG"
docker run --rm hello-world 2>&1 | tee -a "$LOG"
echo "==> Docker install finished OK" | tee -a "$LOG"
