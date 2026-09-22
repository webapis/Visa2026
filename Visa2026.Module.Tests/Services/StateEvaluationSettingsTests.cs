using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class StateEvaluationSettingsTests
{
    [Fact]
    public void FromObjectSpace_Null_UsesDefaultWindowAndEmptyRules()
    {
        var settings = StateEvaluationSettings.FromObjectSpace(null);

        Assert.Equal(ExpirationAlertRule.DefaultExpiringSoonDays, settings.DefaultExpiringSoonDays);
        Assert.Equal(ExpirationAlertRule.DefaultExpiringSoonDays, settings.GetExpiringSoonDays("Visa"));
        Assert.Null(settings.GetExtensionApplicationRequiredDays("Visa"));
    }

    [Fact]
    public void FromSystemSettings_IgnoresArgument_UsesDefaults()
    {
        var settings = StateEvaluationSettings.FromSystemSettings(null);

        Assert.Equal(ExpirationAlertRule.DefaultExpiringSoonDays, settings.DefaultExpiringSoonDays);
        Assert.Equal(ExpirationAlertRule.DefaultExpiringSoonDays, settings.GetExpiringSoonDays("Passport"));
    }

    [Fact]
    public void GetExpiringSoonDays_UsesRuleWhenPositive_ElseDefault()
    {
        var settings = new StateEvaluationSettings(
            defaultExpiringSoonDays: 30,
            rulesByKey: new Dictionary<string, ExpirationAlertRule>
            {
                ["Visa"] = new ExpirationAlertRule
                {
                    BusinessObjectKey = "Visa",
                    ExpiringSoonDays = 14,
                    ExtensionApplicationRequiredDays = 60,
                },
                ["Passport"] = new ExpirationAlertRule
                {
                    BusinessObjectKey = "Passport",
                    ExpiringSoonDays = 0,
                },
            });

        Assert.Equal(14, settings.GetExpiringSoonDays("Visa"));
        Assert.Equal(14, settings.GetExpiringSoonDays(" Visa "));
        // Lookup is ordinal after Trim — case must match the dictionary key.
        Assert.Equal(30, settings.GetExpiringSoonDays("visa"));
        Assert.Equal(30, settings.GetExpiringSoonDays("Passport"));
        Assert.Equal(30, settings.GetExpiringSoonDays("Unknown"));
        Assert.Equal(30, settings.GetExpiringSoonDays(" "));
        Assert.Equal(30, settings.GetExpiringSoonDays(null!));
    }

    [Fact]
    public void GetExtensionApplicationRequiredDays_PositiveOnly()
    {
        var settings = new StateEvaluationSettings(
            defaultExpiringSoonDays: 30,
            rulesByKey: new Dictionary<string, ExpirationAlertRule>
            {
                ["Visa"] = new ExpirationAlertRule
                {
                    BusinessObjectKey = "Visa",
                    ExpiringSoonDays = 14,
                    ExtensionApplicationRequiredDays = 90,
                },
                ["WorkPermitItem"] = new ExpirationAlertRule
                {
                    BusinessObjectKey = "WorkPermitItem",
                    ExpiringSoonDays = 20,
                    ExtensionApplicationRequiredDays = 0,
                },
                ["Passport"] = new ExpirationAlertRule
                {
                    BusinessObjectKey = "Passport",
                    ExpiringSoonDays = 10,
                    ExtensionApplicationRequiredDays = null,
                },
            });

        Assert.Equal(90, settings.GetExtensionApplicationRequiredDays("Visa"));
        Assert.Null(settings.GetExtensionApplicationRequiredDays("WorkPermitItem"));
        Assert.Null(settings.GetExtensionApplicationRequiredDays("Passport"));
        Assert.Null(settings.GetExtensionApplicationRequiredDays("Missing"));
    }
}
