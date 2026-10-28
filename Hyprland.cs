using System.Text.Json.Nodes;

static class Hyprland
{
    public static JsonArray GetClients()
    {
        string output = Processes.RunCapture("hyprctl", "clients", "-j");

        return JsonNode.Parse(output)?.AsArray()
            ?? throw new Exception("Invalid hyprctl JSON");
    }

    public static List<string> GetWorkspacesForClasses(params string[] classes)
    {
        var result = new List<string>();

        foreach (var client in GetClientsForClasses(classes))
        {
            string? workspace = client["workspace"]?["id"]?.GetValue<int>().ToString();

            if (workspace != null)
                result.Add(workspace);
        }

        return result;
    }

    public static List<JsonObject> GetClientsForClasses(params string[] classes)
    {
        var result = new List<JsonObject>();

        try
        {
            foreach (var node in GetClients())
            {
                if (node is JsonObject client &&
                    client["class"]?.GetValue<string>() is string windowClass &&
                    classes.Contains(windowClass))
                {
                    result.Add(client);
                }
            }
        }
        catch
        {
        }

        return result;
    }

    public static void MoveWindow(string address, string workspace)
    {
        string rule = $"hl.dsp.window.move({{ workspace = \"{workspace}\", follow = false, window = \"address:{address}\" }})";

        Processes.Run("hyprctl", ["dispatch", rule], suppressOutput: true);
    }

    public static string? WaitForNewWindow(string[] classes, HashSet<string> alreadySeen, int timeoutMs, int pollMs = 60)
    {
        int waited = 0;

        while (waited < timeoutMs)
        {
            foreach (var client in GetClientsForClasses(classes))
            {
                string? address = client["address"]?.GetValue<string>();

                if (!string.IsNullOrEmpty(address) && alreadySeen.Add(address))
                    return address;
            }

            Thread.Sleep(pollMs);
            waited += pollMs;
        }

        return null;
    }

    public static void WaitUntilClosed(string[] classes, int timeoutMs, int pollMs = 60)
    {
        int waited = 0;

        while (waited < timeoutMs && GetClientsForClasses(classes).Count > 0)
        {
            Thread.Sleep(pollMs);
            waited += pollMs;
        }
    }

    public static void Reload() =>
        Processes.Run("hyprctl", ["reload"], suppressOutput: true);
}
