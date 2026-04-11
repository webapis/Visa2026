using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.DesignTime;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Utils;

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
                DevExpress.Licensing.License.EmbedLicenseKey("LCXv1TmMzVVAxQ205eDRHRU1Ca0paYmRGSVc3SEl2RDBaQzR2RUZXZ1VTZzdNNGlrSVpLR2hHekw1aU1TdkZoTGsra2ZnbWdobDA3NEo0VnJURkx2Vk1MYmRYaDRBUklnQlpKM3REMEpndWYwMkhtVXF2SzFvUWpQWXhqNVVnWUVTVjBCOWFYQWc9PUs4blYrU25CZCpLdE55SmZbRHJbJE5YZkw5Qjhnb2ZicjJHMiRpZFtZWHkjLWlidj46REI0QiRHPkM7ckU/aUBwOnJHI2pMN1pAZjp5Tnk+IzcsKERjakpAPG5yRGo/Wzo2QDxbYy00LThbZSVkZDMpeTxhQChbW2Q2bVEkITgsJENGO21ybGwkbg0KV11VbzFUSF9PLkohK3UwaXo+cit1IXlacitPYS5KSEpIMWE4YnUuLjhvYmFfOEVIYl84Yl9fYWFVXVUxMG8xbUQNCixbciEuICFyY0pJI0o4ODg4ODg4ODg4ODg4ODg4ODg4OmFUYU9fYWFiYlVhR2JdSFRvVVVUSEdVT0dPYjFUVGJVSF9VVF9fX19fX19fX09iMVRUYlVIX1VUX19fX19fX19f");
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
                    host.Run();
                }
            }
            return 0;
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        XafApplication IDesignTimeApplicationFactory.Create()
        {
            IHostBuilder hostBuilder = CreateHostBuilder(Array.Empty<string>());
            return DesignTimeApplicationFactoryHelper.Create(hostBuilder);
        }
    }
}
