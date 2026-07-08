#!/bin/sh
# Runtime config injection: write env-provided URLs into a JS file the app reads.
# This lets one built image work against any backend/AI URL without rebuilding.
set -e

CONFIG_FILE=/usr/share/nginx/html/config.js

cat > "$CONFIG_FILE" <<EOF
window.__CONFIG__ = {
  API_BASE_URL: "${API_BASE_URL:-}",
  AI_BASE_URL: "${AI_BASE_URL:-}"
};
EOF

echo "Injected runtime config: API_BASE_URL=${API_BASE_URL:-} AI_BASE_URL=${AI_BASE_URL:-}"
