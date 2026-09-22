#!/bin/sh
set -e

REPO="Dycellll/hyprstyle"
BINARY_NAME="hyprstyle"
INSTALL_DIR="${HOME}/.local/bin"
CONFIG_DIR="${HOME}/.config/hyprstyle"

uninstall() {
    BIN_PATH="${INSTALL_DIR}/${BINARY_NAME}"

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

if [ "${1:-}" = "--uninstall" ]; then
    uninstall
    exit 0
fi

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

API_URL="https://api.github.com/repos/${REPO}/releases/latest"

echo "Fetching latest release info..."

DOWNLOAD_URL=$(
    curl -fsSL "$API_URL" |
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

mkdir -p "$INSTALL_DIR"

TMP_FILE=$(mktemp)

cleanup() {
    rm -f "$TMP_FILE"
}

trap cleanup EXIT HUP INT TERM

echo "Downloading $DOWNLOAD_URL..."

curl -fsSL "$DOWNLOAD_URL" -o "$TMP_FILE"

chmod +x "$TMP_FILE"
mv "$TMP_FILE" "${INSTALL_DIR}/${BINARY_NAME}"

echo "Installed to ${INSTALL_DIR}/${BINARY_NAME}"

case ":${PATH}:" in
    *":${INSTALL_DIR}:"*)
        ;;
    *)
        echo "Note: ${INSTALL_DIR} is not on your PATH."
        echo "Add it to your shell profile to run '${BINARY_NAME}' directly."
        ;;
esac

echo "Done. Run '${BINARY_NAME} list' to get started."
