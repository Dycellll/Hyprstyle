# Hyprstyle
An intuitive cli styles save/load system for **Hyprland**, with a simple install script and easy customization.

**Hyprstyle** allows you to define your own components to save and load, with a .json config file. It lets you configure components to save/load through simple default pasting, or complex scripts, and anything in between.

# Install guide
To install **hyprstyle**, run this command in your desired shell:

`curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh`

# Uninstall guide
To uninstall **hyprstyle**, run this command in your desired shell:

`curl -fsSL https://raw.githubusercontent.com/Dycellll/hyprstyle/main/install.sh | sh -s -- --uninstall`

# Configuration examples
**Hyprstyle** automatically generates a config file with instructions on how to set components up, but here's an example config, which includes plenty of info you may need, to configure your hyprstyle.

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
    { "name": "fastfetch", "type": "copy", "source": "~/.config/fastfetch", "isDirectory": true }

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
