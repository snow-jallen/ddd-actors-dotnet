#!/usr/bin/env bash
# Render tools/cover/cover.html to book/images/cover.png (1600x2400) and cover.jpg.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
python3 "$root/tools/cover/make-cover.py" >/dev/null
browser="${BOOK_PDF_BROWSER:-$(command -v google-chrome-stable || command -v google-chrome || command -v chromium || command -v chromium-browser)}"
profile="$(mktemp -d)"; trap 'rm -rf "$profile"' EXIT
mkdir -p "$root/book/images"
"$browser" --headless=new --disable-gpu --hide-scrollbars --user-data-dir="$profile" ${BOOK_PDF_EXTRA_FLAGS:-} \
  --window-size=1600,2400 --virtual-time-budget=15000 \
  --screenshot="$root/book/images/cover.png" "file://$root/tools/cover/cover.html" 2>/dev/null
python3 -c "from PIL import Image; Image.open('$root/book/images/cover.png').convert('RGB').save('$root/book/images/cover.jpg', quality=88, optimize=True)"
echo "$root/book/images/cover.png"
