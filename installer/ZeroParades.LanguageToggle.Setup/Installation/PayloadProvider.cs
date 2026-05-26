using System.Reflection;

namespace ZeroParades.LanguageToggle.Setup.Installation;

public interface IPayloadProvider
{
    Stream OpenBepInExArchive();

    Stream OpenPluginDll();
}

public sealed class EmbeddedPayloadProvider : IPayloadProvider
{
    private const string BepInExResourceName = "Payloads.BepInEx.zip";
    private const string PluginResourceName = "Payloads.ZeroParades.LanguageToggle.dll";

    private readonly Assembly _assembly;

    public EmbeddedPayloadProvider()
        : this(typeof(EmbeddedPayloadProvider).Assembly)
    {
    }

    internal EmbeddedPayloadProvider(Assembly assembly)
    {
        _assembly = assembly;
    }

    public Stream OpenBepInExArchive() => OpenRequiredResource(BepInExResourceName);

    public Stream OpenPluginDll() => OpenRequiredResource(PluginResourceName);

    private Stream OpenRequiredResource(string name) =>
        _assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"安装器资源缺失：{name}");
}
