#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
PUBLISH="$ROOT/publish/linux-x64"
INSTALL_DIR="$HOME/.local/lib/FinanceTracker"
DESKTOP_DIR="$HOME/.local/share/applications"
ICON_DIR="$HOME/.local/share/icons/hicolor/scalable/apps"

export PATH="$HOME/.dotnet:$PATH"
dotnet publish "$ROOT/src/FinanceTracker.App" -c Release -r linux-x64 --self-contained true -o "$PUBLISH"
mkdir -p "$INSTALL_DIR" "$DESKTOP_DIR" "$ICON_DIR"
rm -rf "$INSTALL_DIR"/*
cp -a "$PUBLISH"/. "$INSTALL_DIR"/
cp "$ROOT/src/FinanceTracker.App/Assets/financetracker.svg" "$ICON_DIR/financetracker.svg"
cat > "$DESKTOP_DIR/financetracker.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=FinanceTracker
Comment=Учёт личных доходов и расходов
Exec=$INSTALL_DIR/FinanceTracker
Icon=$ICON_DIR/financetracker.svg
Terminal=false
Categories=Office;Finance;
StartupNotify=true
StartupWMClass=FinanceTracker
EOF
chmod +x "$INSTALL_DIR/FinanceTracker" "$DESKTOP_DIR/financetracker.desktop"
update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
echo "FinanceTracker установлен."
