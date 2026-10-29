# Hyprstyle

Save, load and switch between your Hyprland rices, from the terminal or from a GUI.

```sh
curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh
```

- [What it is](#what-it-is)
- [Requirements](#requirements)
- [Getting started](#getting-started)
- [Commands](#commands)
- [Styles](#styles)
- [The GUI](#the-gui)
- [Components](#components)
- [Config reference](#config-reference)
- [JSON output](#json-output)
- [Updating and uninstalling](#updating-and-uninstalling)

## What it is

Hyprstyle saves your setup (call it a style, a rice, dotfiles, whatever tickles your fancy) and loads it back whenever you want. A style is made of **components**: the files, folders and apps you tell hyprstyle to remember, like your terminal config, your bar, your wallpaper.

Hyprstyle doesn't care what's in your components. You describe them once, then `save` and `set` do the rest.

## Requirements

- **Hyprland** (`hyprctl` on your PATH)
- **quickshell** *(optional)*: needed for `hyprstyle gui`
- **grim** *(optional)*: takes the preview screenshots. Without it, styles just have no automatic preview
- **libnotify** *(optional)*: desktop notifications for new versions, and for a missing `grim` or `quickshell`

## Getting started

1. Install it with the command above.
2. Run `hyprstyle gui` and open the **Components** tab. Add the things you want your styles to remember. Or, if you'd rather, you may manually edit the .json config in ~/.config/hyprstyle/.
3. Set your desktop up how you like it, then save it from the **Styles** tab, or with `hyprstyle save my-style`.
4. Switch back to it any time with `hyprstyle set my-style`, or double-click it in the GUI.

The first time you run a command, hyprstyle writes a starter config to `~/.config/hyprstyle/config.json` and stops, so you can add your components before doing anything else.

## Commands

| Command | What it does |
|---|---|
| `hyprstyle list` | Lists your styles, grouped by folder, with their aliases and descriptions. The loaded one is marked with a ★ |
| `hyprstyle current` | Prints the current style's name, or nothing if there isn't one. Useful for other programs to read and display |
| `hyprstyle save <name>` | Saves a new style, or replaces an existing one (aliases are kept). Takes a preview with `grim` if it's installed, and the style becomes the current one. You can also save styles in folders (`folder/name`) |
| `hyprstyle set <name\|alias>` | Loads a style |
| `hyprstyle reload` | Loads the current style again |
| `hyprstyle rename <name> <new_name>` | Renames a style. A new name with a folder (`folder/new_name`) moves it there |
| `hyprstyle alias <name> <alias>` | Gives a style an alias you can use in `set` |
| `hyprstyle deletealias <name> <alias>` | Removes an alias |
| `hyprstyle description <name> [description]` | Sets a short description for a style (up to 60 characters), shown in `list` and in the GUI. Leave it out to remove the description |
| `hyprstyle undo` | Undoes the last `set` or `save`. Loading a style goes back to your previous setup, saving puts the old version back, or deletes the style if it's new. Only the most recent action is remembered |
| `hyprstyle setpreview [name] [image]` | Sets a style's preview from an image (path or `file://` URL), or from a `grim` screenshot if you don't give one. Defaults to the current style |
| `hyprstyle deletepreview [name]` | Removes a preview. Defaults to the current style |
| `hyprstyle delete <name>` | Deletes a style. Give it only a folder name, and the whole folder gets deleted |
| `hyprstyle gui` | Opens the GUI |
| `hyprstyle json` | Prints everything about your styles and config as JSON, primarily for making your own GUIs, see [JSON output](#json-output) |
| `hyprstyle changelogs` | Shows what changed in every version up to the one you have |
| `hyprstyle update` | Updates hyprstyle to the latest release |
| `hyprstyle version` | Prints the installed version |

Running `hyprstyle` with no arguments, or with something it doesn't know, prints the usage text.

For `setpreview`, a single argument is treated as a style name if one matches, and as an image path otherwise.

## Styles

- Styles live in `styleDir` (`~/.config/hyprstyle/styles` by default). Each one is a folder holding one saved item per component, plus a `.style` marker and, if set, `.aliases`, `.description` and `.preview.png`.
- A name can contain `/` to organize styles into folders, like `hyprstyle save dark/nord`. Folders show up as groups in `hyprstyle list` and in the GUI's sidebar, and only exist while they hold styles, otherwise get automatically deleted.
- A name can't be empty, can't be `current`, can't be an absolute path, and none of its parts can be empty or start with a dot.
- Aliases can't be empty, start with a dot, or contain spaces or slashes. Two styles can't share an alias, and an alias can't match a style's name.
- Saving over a name that's already a folder, or that sits inside another style, is refused, so a save can never wipe out other styles.
- Descriptions are a single line of up to 60 characters. Saving over a style keeps its description, just like its aliases.
- Before every `set`, `reload` and `save`, hyprstyle keeps what it needs to undo it in `~/.config/hyprstyle/undo/`: a snapshot of your current setup for a load, or the old copy of the style for a save. Each new action replaces the last one, and renaming or deleting a style that a save-undo points to forgets it.
- Deleting the current style leaves you with no current style (very intuitive). `styleDir/current` is a symlink to the loaded one.

## The GUI

`hyprstyle gui` opens a full-screen overlay powered by quickshell. Press **Escape** or click outside it to close.

### Styles tab

- **Search** by name or alias, and a **folder sidebar** when you have folders.
- **Click** a style to select it, **double-click** to apply it. The current one has a dot.
- The **details panel** has Apply / Reapply, Update with current setup, description, preview image controls, aliases, rename, and delete. Descriptions also show in small text under each style's name.
- **Undo load** / **Undo save** appears next to the save box after you apply or save a style, and reverts it.
- **Save current setup as...** in the top right saves a new style. The GUI hides for a moment so it isn't in the preview screenshot.
- An **Update** button shows up when a new version is out.

### Components tab

This is where you set up what your styles are made of. Every component is a card in a scrollable list, and the **+** card at the end is always there for adding more.

- Give it a **name**. Names can't be empty or repeated, and can't contain a slash or start with a dot.
- Pick **Copy** or **Script** at the top right of the card, and **Delete** (click twice) removes it.
- **Copy** components copy a file or folder into your style on save, and copy it back when you load the style. You can also turn on a command to run after saving, after loading, or both. There's a switch for marking a component as one of Hyprland's own config files. This means, if your file is one of hyprland's .lua files, it should be marked as one. If not, it should be left off.
- **Script** components are for apps a plain copy can't handle. You write the commands that run when a style is saved and when it's loaded.

Saving only rewrites the `components` and `hyprConfigComponents` lists in your config, so the rest of the file stays as you wrote it. A copy of the previous file is kept next to it as `config.json.bak`.

### Changelog

The bar at the bottom of both tabs shows what's new in your version. Click it for the full list, oldest to newest.

### Customizing the GUI

The GUI is a handful of plain `.qml` files that hyprstyle puts in `~/.config/hyprstyle/gui/` when you open it. Edit them and your changes stick. When a hyprstyle update ships a newer version of a file you edited, your copy is saved as `<file>.bak` and the new one takes its place. You may write your own GUI elsewhere, using `hyprstyle json`, although updates may add new features, so it's recommended you take a look at the base GUI every update.

## Components

A component is one thing your styles remember. There are two kinds.

**`copy`** copies a file or folder into the style when you save, and back to where it came from when you load. If the file isn't there on save, or isn't in the style on load, that component is skipped.

**`script`** has no built-in behavior. Its `saveHook` and `loadHook` do the whole job. Use it for apps that remember an "active" theme instead of keeping a theme file, or for any complex save/load systems that a simple copy can't handle.

Both kinds can run shell commands, called **hooks**:

- On a `copy`, `saveHook` runs after saving and `loadHook` after loading, for things like reloading an app.
- On a `script`, they *are* the save and the load.

Hooks run with `sh -c` and get these:

| Variable | Value |
|---|---|
| `$STYLE_PATH` | The style's folder. Read from it and write into it |
| `$COMPONENT` | The component's name |
| `$HOME` | Your home folder |

If a hook exits with a non-zero code, that component is reported as failed and the rest carry on.

### Hyprland config files

If a component is one of Hyprland's own config files (something `hyprland.lua` loads), mark it as one in the GUI, or list its name in `hyprConfigComponents`. On load, those are applied first, Hyprland reloads, the remaining components apply in parallel, and Hyprland reloads once more at the end. That way Hyprland is never caught mid-reload while the rest of the style applies. The result is mostly visual, hyprstyle functions even without marking them, however doing it ensures it doesn't flash any error messages during the transition.

## Config reference

Everything the Components tab does can also be done by editing `~/.config/hyprstyle/config.json` yourself. It supports `//` comments and trailing commas.

| Option | Description |
|---|---|
| `styleDir` | Where styles live. `~` is expanded. Default `~/.config/hyprstyle/styles` |
| `components` | The components, see below |
| `hyprConfigComponents` | Names of the components that are Hyprland config files |

Each entry in `components`:

| Field | For | Description |
|---|---|---|
| `name` | both | The component's name, and the name of its file or folder inside a style |
| `type` | both | `"copy"` or `"script"` |
| `source` | copy | The file or folder to copy. `~` is expanded. Leave it as `""` to switch the component off without deleting it |
| `saveHook` | both | Shell command for saving |
| `loadHook` | both | Shell command for loading |

> **Upgrading from 1.3.x:** the `isDirectory` option is gone. Hyprstyle now works that out itself, and removes the option from your config the first time you run it (the old file is kept as `config.json.bak`).

### Example

```jsonc
{
  "styleDir": "~/.config/hyprstyle/styles",

  "components": [
    // Things that only need copying.
    { "name": "wofi", "type": "copy", "source": "~/.config/wofi" },
    { "name": "fish", "type": "copy", "source": "~/.config/fish" },
    { "name": "hyprlock.conf", "type": "copy", "source": "~/.config/hypr/hyprlock.conf" },

    // Things that need a reload afterwards.
    {
      "name": "dunst",
      "type": "copy",
      "source": "~/.config/dunst",
      "loadHook": "pkill -x dunst; setsid dunst >/dev/null 2>&1 &"
    },
    {
      "name": "kitty",
      "type": "copy",
      "source": "~/.config/kitty",
      "loadHook": "pkill -USR1 kitty"
    },

    // In this example, an empty waybar folder in a style means "don't run waybar" for that style.
    {
      "name": "waybar",
      "type": "copy",
      "source": "~/.config/waybar",
      "loadHook": "pkill waybar; if [ -n \"$(ls -A \"$STYLE_PATH/waybar\" 2>/dev/null)\" ]; then setsid waybar >/dev/null 2>&1 & fi"
    },

    // Hyprland files loaded by hyprland.lua. Also listed in hyprConfigComponents below.
    { "name": "hyprrules.lua", "type": "copy", "source": "~/.config/hypr/hyprrules.lua" },
    { "name": "hyprlook.lua", "type": "copy", "source": "~/.config/hypr/hyprlook.lua" },

    // A script component: image or video wallpaper.
    {
      "name": "wallpaper",
      "type": "script",
      "saveHook": "if [ -f \"$HOME/wallpaper.mp4\" ]; then cp \"$HOME/wallpaper.mp4\" \"$STYLE_PATH/wallpaper.mp4\"; elif [ -f \"$HOME/wallpaper.jpg\" ]; then cp \"$HOME/wallpaper.jpg\" \"$STYLE_PATH/wallpaper.jpg\"; else echo 'No wallpaper found.'; fi",
      "loadHook": "pkill swaybg; pkill mpvpaper; if [ -f \"$STYLE_PATH/wallpaper.mp4\" ]; then cp \"$STYLE_PATH/wallpaper.mp4\" \"$HOME/wallpaper.mp4\"; rm -f \"$HOME/wallpaper.jpg\"; setsid mpvpaper -o 'no-audio loop' ALL \"$HOME/wallpaper.mp4\" >/dev/null 2>&1 & elif [ -f \"$STYLE_PATH/wallpaper.jpg\" ]; then cp \"$STYLE_PATH/wallpaper.jpg\" \"$HOME/wallpaper.jpg\"; rm -f \"$HOME/wallpaper.mp4\"; setsid swaybg -i \"$HOME/wallpaper.jpg\" -m fill >/dev/null 2>&1 & else echo 'No wallpaper found in style.'; fi"
    }
  ],

  "hyprConfigComponents": [
    "hyprrules.lua",
    "hyprlook.lua"
  ]
}
```

## JSON output

`hyprstyle json` prints one indented JSON object, made for building your own quickshell GUIs and for piping into tools like `jq`:

```sh
hyprstyle json | jq '.styles[].name'
```

| Field | Description |
|---|---|
| `version` | Installed version |
| `latestVersion` | Latest known release (from the update cache), or `null` |
| `updateAvailable` | `true` if `latestVersion` is newer than `version` |
| `undo` | What `hyprstyle undo` would revert, as `{ "type": "set" or "save", "style": name }`, or `null` if there's nothing to undo |
| `styleDir` | Absolute path of the style directory |
| `configPath` | Path of the config file |
| `current` | Name of the current style, or `null` |
| `folders` | Folders that contain styles |
| `styles` | The styles, see below |
| `components` | Names of all components |
| `componentConfigs` | Every component's settings: `name`, `type`, `source`, `saveHook`, `loadHook` and `hyprConfig` |
| `changelogs` | `version` and `log` for every release up to the installed one |

Each entry in `styles`:

| Field | Description |
|---|---|
| `name` | Full name, folder included (`dark/nord`) |
| `displayName` | Name without the folder |
| `folder` | Its folder, or `""` |
| `path` | Absolute path |
| `current` | Whether it's the current style |
| `aliases` | Its aliases |
| `description` | Its description, or `""` |
| `preview` | Absolute path of the preview image, or `null` |
| `savedAt` | UTC time it was saved |

The GUI saves components through `hyprstyle --save-components '<json>'`, where the JSON looks like `{ "components": [...], "hyprConfigComponents": [...] }`. It validates the names, then rewrites those two lists in your config.

## Updating and uninstalling

Every command checks for a new release at most once every 5 minutes, in a background process that never slows down what you ran. When one is found you get a desktop notification (once per version), and every command prints a one-line notice until you update.

To update, run:

```sh
hyprstyle update
```

or, if you'd rather use the install script directly:

```sh
curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh -s -- --update
```

To uninstall:

```sh
curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh -s -- --uninstall
```

Run `hyprstyle changelogs` after an update to see what changed.