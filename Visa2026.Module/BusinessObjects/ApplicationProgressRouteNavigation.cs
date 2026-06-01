namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Navigation and ListView ids for <see cref="Application"/> and <see cref="ApplicationItem"/>
    /// split by <see cref="ApplicationProgressRouteKind"/>.
    /// </summary>
    public static class ApplicationProgressRouteNavigation
    {
        public const string NavItemViaMinistries = "Application_ViaMinistries";
        public const string NavItemDirectMigration = "Application_DirectMigration";

        public const string NavItemItemsViaMinistries = "ApplicationItem_ViaMinistries";
        public const string NavItemItemsDirectMigration = "ApplicationItem_DirectMigration";

        public const string ListViewViaMinistries = "Application_ListView_ViaMinistries";
        public const string ListViewDirectMigration = "Application_ListView_DirectMigration";

        public const string ListViewItemsViaMinistries = "ApplicationItem_ListView_ViaMinistries";
        public const string ListViewItemsDirectMigration = "ApplicationItem_ListView_DirectMigration";

        public const string CriteriaViaMinistries =
            "ApplicationType is not null And ApplicationType.ApplicationProgressRoute = ##Enum#"
            + "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,ViaMinistries#";

        public const string CriteriaDirectMigration =
            "ApplicationType is not null And ApplicationType.ApplicationProgressRoute = ##Enum#"
            + "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,DirectToMigrationService#";

        public const string CriteriaItemsViaMinistries =
            "Application is not null And Application.ApplicationType is not null And Application.ApplicationType.ApplicationProgressRoute = ##Enum#"
            + "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,ViaMinistries#";

        public const string CriteriaItemsDirectMigration =
            "Application is not null And Application.ApplicationType is not null And Application.ApplicationType.ApplicationProgressRoute = ##Enum#"
            + "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,DirectToMigrationService#";
    }
}
