static partial class Changelogs
{
    private static void Register()
    {
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
