#!/usr/bin/env bash
set -euo pipefail

if command -v dotnet >/dev/null 2>&1; then
  if dotnet --list-sdks | awk '{print $1}' | grep -q '^8\.'; then
    echo ".NET SDK 8 already installed, skipping."
    exit 0
  fi
fi

echo "Installing .NET SDK 8..."

sudo apt-get update
sudo apt-get install -y apt-transport-https ca-certificates curl gnupg

if [[ ! -f /etc/apt/keyrings/microsoft.gpg ]]; then
  curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
    | gpg --dearmor \
    | sudo tee /etc/apt/keyrings/microsoft.gpg >/dev/null
fi

source /etc/os-release
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/microsoft.gpg] https://packages.microsoft.com/ubuntu/${VERSION_ID}/prod ${VERSION_CODENAME} main" \
  | sudo tee /etc/apt/sources.list.d/microsoft-prod.list >/dev/null

sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

echo ".NET SDK 8 installed:"
dotnet --version
