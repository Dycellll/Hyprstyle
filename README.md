# Hyprstyle

A CLI+GUI tool to save, load, and switch between complete Hyprland styles.

## Quick install

Run this command in your terminal to install the latest release:

```sh
curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh
```

**Requirements:** Hyprland. See the [full installation guide](#install-guide) and [requirements](#requirements) below for details.

# What is Hyprstyle?
**Hyprstyle** is a CLI+GUI (via quickshell) save/load system for **Hyprland**. Its main purpose is to let you easily and intuitively save and load styles, or as you may call them, dotfiles, or maybe, rices, whichever tickles your fancy.
It comes with a .json config, for you to define any component you want your styles to save and load, and optionally define custom save/load logic through normal shell syntax.

# Requirements
- **Hyprland** (`hyprctl` must be on your PATH)
- **grim** *(optional)* -- used to capture style previews (screenshots). Without it, styles simply have no automatic preview.
- **quickshell** *(optional)* -- required for `hyprstyle gui`.
- **libnotify (notify-send)** *(optional)* -- used to show an error notification when `grim` or `quickshell` is missing. Not necessary if those 2 are present.

# How do I use Hyprstyle?
**Hyprstyle** uses only a few main commands, which are listed here:

- **`hyprstyle list`** -- Lists all saved styles, grouped by folder, along with their aliases. The currently loaded style is marked with a ★.

- **`hyprstyle current`** -- Gets the current style name, or "" if there's none. Most useful for other programs to read the current style name. (`hyprstyle current name` still works too.)

- **`hyprstyle save <name>`** -- Saves a new style or replaces an existing one. Replacing keeps the style's aliases. If `grim` is installed, a preview screenshot is saved with it. The saved style becomes the current style. Use `folder/name` to save a style inside a folder.

- **`hyprstyle set <name|alias>`** -- Loads a saved style.

- **`hyprstyle reload`** -- Loads the current style again.

- **`hyprstyle rename <name> <new_name>`** -- Renames a saved style. The new name can include a folder (`folder/new_name`) to move the style there.

- **`hyprstyle alias <name> <alias>`** -- Gives a style an alias, which can be used instead of its name in `set`. Two styles can't share the same alias, and an alias can't be the same as an existing style name. Aliases can't be empty, start with a dot, or contain spaces or slashes.

- **`hyprstyle deletealias <name> <alias>`** -- Removes an alias from a style.

- **`hyprstyle setpreview [name] [image]`** -- Sets a style's preview image.
  - With an image path (or `file://` URL), that image is used.
  - With no image, a screenshot is taken with `grim` instead.
  - With no name, the current style is used.
  - If you pass a single argument, it's treated as a style name if one matches, otherwise as an image path.

- **`hyprstyle deletepreview [name]`** -- Removes a style's preview. Defaults to the current style.

- **`hyprstyle delete <name>`** -- Deletes a saved style. Passing a folder name deletes the whole folder and everything in it.

- **`hyprstyle gui`** -- Opens a GUI showing all your saved styles with previews. Requires `quickshell`. See [The GUI](#the-gui) below.

- **`hyprstyle json`** -- Prints all style data (styles, aliases, previews, current style, version, update status) as indented, human-readable JSON. Made for building your own quickshell GUIs, and easy to pipe into tools like `jq`. See [JSON output](#json-output) below.

- **`hyprstyle update`** -- Updates hyprstyle to the latest release.

- **`hyprstyle version`** -- Prints the installed version.

Running `hyprstyle` with no arguments (or an unknown command) prints the usage text. If no config exists yet, a starter one is created first.

# Styles, folders and names
- Styles are stored in `styleDir` (default `~/.config/hyprstyle/styles`). Each style is a folder containing one saved item per component, a `.style` marker file, and optionally `.aliases` and `.preview.png`.
- Style names can contain `/` to organize styles into folders, e.g. `hyprstyle save dark/nord`. Folders show up as groups in `list` and as a sidebar in the GUI.
- A style name can't be empty, can't be `current`, can't be an absolute path, and no part of it can be empty or start with a dot.
- `styleDir/current` is a symlink to the currently loaded style. The current style name is also remembered in `~/.config/hyprstyle/current_style`.

# The GUI
`hyprstyle gui` opens a full-screen overlay (powered by quickshell):

- **Search** by style name or alias.
- **Folder sidebar** to filter styles (shown only when you have folders).
- **Click** a style to select it, **double-click** to apply it. The current style is marked with a dot.
- **Details panel** for the selected style, with:
  - **Apply / Reapply**
  - **Preview image** -- drag and drop an image onto the preview, or type a path and press *Set*. *Screenshot* hides the GUI and takes a new screenshot, *Remove* deletes the preview.
  - **Aliases** -- click an alias to remove it, or type a new one and press *Add*.
  - **Rename**
  - **Delete** (click twice to confirm)
- **Save** -- type a name in the top-right box and press *Save* (or Enter) to save your current setup. The GUI hides briefly so the preview screenshot doesn't include it.
- An **Update** button appears when a new version is available.
- Press **Escape** or click outside the window to close it.

# JSON output
`hyprstyle json` prints a single JSON object, indented for readability:

| Field | Description |
|---|---|
| `version` | Installed hyprstyle version |
| `latestVersion` | Latest known release (from the update cache), or `null` |
| `updateAvailable` | `true` if `latestVersion` is newer than `version` |
| `styleDir` | Absolute path of the style directory |
| `configPath` | Path of the config file |
| `current` | Name of the current style, or `null` |
| `folders` | List of folder names containing styles |
| `styles` | List of styles (see below) |
| `components` | Names of all configured components |

Each entry in `styles` has:

| Field | Description |
|---|---|
| `name` | Full name, including folder (e.g. `dark/nord`) |
| `displayName` | Name without the folder |
| `folder` | Folder the style is in, or `""` |
| `path` | Absolute path of the style |
| `current` | Whether this is the current style |
| `aliases` | List of aliases |
| `preview` | Absolute path of the preview image, or `null` |
| `savedAt` | UTC timestamp of when the style was saved |

Example:

```
hyprstyle json | jq '.styles[].name'
```

# Install guide
To install **hyprstyle**, run this command in your desired shell:

`curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh`

# Update guide
Whenever a new version is out, hyprstyle will let you know the next time you run a command (the notice is printed to stderr). The check runs in the background and is cached for 12 hours. To update, run:

`hyprstyle update`

Or, if you'd rather use the install script directly:

`curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh -s -- --update`

# Uninstall guide
To uninstall **hyprstyle**, run this command in your desired shell:

`curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh -s -- --uninstall`

# Configuration
The config lives at `~/.config/hyprstyle/config.json`. If it doesn't exist, hyprstyle writes a starter one the first time you run it. The file supports `//` comments and trailing commas.

| Option | Description |
|---|---|
| `styleDir` | Where saved styles live. `~` is expanded. Default: `~/.config/hyprstyle/styles` |
| `components` | The list of things saved into / loaded from each style (see below) |
| `hyprConfigComponents` | Names of components that are Hyprland config files (see below) |

## Components
Every component is saved into a style on `save`, and loaded back on `set`/`reload`. A component has these fields:

| Field | Applies to | Description |
|---|---|---|
| `name` | both | Name of the component. Also the file/folder name inside the style. |
| `type` | both | `"copy"` (default) or `"script"` |
| `source` | copy | File or folder to copy. `~` is expanded. Leave it as `""` to disable the component without deleting it. |
| `isDirectory` | copy | `true` if `source` is a folder |
| `saveHook` | both | Shell command run after saving (copy) or doing the whole save (script) |
| `loadHook` | both | Shell command run after loading (copy) or doing the whole load (script) |

- **`copy`** copies `source` into the style on save, and back to `source` on load. Hooks are optional extras, e.g. a reload signal. If the source (on save) or the saved item (on load) doesn't exist, the component is skipped.
- **`script`** has no default save/load behavior; `saveHook` and `loadHook` are responsible for the whole operation. Useful for programs that remember an "active" theme rather than storing a theme file.

Hooks are run with `sh -c`, and have these environment variables available:

- `$STYLE_PATH` -- the style's folder (read from / write into it)
- `$COMPONENT` -- the component's name
- `$HOME`

If a hook exits with a non-zero code, that component is reported as failed, but the other components still run.

## Hyprland config components
If a component is one of Hyprland's own config files (files required by `hyprland.lua` or its dependencies), list its name in `hyprConfigComponents` too. When loading a style, those components are applied first, then Hyprland is reloaded, then all the remaining components are applied in parallel, and Hyprland is reloaded once more at the end. This keeps Hyprland from being left mid-reload while the rest of the style applies.

## Configuration examples
**Hyprstyle** automatically generates a config file with instructions on how to set components up.
This is an example config, which includes plenty of info you may need to configure your hyprstyle.

```
{
  "styleDir": "~/.config/hyprstyle/styles",

  "components": [
    // STATIC/ANIMATED WALLPAPER (may require extra programs to run, such as mpvpaper)
    { "name": "wallpaper", "type": "script",
      "saveHook": "if [ -f \"$HOME/wallpaper.mp4\" ]; then cp \"$HOME/wallpaper.mp4\" \"$STYLE_PATH/wallpaper.mp4\"; elif [ -f \"$HOME/wallpaper.jpg\" ]; then cp \"$HOME/wallpaper.jpg\" \"$STYLE_PATH/wallpaper.jpg\"; else echo 'No wallpaper found.'; fi",
      "loadHook": "pkill swaybg; pkill mpvpaper; if [ -f \"$STYLE_PATH/wallpaper.mp4\" ]; then cp \"$STYLE_PATH/wallpaper.mp4\" \"$HOME/wallpaper.mp4\"; rm -f \"$HOME/wallpaper.jpg\"; setsid mpvpaper -o 'no-audio loop' ALL \"$HOME/wallpaper.mp4\" >/dev/null 2>&1 & elif [ -f \"$STYLE_PATH/wallpaper.jpg\" ]; then cp \"$STYLE_PATH/wallpaper.jpg\" \"$HOME/wallpaper.jpg\"; rm -f \"$HOME/wallpaper.mp4\"; setsid swaybg -i \"$HOME/wallpaper.jpg\" -m fill >/dev/null 2>&1 & else echo 'No wallpaper found in style.'; fi" },

    // WAYBAR (if your waybar config folder is empty, it won't be loaded, to ensure you can disable waybar on individual styles)
    { "name": "waybar", "type": "copy", "source": "~/.config/waybar", "isDirectory": true,
      "loadHook": "pkill waybar; if [ -n \"$(ls -A \"$STYLE_PATH/waybar\" 2>/dev/null)\" ]; then setsid waybar >/dev/null 2>&1 & fi" },

    // QUICKSHELL (same as waybar)
    { "name": "quickshell", "type": "copy", "source": "~/.config/quickshell", "isDirectory": true,
      "loadHook": "pkill quickshell; if [ -n \"$(ls -A \"$STYLE_PATH/quickshell\" 2>/dev/null)\" ]; then setsid quickshell >/dev/null 2>&1 & fi" },

    // WOFI, FISH, HYPREMOJI, FASTFETCH (simple folder copying logic)
    { "name": "wofi", "type": "copy", "source": "~/.config/wofi", "isDirectory": true },
    { "name": "fish", "type": "copy", "source": "~/.config/fish", "isDirectory": true },
    { "name": "hypremoji", "type": "copy", "source": "~/.config/hypremoji", "isDirectory": true },
    { "name": "fastfetch", "type": "copy", "source": "~/.config/fastfetch", "isDirectory": true },

    // DUNST, KITTY (simple folder copying + forced manual reload)
    { "name": "dunst", "type": "copy", "source": "~/.config/dunst", "isDirectory": true,
      "loadHook": "pkill -x dunst; setsid dunst >/dev/null 2>&1 &" },
    { "name": "kitty", "type": "copy", "source": "~/.config/kitty", "isDirectory": true,
      "loadHook": "pkill -USR1 kitty" },

    // HYPRLOCK CONFIG (simple file copying logic)
    { "name": "hyprlock.conf", "type": "copy", "source": "~/.config/hypr/hyprlock.conf" },
    { "name": "hyprlock.png", "type": "copy", "source": "~/.config/hypr/hyprlock.png" },

    // HYPRLAND-RELATED CONFIG (simple file copying, + defined as hyprland (see hyprConfigComponents below))
    // Do note, these are files referenced in hyprland.lua via require("..."). Even hyprland.lua itself would be handled like this, but hyprland.lua includes several other features that should NOT be per-style, so it's important to register individual smaller files.
    { "name": "hyprrules.conf", "type": "copy", "source": "~/.config/hypr/hyprrules.lua" },
    { "name": "hyprlook.conf", "type": "copy", "source": "~/.config/hypr/hyprlook.lua" },

    // WATERFOX (similar to dunst/kitty, but runs a script on load instead. This can be used for more complex loading logic. In this case, it kills the current waterfox instance, and re-starts waterfox in the same workspace, to ensure a seamless switching experience)
    { "name": "chrome", "type": "copy", "source": "~/.waterfox/mxvthmxu.default-release/chrome", "isDirectory": true,
      "loadHook": "$HOME/.config/hyprstyle/scripts/restart-browser.sh" },

    // XOURNALPP (this example is purely to display the script component type.)
    // This skips the default pasting logic that comes with the type "copy", and allows you to write completely custom load/save logic. for most components this can be avoided, but it allows for more control over what the component actually does in the style.
    { "name": "xournalpp", "type": "script",
      "saveHook": "[ -f \"$HOME/.config/xournalpp/template\" ] && mkdir -p \"$STYLE_PATH/xournalpp\" && cp \"$HOME/.config/xournalpp/template\" \"$STYLE_PATH/xournalpp/template\"",
      "loadHook": "[ -f \"$STYLE_PATH/xournalpp/template\" ] || exit 0; cp \"$STYLE_PATH/xournalpp/template\" \"$HOME/.config/xournalpp/template\"; pkill -SIGTERM -x xournalpp; for i in $(seq 1 20); do pgrep -x xournalpp >/dev/null || break; sleep 0.1; done; pkill -SIGKILL -x xournalpp 2>/dev/null; rm -f \"$HOME/.config/xournalpp\"/*recovery* \"$HOME/.config/xournalpp/emergencysave.xopp\"; setsid \"$HOME/.config/hypr/Scripts/canvas-start.sh\" >/dev/null 2>&1 &" },
  ],

  // HYPRLAND-RELATED CONFIG
  // Every time you define hyprland related config files, aka files that are required by hyprland.lua or any of its dependencies, it's important that you define them here as well. Placing the component name in here, ensures that they are pasted in a much more secure way, that prevents visual and/or practical glitches for hyprland during style loading.
  "hyprConfigComponents": [
    "hyprrules.conf",
    "hyprlook.conf"
  ]
}
```
