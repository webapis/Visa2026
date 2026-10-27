using Microsoft.AspNetCore.DataProtection;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Mail;

public static class OfficerMailServiceCollectionExtensions
{
    public const string ProviderKey = "OfficerMail:Provider";

    /// <summary>
    /// Email document copies: mail sender, attachment builder, send service. <c>OfficerMail:Provider</c>
    /// selects the transport: <c>Smtp</c> (default, shared account) or <c>MicrosoftGraph</c> (each officer's
    /// own mailbox). Graph refresh tokens are encrypted with Data Protection; set <c>DataProtection:KeysPath</c>
    /// on hosts where the default key ring does not survive restarts, or officers must reconnect after each restart.
    /// </summary>
    public static IServiceCollection AddVisaOfficerMail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpMailOptions>(configuration.GetSection(SmtpMailOptions.SectionName));
        services.Configure<MicrosoftGraphMailOptions>(configuration.GetSection(MicrosoftGraphMailOptions.SectionName));
        services.AddMemoryCache();
        services.AddHttpClient(OfficerMailTokenService.HttpClientName, client => client.Timeout = TimeSpan.FromMinutes(3));

        var dataProtection = services.AddDataProtection();
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        services.AddScoped<IOfficerMailSecretProtector, OfficerMailSecretProtector>();
        services.AddScoped<OfficerMailTokenService>();
        if (string.Equals(configuration[ProviderKey]?.Trim(), "MicrosoftGraph", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IOfficerMailSender, GraphOfficerMailSender>();
        else
            services.AddScoped<IOfficerMailSender, SmtpOfficerMailSender>();
        services.AddScoped<EmailDocumentCopiesAttachmentBuilder>();
        services.AddScoped<EmailDocumentCopiesSendService>();
        services.AddScoped<IEmailDocumentCopiesDialogService, EmailDocumentCopiesDialogService>();
        return services;
    }
}
