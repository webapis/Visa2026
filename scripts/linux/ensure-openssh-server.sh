#!/bin/bash
# Install and enable OpenSSH server on Ubuntu (on-prem LAN admin).
set -euo pipefail

if [ "$(id -u)" -ne 0 ]; then
  echo "Run with sudo: sudo bash ensure-openssh-server.sh" >&2
  exit 1
fi

apt-get update
apt-get install -y openssh-server

systemctl enable --now ssh
systemctl is-active ssh

if command -v ufw >/dev/null 2>&1 && ufw status 2>/dev/null | grep -q "Status: active"; then
  ufw allow 22/tcp
  echo "ufw: allowed 22/tcp"
fi

echo "Listening on port 22:"
ss -tlnp | grep ':22 ' || true
