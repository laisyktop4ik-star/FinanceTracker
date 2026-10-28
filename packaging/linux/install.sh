#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
APP_SOURCE="$ROOT/app"
INSTALL_DIR="$HOME/.local/lib/FinanceTracker"
DESKTOP_DIR="$HOME/.local/share/applications"
ICON_DIR="$HOME/.local/share/icons/hicolor/scalable/apps"

if [[ ! -x "$APP_SOURCE/FinanceTracker" ]]; then
  echo "Ошибка: файл app/FinanceTracker не найден. Распакуйте архив полностью." >&2
  exit 1
fi

mkdir -p "$INSTALL_DIR" "$DESKTOP_DIR" "$ICON_DIR"
rm -rf "$INSTALL_DIR"/*
cp -a "$APP_SOURCE"/. "$INSTALL_DIR"/
cp "$ROOT/financetracker.svg" "$ICON_DIR/financetracker.svg"

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
if command -v desktop-file-validate >/dev/null 2>&1; then
  desktop-file-validate "$DESKTOP_DIR/financetracker.desktop"
fi
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
fi

echo
echo "FinanceTracker 1.1.0 установлен."
echo "Запустите его из меню приложений или командой:"
echo "  $INSTALL_DIR/FinanceTracker"
echo
echo "Данные будут храниться отдельно: ~/.local/share/FinanceTracker/"
