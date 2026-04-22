#!/bin/bash
# Kitsune pre-start hook. Runs on every systemd ExecStartPre of the 7D2D service.
# Mirrors the Windows launch.bat load order:
#   1. steamcmd update (gated by KC's kitsune-update.conf AutoUpdate flag)
#   2. restore serverconfig.xml from serverconfig.xml.bak
#   3. trim log files, keep the most recent N
#
# Config: /home/ada/7d2d-server/kitsune-update.conf (written by ServerUpdateFeature)
# Failures in steps 1 or 3 don't block the server start - the config restore step does,
# because a missing config file means the server boots with TFP defaults, which is
# nobody's idea of a good time.

set -u  # catch unset vars, but NOT -e (we want to degrade gracefully on update failure)

SERVER_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SERVER_DIR"

CONF="$SERVER_DIR/kitsune-update.conf"
STEAMCMD="${STEAMCMD:-/usr/games/steamcmd}"

log() { echo "[kitsune-pre-start] $*"; }

# --- 1. Auto-update ---
if [ -f "$CONF" ]; then
    # shellcheck disable=SC1090
    source "$CONF"
    if [ "${AutoUpdate:-false}" = "true" ]; then
        if [ -x "$STEAMCMD" ] || command -v steamcmd >/dev/null 2>&1; then
            APP_ID="${SteamAppId:-251570}"
            BETA_ARGS=""
            if [ -n "${Branch:-}" ] && [ "${Branch}" != "public" ]; then
                BETA_ARGS="-beta ${Branch}"
                if [ -n "${BranchPassword:-}" ]; then
                    BETA_ARGS="$BETA_ARGS -betapassword ${BranchPassword}"
                fi
            fi
            LOGIN_USER="${SteamUsername:-anonymous}"
            [ -z "$LOGIN_USER" ] && LOGIN_USER="anonymous"
            log "Running steamcmd update for app $APP_ID as '$LOGIN_USER' ${BETA_ARGS:-(public branch)}..."
            # shellcheck disable=SC2086
            "$STEAMCMD" +force_install_dir "$SERVER_DIR" +login "$LOGIN_USER" +app_update "$APP_ID" $BETA_ARGS validate +quit \
                || log "WARN: steamcmd returned non-zero. Continuing with existing install. If using a non-anonymous account, make sure you've cached credentials first: steamcmd +login $LOGIN_USER +quit"
        else
            log "WARN: steamcmd not found at $STEAMCMD - skipping update."
        fi
    else
        log "AutoUpdate is disabled. Skipping steamcmd."
    fi
else
    log "No $CONF - KC hasn't initialized ServerUpdateFeature yet. Skipping update."
fi

# --- 2. Restore sticky serverconfig ---
if [ -f "$SERVER_DIR/serverconfig.xml.bak" ]; then
    cp -f "$SERVER_DIR/serverconfig.xml.bak" "$SERVER_DIR/serverconfig.xml"
    log "Restored serverconfig.xml from serverconfig.xml.bak"
else
    log "No serverconfig.xml.bak found. Using whatever serverconfig.xml exists (possibly TFP default if steamcmd just ran)."
fi

# --- 3. Log rotation ---
RETENTION="${LogRetention:-20}"
# Keep the RETENTION newest output_log__*.txt files, delete the rest.
# shellcheck disable=SC2012
ls -t "$SERVER_DIR"/output_log__*.txt 2>/dev/null | tail -n +"$((RETENTION + 1))" | xargs -r rm -f
log "Trimmed logs, keeping newest $RETENTION."

log "Pre-start complete."
exit 0
