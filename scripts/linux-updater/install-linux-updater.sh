#!/bin/bash
# One-time installer for KitsuneCommand's Linux auto-update support.
# Run as the user who owns the 7D2D server install. Will prompt for sudo.
#
# What this does:
#   1. Copies kitsune-pre-start.sh into the server directory
#   2. Modifies the systemd service unit to add:
#        - ExecStartPre=/path/to/kitsune-pre-start.sh
#        - Restart=always (so in-game Shutdown button = effective restart)
#        - RestartSec=5
#   3. Adds a sudoers.d rule so this user can restart the service without password
#
# After this, KitsuneCommand's web UI can control auto-update via Settings UI.

set -e

SERVER_DIR="${1:-$HOME/7d2d-server}"
SERVICE_NAME="${2:-7daystodie.service}"
SERVICE_UNIT="/etc/systemd/system/$SERVICE_NAME"
CURRENT_USER="$(whoami)"

if [ ! -d "$SERVER_DIR" ]; then
    echo "ERROR: Server directory not found: $SERVER_DIR"
    echo "Usage: $0 [server-dir] [service-name]"
    exit 1
fi

if [ ! -f "$SERVICE_UNIT" ]; then
    echo "ERROR: Service unit not found: $SERVICE_UNIT"
    exit 1
fi

SCRIPT_SRC="$(dirname "$(readlink -f "$0")")/kitsune-pre-start.sh"
if [ ! -f "$SCRIPT_SRC" ]; then
    echo "ERROR: kitsune-pre-start.sh not found next to this installer."
    exit 1
fi

echo "=== KitsuneCommand Linux Updater Installer ==="
echo "Server dir:   $SERVER_DIR"
echo "Service:      $SERVICE_NAME"
echo "User:         $CURRENT_USER"
echo ""
read -rp "Proceed? [y/N] " confirm
[ "$confirm" = "y" ] || [ "$confirm" = "Y" ] || { echo "Aborted."; exit 1; }

# --- 1. Copy pre-start script ---
echo ""
echo "[1/3] Installing kitsune-pre-start.sh..."
cp "$SCRIPT_SRC" "$SERVER_DIR/kitsune-pre-start.sh"
chmod +x "$SERVER_DIR/kitsune-pre-start.sh"
echo "  Copied to $SERVER_DIR/kitsune-pre-start.sh"

# --- 2. Modify systemd service ---
echo ""
echo "[2/3] Modifying systemd service unit..."
echo "  (will prompt for sudo)"

# Build the override content
OVERRIDE_DIR="/etc/systemd/system/${SERVICE_NAME}.d"
OVERRIDE_FILE="$OVERRIDE_DIR/kitsune-updater.conf"

sudo mkdir -p "$OVERRIDE_DIR"
sudo tee "$OVERRIDE_FILE" > /dev/null << EOF
# Added by KitsuneCommand install-linux-updater.sh
[Service]
ExecStartPre=$SERVER_DIR/kitsune-pre-start.sh
Restart=always
RestartSec=5
EOF

echo "  Wrote $OVERRIDE_FILE"
sudo systemctl daemon-reload
echo "  systemd daemon-reloaded"

# --- 3. Sudoers entry for passwordless restart ---
echo ""
echo "[3/3] Adding sudoers entry for passwordless service restart..."
SUDOERS_FILE="/etc/sudoers.d/kitsune-7d2d-$CURRENT_USER"
sudo tee "$SUDOERS_FILE" > /dev/null << EOF
# Added by KitsuneCommand install-linux-updater.sh
$CURRENT_USER ALL=(root) NOPASSWD: /bin/systemctl restart $SERVICE_NAME, /bin/systemctl stop $SERVICE_NAME, /bin/systemctl start $SERVICE_NAME
EOF
sudo chmod 0440 "$SUDOERS_FILE"
# Validate
if sudo visudo -cf "$SUDOERS_FILE"; then
    echo "  sudoers entry validated at $SUDOERS_FILE"
else
    echo "  ERROR: sudoers entry failed validation. Removing."
    sudo rm -f "$SUDOERS_FILE"
    exit 1
fi

# --- 4. Seed serverconfig.xml.bak if missing ---
if [ ! -f "$SERVER_DIR/serverconfig.xml.bak" ] && [ -f "$SERVER_DIR/serverconfig.xml" ]; then
    echo ""
    echo "[bonus] Seeding serverconfig.xml.bak from current serverconfig.xml..."
    cp "$SERVER_DIR/serverconfig.xml" "$SERVER_DIR/serverconfig.xml.bak"
    echo "  Your current config is now the sticky baseline."
fi

echo ""
echo "=== Done ==="
echo ""
echo "Next steps:"
echo "  - In KC web UI, open Server Update settings, toggle AutoUpdate if you want it on."
echo "  - To update and restart: click 'Update Now' (or 'sudo systemctl restart $SERVICE_NAME')."
echo "  - Edit serverconfig.xml.bak via the web UI - that's where durable config edits live now."
echo "  - First steamcmd run may need interactive login: sudo -u $CURRENT_USER steamcmd +login <user>"
echo ""
