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

        private const string ViaMinistriesEnum =
            "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,ViaMinistries#";

        private const string DirectMigrationEnum =
            "Visa2026.Module.BusinessObjects.ApplicationProgressRouteKind,DirectToMigrationService#";

        /// <summary>
        /// Profile-first route filter: <see cref="Application.CreationProgressRoute"/>, then profile, then deprecated type.
        /// </summary>
        public const string CriteriaViaMinistries =
            "CreationProgressRoute = ##Enum#" + ViaMinistriesEnum
            + " Or (CreationProgressRoute is null And ApplicationProfile is not null And ApplicationProfile.ProgressRoute = ##Enum#"
            + ViaMinistriesEnum
            + ") Or (CreationProgressRoute is null And ApplicationProfile is null And ApplicationType is not null And ApplicationType.ApplicationProgressRoute = ##Enum#"
            + ViaMinistriesEnum + ")";

        public const string CriteriaDirectMigration =
            "CreationProgressRoute = ##Enum#" + DirectMigrationEnum
            + " Or (CreationProgressRoute is null And ApplicationProfile is not null And ApplicationProfile.ProgressRoute = ##Enum#"
            + DirectMigrationEnum
            + ") Or (CreationProgressRoute is null And ApplicationProfile is null And ApplicationType is not null And ApplicationType.ApplicationProgressRoute = ##Enum#"
            + DirectMigrationEnum + ")";

        public const string CriteriaItemsViaMinistries =
            "Application is not null And ("
            + "Application.CreationProgressRoute = ##Enum#" + ViaMinistriesEnum
            + " Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is not null And Application.ApplicationProfile.ProgressRoute = ##Enum#"
            + ViaMinistriesEnum
            + ") Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is null And Application.ApplicationType is not null And Application.ApplicationType.ApplicationProgressRoute = ##Enum#"
            + ViaMinistriesEnum + "))";

        public const string CriteriaItemsDirectMigration =
            "Application is not null And ("
            + "Application.CreationProgressRoute = ##Enum#" + DirectMigrationEnum
            + " Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is not null And Application.ApplicationProfile.ProgressRoute = ##Enum#"
            + DirectMigrationEnum
            + ") Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is null And Application.ApplicationType is not null And Application.ApplicationType.ApplicationProgressRoute = ##Enum#"
            + DirectMigrationEnum + "))";
    }
}
