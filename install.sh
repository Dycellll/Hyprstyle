#!/bin/sh
set -e

REPO="Dycellll/hyprstyle"
BINARY_NAME="hyprstyle"
INSTALL_DIR="$HOME/.local/bin"
CONFIG_DIR="$HOME/.config/hyprstyle"

uninstall() {
    BIN_PATH="$INSTALL_DIR/$BINARY_NAME"

    if [ ! -f "$BIN_PATH" ]; then
        echo "hyprstyle doesn't appear to be installed at $BIN_PATH."
    else
        rm -f "$BIN_PATH"
        echo "Removed $BIN_PATH"
    fi

    if [ -d "$CONFIG_DIR" ]; then
        printf "Keep your styles and config at %s? [Y/n] " "$CONFIG_DIR" </dev/tty
        read -r answer </dev/tty

        case "$answer" in
            [Nn]*)
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
    echo "It doesn't look like you're currently running Hyprland, so this probably won't be useful to you."
    exit 1
fi

printf "Install hyprstyle to %s? [y/N] " "$INSTALL_DIR" </dev/tty
read -r answer </dev/tty

case "$answer" in
    [Yy]*)
        ;;
    *)
        echo "Aborted."
        exit 0
        ;;
esac

ARCH=$(uname -m)

case "$ARCH" in
    x86_64)
        ASSET="hyprstyle-linux-x64"
        ;;
    aarch64|arm64)
        ASSET="hyprstyle-linux-arm64"
        ;;
    *)
        echo "Unsupported architecture: $ARCH"
        exit 1
        ;;
esac

API_URL="https://api.github.com/repos/$REPO/releases/latest"

echo "Fetching latest release info..."

DOWNLOAD_URL=$(
    curl -fsSL "$API_URL" |
        grep -o "\"browser_download_url\": *\"[^\"]*$ASSET[^\"]*\"" |
        sed -E 's/.*"(https[^"]+)"/\1/'
)

if [ -z "$DOWNLOAD_URL" ]; then
    echo "Couldn't find a release asset matching '$ASSET' at $REPO."
    echo "Check https://github.com/$REPO/releases for available builds."
    exit 1
fi

mkdir -p "$INSTALL_DIR"

TMP_FILE=$(mktemp)
trap 'rm -f "$TMP_FILE"' EXIT HUP INT TERM

echo "Downloading $DOWNLOAD_URL ..."
curl -fsSL "$DOWNLOAD_URL" -o "$TMP_FILE"

chmod +x "$TMP_FILE"
mv "$TMP_FILE" "$INSTALL_DIR/$BINARY_NAME"

echo "Installed to $INSTALL_DIR/$BINARY_NAME"

case ":$PATH:" in
    *":$INSTALL_DIR:"*)
        ;;
    *)
        echo "Note: $INSTALL_DIR isn't on your PATH. Add it to your shell profile to run '$BINARY_NAME' directly."
        ;;
esac

echo "Done. Run '$BINARY_NAME list' to get started."
