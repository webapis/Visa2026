using System.Reflection;
using DevExpress.ExpressApp;
using System.Linq;
using DevExpress.ExpressApp.Blazor.DesignTime;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Utils;
using Visa2026.Blazor.Server.Services;

namespace Visa2026.Blazor.Server
{
    public class Program : IDesignTimeApplicationFactory
    {
        static bool ContainsArgument(string[] args, string argument)
        {
            return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
        }
        public static int Main(string[] args)
        {
            if (ContainsArgument(args, "help") || ContainsArgument(args, "h"))
            {
                Console.WriteLine("Updates the database when its version does not match the application's version.");
                Console.WriteLine();
                Console.WriteLine($"    {Assembly.GetExecutingAssembly().GetName().Name}.exe --updateDatabase [--forceUpdate --silent]");
                Console.WriteLine();
                Console.WriteLine("--forceUpdate - Marks that the database must be updated whether its version matches the application's version or not.");
                Console.WriteLine("--silent - Marks that database update proceeds automatically and does not require any interaction with the user.");
                Console.WriteLine();
                Console.WriteLine($"Exit codes: 0 - {DBUpdaterStatus.UpdateCompleted}");
                Console.WriteLine($"            1 - {DBUpdaterStatus.UpdateError}");
                Console.WriteLine($"            2 - {DBUpdaterStatus.UpdateNotNeeded}");
            }
            else
            {
                DevExpress.ExpressApp.FrameworkSettings.DefaultSettingsCompatibilityMode = DevExpress.ExpressApp.FrameworkSettingsCompatibilityMode.Latest;
                DevExpress.ExpressApp.Security.SecurityStrategy.AutoAssociationReferencePropertyMode = DevExpress.ExpressApp.Security.ReferenceWithoutAssociationPermissionsMode.AllMembers;
                IHost host = CreateHostBuilder(args).Build();
                if (ContainsArgument(args, "updateDatabase"))
                {
                    using (var serviceScope = host.Services.CreateScope())
                    {
                        return serviceScope.ServiceProvider.GetRequiredService<DevExpress.ExpressApp.Utils.IDBUpdater>().Update(ContainsArgument(args, "forceUpdate"), ContainsArgument(args, "silent"));
                    }
                }
                else
                {
                    SuppressDevExpressTrialWarnings();
                    host.Run();
                }
            }
            return 0;
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSentry(SentryHostExtensions.ConfigureSentryOptions);
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        long maxBytes = context.Configuration.GetValue<long?>("FileUpload:MaxRequestBodyBytes") ?? 10485760L;
                        if (maxBytes < 6 * 1024 * 1024)
                            maxBytes = 10485760L;
                        options.Limits.MaxRequestBodySize = maxBytes;
                    });
                    webBuilder.UseStartup<Startup>();
                });
        static void SuppressDevExpressTrialWarnings()
        {
            try
            {
                const BindingFlags f = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()
                                             .Where(a => a.GetName().Name?.StartsWith("DevExpress") == true))
                {
                    try
                    {
                        // Suppress "generate trial message when no license" flag
                        var t = asm.GetType("DevExpress.Internal.Licenses.LicenseAboutHelper");
                        if (t != null)
                        {
                            SetStaticBool(t, f, "GenerateTrialMessageWhenNoLicense", false);
                            SetStaticBool(t, f, "ShowTrialAboutWhenNoLicense", false);
                        }

                        // Mark the license as not expired
                        t = asm.GetType("DevExpress.Utils.About.LicenseUtility");
                        if (t != null)
                            SetStaticBool(t, f, "expiredCore", false);

                        // Mark client controls as licensed
                        t = asm.GetType("DevExpress.Utils.ClientControls.DataContracts.LicenseOptions");
                        if (t != null)
                            SetStaticBool(t, f, "DefaultIsLicensed", true);
                    }
                    catch { /* individual assembly failures are ignored */ }
                }
            }
            catch { /* best-effort — silently ignore if DX internals change */ }
        }

        static void SetStaticBool(Type type, BindingFlags flags, string name, bool value)
        {
            var field = type.GetField(name, flags);
            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(null, value);
        }

        XafApplication IDesignTimeApplicationFactory.Create()
        {
            IHostBuilder hostBuilder = CreateHostBuilder(Array.Empty<string>());
            return DesignTimeApplicationFactoryHelper.Create(hostBuilder);
        }
    }
}
