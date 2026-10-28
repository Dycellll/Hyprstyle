abstract class StyleComponent
{
    public string Name { get; }

    protected StyleComponent(string name)
    {
        Name = name;
    }

    public abstract void Save(string stylePath);
    public abstract void Load(string stylePath);
}

sealed class CopyComponent : StyleComponent
{
    private readonly string source;
    private readonly bool isDirectory;
    private readonly string? saveHook;
    private readonly string? loadHook;

    public CopyComponent(string name, string source, bool isDirectory, string? saveHook, string? loadHook)
        : base(name)
    {
        this.source = source;
        this.isDirectory = isDirectory;
        this.saveHook = saveHook;
        this.loadHook = loadHook;
    }

    public override void Save(string stylePath)
    {
        if (string.IsNullOrEmpty(source))
            return;

        if (!File.Exists(source) && !Directory.Exists(source))
            return;

        string destination = Path.Join(stylePath, Name);

        if (isDirectory)
            Files.CopyDirectory(source, destination);
        else
            Files.CopyFile(source, destination);

        Processes.RunHook(saveHook, stylePath, Name);
    }

    public override void Load(string stylePath)
    {
        if (string.IsNullOrEmpty(source))
            return;

        string saved = Path.Join(stylePath, Name);

        if (!File.Exists(saved) && !Directory.Exists(saved))
            return;

        if (isDirectory)
            Files.ReplaceDirectory(saved, source);
        else
            Files.CopyFile(saved, source);

        Processes.RunHook(loadHook, stylePath, Name);
    }
}

sealed class ScriptComponent : StyleComponent
{
    private readonly string? saveHook;
    private readonly string? loadHook;

    public ScriptComponent(string name, string? saveHook, string? loadHook) : base(name)
    {
        this.saveHook = saveHook;
        this.loadHook = loadHook;
    }

    public override void Save(string stylePath) => Processes.RunHook(saveHook, stylePath, Name);

    public override void Load(string stylePath) => Processes.RunHook(loadHook, stylePath, Name);
}
