namespace Visa2026.Module.BusinessObjects
{
    /// <summary>Navigation and ListView ids for <see cref="Application"/> split by <see cref="ApplicationProgressRouteKind"/>.</summary>
    public static class ApplicationProgressRouteNavigation
    {
        public const string NavItemViaMinistries = "Application_ViaMinistries";
        public const string NavItemDirectMigration = "Application_DirectMigration";

        public const string ListViewViaMinistries = "Application_ListView_ViaMinistries";
        public const string ListViewDirectMigration = "Application_ListView_DirectMigration";

        public const string CriteriaViaMinistries =
            "ApplicationType is not null And ApplicationType.ApplicationProgressRoute = ##Enum#"
            + "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,ViaMinistries#";

        public const string CriteriaDirectMigration =
            "ApplicationType is not null And ApplicationType.ApplicationProgressRoute = ##Enum#"
            + "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,DirectToMigrationService#";
    }
}
