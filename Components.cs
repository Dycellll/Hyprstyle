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
    private readonly string? saveHook;
    private readonly string? loadHook;

    public CopyComponent(string name, string source, string? saveHook, string? loadHook)
        : base(name)
    {
        this.source = source;
        this.saveHook = saveHook;
        this.loadHook = loadHook;
    }

    public override void Save(string stylePath)
    {
        if (string.IsNullOrEmpty(source))
            return;

        string destination = Path.Join(stylePath, Name);

        if (Directory.Exists(source))
            Files.CopyDirectory(source, destination);
        else if (File.Exists(source))
            Files.CopyFile(source, destination);
        else
            return;

        Processes.RunHook(saveHook, stylePath, Name);
    }

    public override void Load(string stylePath)
    {
        if (string.IsNullOrEmpty(source))
            return;

        string saved = Path.Join(stylePath, Name);

        if (Directory.Exists(saved))
            Files.ReplaceDirectory(saved, source);
        else if (File.Exists(saved))
            Files.CopyFile(saved, source);
        else
            return;

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
