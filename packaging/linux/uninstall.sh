#!/usr/bin/env bash
set -euo pipefail

INSTALL_DIR="$HOME/.local/lib/FinanceTracker"
DESKTOP_FILE="$HOME/.local/share/applications/financetracker.desktop"
ICON_FILE="$HOME/.local/share/icons/hicolor/scalable/apps/financetracker.svg"
DATA_DIR="$HOME/.local/share/FinanceTracker"

rm -rf "$INSTALL_DIR"
rm -f "$DESKTOP_FILE" "$ICON_FILE"

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true
fi

if [[ "${1:-}" == "--remove-data" ]]; then
  rm -rf "$DATA_DIR"
  echo "FinanceTracker и пользовательские данные удалены."
else
  echo "FinanceTracker удалён. Личные данные сохранены в: $DATA_DIR"
  echo "Чтобы удалить и данные: ./uninstall.sh --remove-data"
fi
