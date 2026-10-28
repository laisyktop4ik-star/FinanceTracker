#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
VERSION="1.1.0"
DIST="$ROOT/dist"
STAGE="$(mktemp -d /tmp/financetracker-release-XXXXXX)"
trap 'rm -rf "$STAGE"' EXIT

export PATH="$HOME/.dotnet:$PATH"
mkdir -p "$DIST"
rm -f "$DIST"/FinanceTracker-* "$DIST"/SHA256SUMS.txt

echo "[1/4] Tests"
dotnet test "$ROOT/FinanceTracker.sln" --no-restore

echo "[2/4] Linux x64 self-contained"
dotnet publish "$ROOT/src/FinanceTracker.App" \
  -c Release -r linux-x64 --self-contained true \
  -o "$STAGE/linux-bundle/app"
cp "$ROOT/packaging/linux/install.sh" "$STAGE/linux-bundle/install.sh"
cp "$ROOT/packaging/linux/uninstall.sh" "$STAGE/linux-bundle/uninstall.sh"
cp "$ROOT/packaging/linux/README-INSTALL.txt" "$STAGE/linux-bundle/README-INSTALL.txt"
cp "$ROOT/src/FinanceTracker.App/Assets/financetracker.svg" "$STAGE/linux-bundle/financetracker.svg"
cp "$ROOT/LICENSE" "$STAGE/linux-bundle/LICENSE"
chmod +x "$STAGE/linux-bundle/install.sh" "$STAGE/linux-bundle/uninstall.sh" "$STAGE/linux-bundle/app/FinanceTracker"
tar -C "$STAGE/linux-bundle" -czf "$DIST/FinanceTracker-${VERSION}-linux-x64.tar.gz" .

echo "[3/4] Windows x64 self-contained"
dotnet publish "$ROOT/src/FinanceTracker.App" \
  -c Release -r win-x64 --self-contained true \
  -o "$STAGE/windows-bundle"
cp "$ROOT/packaging/windows/README-WINDOWS.txt" "$STAGE/windows-bundle/README-WINDOWS.txt"
cp "$ROOT/LICENSE" "$STAGE/windows-bundle/LICENSE"
python3 - "$STAGE/windows-bundle" "$DIST/FinanceTracker-${VERSION}-windows-x64.zip" <<'PY'
import pathlib, sys, zipfile
source = pathlib.Path(sys.argv[1])
destination = pathlib.Path(sys.argv[2])
with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
    for path in sorted(source.rglob('*')):
        if path.is_file():
            archive.write(path, path.relative_to(source))
PY

echo "[4/4] Checksums"
(
  cd "$DIST"
  sha256sum "FinanceTracker-${VERSION}-linux-x64.tar.gz" \
            "FinanceTracker-${VERSION}-windows-x64.zip" > SHA256SUMS.txt
)

printf '\nRelease artifacts:\n'
ls -lh "$DIST/FinanceTracker-${VERSION}-linux-x64.tar.gz" \
       "$DIST/FinanceTracker-${VERSION}-windows-x64.zip" \
       "$DIST/SHA256SUMS.txt"
