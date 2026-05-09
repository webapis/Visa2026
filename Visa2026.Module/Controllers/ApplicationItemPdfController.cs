using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DevExpress.ExpressApp.Security;

namespace Visa2026.Module.Controllers
{
    public class ApplicationItemPdfController : ViewController
    {
        private SimpleAction generatePdfBatchAction;
        private SimpleAction myPdfJobsAction;

        public ApplicationItemPdfController()
        {
            TargetObjectType = typeof(ApplicationItem);
            TargetViewType = ViewType.Any;

            generatePdfBatchAction = new SimpleAction(this, "GenerateApplicationPdfBatch", "View");
            generatePdfBatchAction.Caption = "Generate PDF";
            generatePdfBatchAction.ImageName = "Action_Workflow";
            // One or more ApplicationItem rows — not RequireMultipleObjects (that needs 2+).
            generatePdfBatchAction.SelectionDependencyType = SelectionDependencyType.Independent;
            generatePdfBatchAction.Execute += GeneratePdfBatchAction_Execute;

            myPdfJobsAction = new SimpleAction(this, "ShowMyPdfBatches", "View");
            myPdfJobsAction.Caption = "My PDF Jobs";
            myPdfJobsAction.ImageName = "BO_List";
            myPdfJobsAction.SelectionDependencyType = SelectionDependencyType.Independent;
            myPdfJobsAction.Execute += MyPdfJobsAction_Execute;
        }

        private void GeneratePdfBatchAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var selectedItems = e.SelectedObjects?.OfType<ApplicationItem>().ToList()
                ?? new List<ApplicationItem>();

            selectedItems = selectedItems
                .Where(i => i != null && i.Application != null && !i.IsDeleted)
                .Distinct()
                .ToList();

            if (selectedItems.Count < 1)
            {
                Application.ShowViewStrategy.ShowMessage(
                    "Select at least one application item to run background PDF generation.",
                    InformationType.Warning);
                return;
            }

            var keys = selectedItems
                .Select(i => View.ObjectSpace.GetKeyValue(i))
                .Where(k => k != null)
                .ToList();

            if (keys.Count < 1)
                return;

            var keyType = keys.First().GetType();
            var keyStrings = keys.Select(k => Convert.ToString(k, System.Globalization.CultureInfo.InvariantCulture)).ToList();

            // Secured ObjectSpace denies Create on PdfGenerationBatch for typical roles; the worker uses a non-secured space.
            var factory = Application.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using (var os = factory.CreateNonSecuredObjectSpace<PdfGenerationBatch>())
            {
                var batch = os.CreateObject<PdfGenerationBatch>();
                batch.RequestedBy = SecuritySystem.CurrentUserName;
                batch.ItemKeyType = keyType.AssemblyQualifiedName ?? keyType.FullName ?? keyType.Name;
                batch.ItemKeysJson = JsonSerializer.Serialize(keyStrings);
                batch.TotalItems = keyStrings.Count;
                batch.Status = PdfGenerationBatchStatus.Queued;
                os.CommitChanges();
            }

            Application.ShowViewStrategy.ShowMessage(
                $"PDF generation queued for {keyStrings.Count} item(s). Use \"My PDF Jobs\" to track progress.",
                InformationType.Success);
        }

        private void MyPdfJobsAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var os = Application.CreateObjectSpace(typeof(PdfGenerationBatch));
            var listView = Application.CreateListView(os, typeof(PdfGenerationBatch), true);
            listView.CollectionSource.Criteria["RequestedBy"] = new DevExpress.Data.Filtering.BinaryOperator(
                nameof(PdfGenerationBatch.RequestedBy),
                SecuritySystem.CurrentUserName);

            var svp = new ShowViewParameters(listView)
            {
                TargetWindow = TargetWindow.NewModalWindow
            };
            Application.ShowViewStrategy.ShowView(svp, new ShowViewSource(Frame, null));
        }

    }
}