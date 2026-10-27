using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects.Email;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Ensures the email document copies tables exist (PostgreSQL).</summary>
public sealed class EmailDocumentCopiesSchemaUpdater : ModuleUpdater
{
    public EmailDocumentCopiesSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ApplySchemaSql();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplySchemaSql();
        EnsureDefaultTemplate();
        _ = EmailSmtpSettings.GetOrCreateInstance(ObjectSpace);
        ObjectSpace.CommitChanges();
    }

    private void EnsureDefaultTemplate()
    {
        if (ObjectSpace.GetObjectsQuery<EmailTemplate>().Any())
            return;

        var template = ObjectSpace.CreateObject<EmailTemplate>();
        template.Name = "Document copies";
        template.Subject = "Document copies - {PersonNames}";
        template.Body = "Dear colleagues,\n\nPlease find attached copies of the following documents for {PersonCount} person(s):\n\n{DocumentList}\n\nKind regards,\n{OfficerName}";
        template.IsDefault = true;
        ObjectSpace.CommitChanges();
    }

    private void ApplySchemaSql()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(EmailDocumentCopiesSchemaSql.EnsureTablesPostgres, false);
    }
}
