using Visa2026.Module.BusinessObjects;

namespace Visa2026.Tools.CarboneSpike;

internal static class SpikeSampleFactory
{
    /// <summary>Minimal 1×1 PNG for injector smoke tests.</summary>
    public static byte[] TinyPng { get; } =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
        0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
        0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
        0xAE, 0x42, 0x60, 0x82,
    ];

    public static Application BuildApplication(int itemCount, bool withVisaSample = true)
    {
        var application = new Application
        {
            FullApplicationNumber = "3/-433",
            ApplicationDate = new DateTime(2026, 3, 24),
        };

        for (int i = 1; i <= itemCount; i++)
        {
            var item = new ApplicationItem();
            if (withVisaSample || i == 1)
            {
                item.CurrentVisa = new Visa
                {
                    VisaNumber = "A1691452",
                    StartDate = new DateTime(2026, 2, 19),
                    ExpirationDate = new DateTime(2026, 8, 6),
                    VisaCategory = new VisaCategory { NameTm = "köp gezeklik", Name = "Multiple" },
                };
            }

            application.ApplicationItems.Add(item);
        }

        return application;
    }
}
