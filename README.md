# Hyprstyle

A CLI+GUI tool to save, load, and switch between your Hyprland rices.

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
- **libnotify (notify-send)** *(optional)* -- used to send a desktop notification when a new version is out, and to show an error notification when `grim` or `quickshell` is missing.

# How do I use Hyprstyle?
**Hyprstyle** uses only a few main commands, which are listed here:

- **`hyprstyle list`** -- Lists all saved styles, grouped by folder, along with their aliases. The currently loaded style is marked with a ★.

- **`hyprstyle current`** -- Gets the current style name, or "" if there's none. Most useful for other programs to read the current style name. (`hyprstyle current name` still works too.)

- **`hyprstyle save <name>`** -- Saves a new style or replaces an existing one. Replacing keeps the style's aliases. If `grim` is installed, a preview screenshot is saved with it. The saved style becomes the current style. Use `folder/name` to save a style inside a folder. A name that is already a folder (or sits inside another style) is refused, so saving can never wipe out other styles.

- **`hyprstyle set <name|alias>`** -- Loads a saved style.

- **`hyprstyle reload`** -- Loads the current style again.

- **`hyprstyle rename <name> <new_name>`** -- Renames a saved style. The new name can include a folder (`folder/new_name`) to move the style there. Folders left empty by the move are removed.

- **`hyprstyle alias <name> <alias>`** -- Gives a style an alias, which can be used instead of its name in `set`. Two styles can't share the same alias, and an alias can't be the same as an existing style name. Aliases can't be empty, start with a dot, or contain spaces or slashes.

- **`hyprstyle deletealias <name> <alias>`** -- Removes an alias from a style.

- **`hyprstyle setpreview [name] [image]`** -- Sets a style's preview image.
  - With an image path (or `file://` URL), that image is used.
  - With no image, a screenshot is taken with `grim` instead.
  - With no name, the current style is used.
  - If you pass a single argument, it's treated as a style name if one matches, otherwise as an image path.

- **`hyprstyle deletepreview [name]`** -- Removes a style's preview. Defaults to the current style.

- **`hyprstyle delete <name>`** -- Deletes a saved style. Passing a folder name deletes the whole folder and everything in it. Folders that end up empty are removed automatically. If the deleted style was the current one, there is simply no current style afterwards.

- **`hyprstyle gui`** -- Opens a GUI showing all your saved styles with previews. Requires `quickshell`. See [The GUI](#the-gui) below.

- **`hyprstyle json`** -- Prints all style data (styles, aliases, previews, current style, version, update status) as indented, human-readable JSON. Made for building your own quickshell GUIs, and easy to pipe into tools like `jq`. See [JSON output](#json-output) below.

- **`hyprstyle update`** -- Updates hyprstyle to the latest release.

- **`hyprstyle version`** -- Prints the installed version.

Running `hyprstyle` with no arguments (or an unknown command) prints the usage text. If no config exists yet, a starter one is created first.

# Styles, folders and names
- Styles are stored in `styleDir` (default `~/.config/hyprstyle/styles`). Each style is a folder containing one saved item per component, a `.style` marker file, and optionally `.aliases` and `.preview.png`.
- Style names can contain `/` to organize styles into folders, e.g. `hyprstyle save dark/nord`. Folders show up as groups in `list` and as a sidebar in the GUI. A folder only exists while it contains styles; deleting or moving the last one removes it.
- A style name can't be empty, can't be `current`, can't be an absolute path, and no part of it can be empty or start with a dot.

## The current style
- `styleDir/current` is a symlink to the currently loaded style.
- Older versions kept a copy of the name in `~/.config/hyprstyle/current_style`. It is no longer read, you can delete it.

# The GUI
`hyprstyle gui` opens a full-screen overlay (powered by quickshell).

- **Search** by style name or alias.
- **Folder sidebar** to filter styles (shown only when you have folders).
- **Click** a style to select it, **double-click** to apply it. The current style is marked with a dot.
- **Details panel** for the selected style, with:
  - **Apply / Reapply**
  - **Update with current setup** -- overwrites the selected style with your current setup (click twice to confirm). Aliases are kept and the style becomes the current one. The GUI hides briefly so the preview screenshot doesn't include it.
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
| `current` | Name of the current style, or `null` if there is none |
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
Every command that runs checks, at most once every 5 minutes, whether a new release is out. The check is a single small request that runs in a background process, so it never slows the command you actually ran.

When the background check finds a new version, you get a desktop notification (through `notify-send`), once per version. After that, every command prints a one-line notice to stderr until you update. The notice disappears as soon as you're on the new version.

To update, run:

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
    // COPY COMPONENT (see script component below)
    // Copy components are components which automatically copy
    // a file/folder (based on isDirectory) to your style folder.
    
    // As shown further below, you can provide an extra loadHook,
    // which is ran right after the copying is done. It's most commonly
    // used to force a hot reload, or to restart an app for its config to update.

    // Configs that only need copying.
    {
      "name": "wofi",
      "type": "copy",
      "source": "~/.config/wofi",
      "isDirectory": true
    },
    {
      "name": "fish",
      "type": "copy",
      "source": "~/.config/fish",
      "isDirectory": true
    },
    {
      "name": "hypremoji",
      "type": "copy",
      "source": "~/.config/hypremoji",
      "isDirectory": true
    },
    {
      "name": "hyprlock.conf",
      "type": "copy",
      "source": "~/.config/hypr/hyprlock.conf"
    },
    {
      "name": "hyprlock.png",
      "type": "copy",
      "source": "~/.config/hypr/hyprlock.png"
    },

    // Configs that need restarting or reloading.
    {
      "name": "dunst",
      "type": "copy",
      "source": "~/.config/dunst",
      "isDirectory": true,
      "loadHook": "pkill -x dunst; setsid dunst >/dev/null 2>&1 &"
    },
    {
      "name": "kitty",
      "type": "copy",
      "source": "~/.config/kitty",
      "isDirectory": true,
      "loadHook": "pkill -USR1 kitty"
    },

    // Waybar: an empty config directory disables it for that style, for quickshell compatibility.
    {
      "name": "waybar",
      "type": "copy",
      "source": "~/.config/waybar",
      "isDirectory": true,
      "loadHook": "pkill waybar; if [ -n \"$(ls -A \"$STYLE_PATH/waybar\" 2>/dev/null)\" ]; then setsid waybar >/dev/null 2>&1 & fi"
    },

    // Quickshell: an empty config directory disables it for that style, for wayland compatibility.
    {
      "name": "quickshell",
      "type": "copy",
      "source": "~/.config/quickshell",
      "isDirectory": true,
      "loadHook": "pkill quickshell; if [ -n \"$(ls -A \"$STYLE_PATH/quickshell\" 2>/dev/null)\" ]; then setsid quickshell >/dev/null 2>&1 & fi"
    },

    // Hyprland files loaded by hyprland.lua.
    // Register these below in hyprConfigComponents as well.
    {
      "name": "hyprrules.lua",
      "type": "copy",
      "source": "~/.config/hypr/hyprrules.lua"
    },
    {
      "name": "hyprlook.lua",
      "type": "copy",
      "source": "~/.config/hypr/hyprlook.lua"
    },

    // SCRIPT COMPONENT
    // Use this when copying files isn't enough and you need custom
    // save/load logic. For longer scripts, you can call a separate
    // .sh file instead of putting everything here.

    // Wallpaper: supports both video and image wallpapers.
    {
      "name": "wallpaper",
      "type": "script",
      "saveHook": "if [ -f \"$HOME/wallpaper.mp4\" ]; then cp \"$HOME/wallpaper.mp4\" \"$STYLE_PATH/wallpaper.mp4\"; elif [ -f \"$HOME/wallpaper.jpg\" ]; then cp \"$HOME/wallpaper.jpg\" \"$STYLE_PATH/wallpaper.jpg\"; else echo 'No wallpaper found.'; fi",
      "loadHook": "pkill swaybg; pkill mpvpaper; if [ -f \"$STYLE_PATH/wallpaper.mp4\" ]; then cp \"$STYLE_PATH/wallpaper.mp4\" \"$HOME/wallpaper.mp4\"; rm -f \"$HOME/wallpaper.jpg\"; setsid mpvpaper -o 'no-audio loop' ALL \"$HOME/wallpaper.mp4\" >/dev/null 2>&1 & elif [ -f \"$STYLE_PATH/wallpaper.jpg\" ]; then cp \"$STYLE_PATH/wallpaper.jpg\" \"$HOME/wallpaper.jpg\"; rm -f \"$HOME/wallpaper.mp4\"; setsid swaybg -i \"$HOME/wallpaper.jpg\" -m fill >/dev/null 2>&1 & else echo 'No wallpaper found in style.'; fi"
    },
  ],

  // Files that Hyprland depends on. These are applied with extra care
  // during style loading to avoid visual glitches.
  "hyprConfigComponents": [
    "hyprrules.lua",
    "hyprlook.lua"
  ]
}
```
