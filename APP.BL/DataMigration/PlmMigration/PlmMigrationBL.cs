namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public const string SessionStatusInProgress = "InProgress";
        public const string SessionStatusCompleted = "Completed";

        public const string JobStatusQueued = "Queued";
        public const string JobStatusRunning = "Running";
        public const string JobStatusCompleted = "Completed";
        public const string JobStatusFailed = "Failed";
        public const string JobStatusCancelled = "Cancelled";

        public const string StepConnect = "Connect";
        public const string StepEntity = "Entity";
        public const string StepTemplate = "Template";
        public const string StepOtherData = "OtherData";

        public const string JobTypePlmTableExport = "PlmTableExport";
        public const string JobTypeSystemDefineEntityImport = "SystemDefineEntityImport";
        public const string JobTypeUserDefineEntityImport = "UserDefineEntityImport";
        public const string JobTypeTemplateImport = "TemplateImport";

        public const string PlmExportIssueMissingSourceTable = "MissingSourceTable";
        public const string PlmExportIssueExportFailed = "ExportFailed";

        public const string PlmExportActionPreview = "PlmTableExportPreview";
        public const string PlmExportActionExport = "PlmTableExport";

        public const string PlmSysDefineActionPreview = "SystemDefineEntityPreview";
        public const string PlmSysDefineActionImport = "SystemDefineEntityImport";

        public const string DefaultTablePrefix = "Plm_";
        public const string EntityWideTableSuffix = "Entity_";
        public const string DefaultEntityWideTablePrefix = DefaultTablePrefix + EntityWideTableSuffix;
    }
}
