#!/bin/sh
set -e

REPO="Dycellll/hyprstyle"
BINARY_NAME="hyprstyle"
INSTALL_DIR="${HOME}/.local/bin"
CONFIG_DIR="${HOME}/.config/hyprstyle"
BIN_PATH="${INSTALL_DIR}/${BINARY_NAME}"

uninstall() {
    if [ -f "$BIN_PATH" ]; then
        rm -f "$BIN_PATH"
        echo "Removed $BIN_PATH"
    else
        echo "hyprstyle is not installed at $BIN_PATH."
    fi

    if [ -d "$CONFIG_DIR" ]; then
        printf "Remove styles and config at %s? [y/N] " "$CONFIG_DIR" > /dev/tty
        read -r answer < /dev/tty

        case "$answer" in
            y|Y|yes|YES|Yes)
                rm -rf "$CONFIG_DIR"
                echo "Removed $CONFIG_DIR"
                ;;
            *)
                echo "Keeping $CONFIG_DIR"
                ;;
        esac
    fi

    echo "Uninstall complete."
}

MODE="install"

case "${1:-}" in
    --uninstall)
        uninstall
        exit 0
        ;;
    --update)
        MODE="update"
        ;;
esac

if [ "$MODE" = "update" ]; then
    if [ ! -f "$BIN_PATH" ]; then
        echo "hyprstyle isn't installed at $BIN_PATH."
        echo "Run this script without --update to install it."
        exit 1
    fi
else
    if [ -z "${HYPRLAND_INSTANCE_SIGNATURE:-}" ]; then
        echo "hyprstyle is a theme switcher built specifically for Hyprland."
        echo "It doesn't look like you're currently running Hyprland."
        exit 1
    fi

    printf "Install hyprstyle to %s? [y/N] " "$INSTALL_DIR" > /dev/tty
    read -r answer < /dev/tty

    case "$answer" in
        y|Y|yes|YES|Yes)
            ;;
        *)
            echo "Aborted."
            exit 0
            ;;
    esac
fi

API_URL="https://api.github.com/repos/${REPO}/releases/latest"

echo "Fetching latest release info..."

RELEASE=$(curl -fsSL "$API_URL")

LATEST_TAG=$(
    printf '%s\n' "$RELEASE" |
    grep '"tag_name":' |
    sed 's/.*"tag_name": *"\([^"]*\)".*/\1/' |
    head -n 1
)

DOWNLOAD_URL=$(
    printf '%s\n' "$RELEASE" |
    grep '"browser_download_url":' |
    grep '"https://github.com/.*/releases/download/.*/hyprstyle"' |
    sed 's/.*"browser_download_url": *"\([^"]*\)".*/\1/' |
    head -n 1
)

if [ -z "$DOWNLOAD_URL" ]; then
    echo "Couldn't find the hyprstyle binary in the latest release."
    echo "Check https://github.com/${REPO}/releases for available builds."
    exit 1
fi

if [ "$MODE" = "update" ]; then
    INSTALLED=$("$BIN_PATH" version 2>/dev/null || true)

    if [ -n "$LATEST_TAG" ] && [ "${INSTALLED#v}" = "${LATEST_TAG#v}" ]; then
        echo "Already up to date (${INSTALLED})."
        exit 0
    fi
fi

mkdir -p "$INSTALL_DIR"

TMP_FILE=$(mktemp "${INSTALL_DIR}/.${BINARY_NAME}.XXXXXX")

cleanup() {
    rm -f "$TMP_FILE"
}

trap cleanup EXIT HUP INT TERM

echo "Downloading $DOWNLOAD_URL..."

curl -fsSL "$DOWNLOAD_URL" -o "$TMP_FILE"

chmod +x "$TMP_FILE"
mv "$TMP_FILE" "$BIN_PATH"

if [ "$MODE" = "update" ]; then
    echo "Updated to ${LATEST_TAG:-the latest release}."
    exit 0
fi

echo "Installed to ${BIN_PATH}"

case ":${PATH}:" in
    *":${INSTALL_DIR}:"*)
        ;;
    *)
        echo "Note: ${INSTALL_DIR} is not on your PATH."
        echo "Add it to your shell profile to run '${BINARY_NAME}' directly."
        ;;
esac

echo "Done. Run '${BINARY_NAME}' to get started."
