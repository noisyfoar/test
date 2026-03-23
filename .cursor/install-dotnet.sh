#!/usr/bin/env bash
set -euo pipefail

if command -v dotnet >/dev/null 2>&1; then
  echo ".NET SDK already installed, skipping."
  exit 0
fi

echo "Installing .NET SDK (LTS channel)..."

mkdir -p /tmp/dotnet-install
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install/install.sh
bash /tmp/dotnet-install/install.sh --channel LTS --install-dir "$HOME/.dotnet"

if ! grep -q 'HOME/.dotnet' "$HOME/.profile" 2>/dev/null; then
  {
    echo 'export DOTNET_ROOT="$HOME/.dotnet"'
    echo 'export PATH="$HOME/.dotnet:$PATH"'
  } >> "$HOME/.profile"
fi

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

echo ".NET SDK installed:"
dotnet --version
