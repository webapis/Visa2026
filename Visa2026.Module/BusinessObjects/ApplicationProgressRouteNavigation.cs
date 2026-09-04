namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Navigation and ListView ids for <see cref="Application"/> and <see cref="ApplicationRosterMergeLine"/>
    /// split by <see cref="ApplicationProfileInstanceProgressRouteKind"/>.
    /// </summary>
    public static class ApplicationProfileInstanceProgressRouteNavigation
    {
        public const string NavItemViaMinistries = "Application_ViaMinistries";
        public const string NavItemDirectMigration = "Application_DirectMigration";
        public const string NavItemStaged = "Application_Staged";
        public const string NavItemInProcess = "Application_InProcess";

        public const string CaptionGroup = "Application Profiles";
        public const string CaptionStaged = "Staged profiles";
        public const string CaptionInProcess = "In process";
        public const string CaptionViaMinistries = "Application Profile Instances (via ministry)";
        public const string CaptionDirectMigration = "Application Profile Instances (direct migration)";
        public const string CaptionTemplates = "Application Profile Templates";

        public const string NavItemItemsViaMinistries = "ApplicationItem_ViaMinistries";
        public const string NavItemItemsDirectMigration = "ApplicationItem_DirectMigration";

        public const string ListViewViaMinistries = "Application_ListView_ViaMinistries";
        public const string ListViewDirectMigration = "Application_ListView_DirectMigration";
        public const string ListViewStaged = "Application_ListView_Staged";
        public const string ListViewInProcess = "Application_ListView_InProcess";

        /// <summary>XAF default ListView id after the Application → ApplicationProfileInstance rename.</summary>
        public const string SourceListView = "ApplicationProfileInstance_ListView";

        /// <summary>Pre-rename ListView id; still present in some host model diffs.</summary>
        public const string LegacySourceListView = "Application_ListView";

        public const string ListViewItemsViaMinistries = "ApplicationItem_ListView_ViaMinistries";
        public const string ListViewItemsDirectMigration = "ApplicationItem_ListView_DirectMigration";

        private const string ViaMinistriesEnum =
            "Visa2026.Module.BusinessObjects.ApplicationProfileInstanceProgressRouteKind,ViaMinistries#";

        private const string DirectMigrationEnum =
            "Visa2026.Module.BusinessObjects.ApplicationProfileInstanceProgressRouteKind,DirectToMigrationService#";

        /// <summary>
        /// Profile-first route filter: <see cref="Application.CreationProgressRoute"/>, then profile, then deprecated type.
        /// </summary>
        public const string CriteriaViaMinistries =
            "CreationProgressRoute = ##Enum#" + ViaMinistriesEnum
            + " Or (CreationProgressRoute is null And ApplicationProfile is not null And ApplicationProfile.ProgressRoute = ##Enum#"
            + ViaMinistriesEnum
            + ") Or (CreationProgressRoute is null And ApplicationProfile is null And ApplicationType is not null And ApplicationType.ApplicationProfileInstanceProgressRoute = ##Enum#"
            + ViaMinistriesEnum + ")";

        public const string CriteriaDirectMigration =
            "CreationProgressRoute = ##Enum#" + DirectMigrationEnum
            + " Or (CreationProgressRoute is null And ApplicationProfile is not null And ApplicationProfile.ProgressRoute = ##Enum#"
            + DirectMigrationEnum
            + ") Or (CreationProgressRoute is null And ApplicationProfile is null And ApplicationType is not null And ApplicationType.ApplicationProfileInstanceProgressRoute = ##Enum#"
            + DirectMigrationEnum + ")";

        /// <summary>No process number yet (officer shell staged queue).</summary>
        public const string CriteriaStaged =
            "HasLeftStagedQueue = False And (LatestPrimaryStateCode is null Or LatestPrimaryStateCode = 'OFFICE_PREPARATION' Or LatestPrimaryStateCode = 'DRAFT')";

        /// <summary>Started cases (officer shell in-process queue), including those waiting for a process number.</summary>
        public const string CriteriaInProcess =
            "Not (HasLeftStagedQueue = False And (LatestPrimaryStateCode is null Or LatestPrimaryStateCode = 'OFFICE_PREPARATION' Or LatestPrimaryStateCode = 'DRAFT'))";

        public const string CriteriaItemsViaMinistries =
            "ApplicationProfileInstance is not null And ("
            + "Application.CreationProgressRoute = ##Enum#" + ViaMinistriesEnum
            + " Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is not null And Application.ApplicationProfile.ProgressRoute = ##Enum#"
            + ViaMinistriesEnum
            + ") Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is null And Application.ApplicationType is not null And Application.ApplicationType.ApplicationProfileInstanceProgressRoute = ##Enum#"
            + ViaMinistriesEnum + "))";

        public const string CriteriaItemsDirectMigration =
            "ApplicationProfileInstance is not null And ("
            + "Application.CreationProgressRoute = ##Enum#" + DirectMigrationEnum
            + " Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is not null And Application.ApplicationProfile.ProgressRoute = ##Enum#"
            + DirectMigrationEnum
            + ") Or (Application.CreationProgressRoute is null And Application.ApplicationProfile is null And Application.ApplicationType is not null And Application.ApplicationType.ApplicationProfileInstanceProgressRoute = ##Enum#"
            + DirectMigrationEnum + "))";
    }
}
