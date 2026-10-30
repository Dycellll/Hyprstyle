static partial class Changelogs
{
    private static void Register()
    {
        AddChangelog("v1.6.2",
            """
            - Modified the GUI to run under a different process name (hyprstyle-gui), so that "pkill quickshell" in load hooks doesn't kill it.
            - Modified the transition to run under a different process name (hyprstyle-overlay), so that "pkill quickshell" in load hooks doesn't kill it.
            """);
        AddChangelog("v1.6.1",
            """
            - Added "--timing". Put it in a command, like "hyprstyle set my-style --timing", to see how long every step of a load or save took. Useful for optimizing your load/save times. Do note, they run in parallel, so the total time is not the sum of each step's time.
            """);
        AddChangelog("v1.6.0",
            """
            - Loading a style now plays a transition.
            - Pick between "circle" (grows from your cursor), "circle-centered", "horizontal", "vertical" and "diagonal", and set how long it lasts, in the GUI or with "transition" and "transitionMs" in config.json. Turn it off with "none", the GUI toggle, or --no-transition.
            - While a style loads, a subtle progress bar shows under the frozen screenshot, so the screen never just sits there.
            - The transition needs grim and quickshell. If either is missing, styles load as before.
            """);
        AddChangelog("v1.5.0",
            """
            - Styles can now have a short description, up to 60 characters. Set one with "hyprstyle description <name> <description>" or in the GUI's details panel. It shows up in "hyprstyle list" and under the style's name in the GUI.
            - Added "hyprstyle undo", and an Undo button in the GUI. It reverts your last load or save, and only remembers that one action.
            """);
        AddChangelog("v1.4.0",
            """
            - Added a Components tab in the GUI. Add, edit and remove your components there instead of editing config.json by hand.
            - Removed the "isDirectory" option. Hyprstyle works out on its own whether a component is a file or a folder, and removes the option from your config for you.
            - Added changelogs. Run "hyprstyle changelogs", or click the bar at the bottom of the GUI.
            - The GUI is now made of .qml files in ~/.config/hyprstyle/gui.
            """);
        AddChangelog("v1.3.1",
            """
            - Improved update logic on the GUI, which used to almost never trigger update checks on its own.
            """);
        AddChangelog("v1.3.0", 
            """
            - Improved the GUI by adding various animations and color to important actions.
            - Improved the updater by reducing the time between checks and adding notifications.
            - Fixed minor bugs regarding folder deleting, duplicate references, and various performance issues.
            """);
        AddChangelog("v1.2.3",
            """
            - Fixed a folder sidebar bug, where deleting the last folder would glitch the UI into thinking no styles existed, until you closed and reopened it.
            """);
        AddChangelog("v1.2.2",
            """
            - Fixed a GUI bug: the sidebar to pick between folders was the wrong width, taking up the entire window.
            - Reduced update cache lifetime from 12 hours to 1 hour. This means that updates on the github get noticed faster by the app.
            """);
        AddChangelog("v1.2.1",
            """
            - Fixed a minor bug with previews, where it would display the usage tutorial even though the command was valid.
            """);
        AddChangelog("v1.2.0",
            """
            - Added a GUI for Hyprstyle! Run "hyprstyle gui" to open it.
            - Added "hyprstyle reload", "hyprstyle rename", aliases, and various other commands.
            - Added "hyprstyle update" and the github update system.
            """);
        AddChangelog("v1.1.0",
            """
            - Improved speed of "hyprstyle current", which is now essentially instant.
            - A lot of minor bug fixes and improvements.
            """);
        AddChangelog("v1.0.0",
            """
            - Initial release of Hyprstyle.
            """);
    }
}
