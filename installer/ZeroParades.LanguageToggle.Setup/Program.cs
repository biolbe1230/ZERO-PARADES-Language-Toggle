using ZeroParades.LanguageToggle.Setup.Installation;

namespace ZeroParades.LanguageToggle.Setup;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        InstallerService service = new(new EmbeddedPayloadProvider());
        if (args.Length > 0)
        {
            return CommandLineApplication.Run(args, service, Console.Out);
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm(service));
        return 0;
    }
}
