using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers
{
    public class ApplicationItemPdfController : ViewController
    {
        private PopupWindowShowAction generatePdfBatchAction;
        private SimpleAction myPdfJobsAction;

        public ApplicationItemPdfController()
        {
            TargetObjectType = typeof(ApplicationItem);
            TargetViewType = ViewType.Any;

            generatePdfBatchAction = new PopupWindowShowAction(this, "GenerateApplicationPdfBatch", "View");
            generatePdfBatchAction.ImageName = "Action_Workflow";
            generatePdfBatchAction.SelectionDependencyType = SelectionDependencyType.Independent;
            generatePdfBatchAction.CustomizePopupWindowParams += GeneratePdfBatchAction_CustomizePopupWindowParams;
            generatePdfBatchAction.Execute += GeneratePdfBatchAction_Execute;

            myPdfJobsAction = new SimpleAction(this, "ShowMyPdfBatches", "View");
            myPdfJobsAction.ImageName = "BO_List";
            myPdfJobsAction.SelectionDependencyType = SelectionDependencyType.Independent;
            myPdfJobsAction.Execute += MyPdfJobsAction_Execute;
        }

        private void GeneratePdfBatchAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var objectSpace = Application.CreateObjectSpace(typeof(PdfBatchEnqueueOptions));
            var opts = objectSpace.CreateObject<PdfBatchEnqueueOptions>();

            var detailView = Application.CreateDetailView(objectSpace, opts, true);
            detailView.ViewEditMode = ViewEditMode.Edit;
            e.View = detailView;
            e.DialogController.SaveOnAccept = false;
            e.DialogController.AcceptAction.Caption = VisaUiMessages.Get("Pdf.QueueJob");
        }

        private void GeneratePdfBatchAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            if (e.PopupWindowView is not DetailView { CurrentObject: PdfBatchEnqueueOptions opts })
                return;

            var selectedItems = View.SelectedObjects?.OfType<ApplicationItem>().ToList()
                ?? new List<ApplicationItem>();

            selectedItems = selectedItems
                .Where(i => i != null && i.Application != null && !i.IsDeleted)
                .Distinct()
                .ToList();

            if (selectedItems.Count < 1)
            {
                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Get("Pdf.SelectAtLeastOneItem"),
                    InformationType.Warning);
                return;
            }

            // Passport ZIP uses ApplicationItem.CurrentPassport only. Snapshot Person.CurrentPassport onto the line
            // at queue time (once) so the worker never reads live Person — line stays frozen after this.
            if (opts.IncludePassportCopies)
            {
                SnapshotCurrentPassportOntoLinesFromPersonIfMissing(selectedItems);
                foreach (var shell in selectedItems)
                    View.ObjectSpace.ReloadObject(shell);
            }

            var keys = selectedItems
                .Select(i => View.ObjectSpace.GetKeyValue(i))
                .Where(k => k != null)
                .ToList();

            if (keys.Count < 1)
                return;

            var keyType = keys.First().GetType();
            var keyStrings = keys.Select(k => Convert.ToString(k, System.Globalization.CultureInfo.InvariantCulture)).ToList();

            int itemsMissingCurrentPassport = 0;
            if (opts.IncludePassportCopies)
            {
                using (var countOs = Application.CreateObjectSpace(typeof(ApplicationItem)))
                {
                    foreach (var shell in selectedItems)
                    {
                        var key = View.ObjectSpace.GetKeyValue(shell);
                        if (key == null)
                            continue;
                        var live = countOs.GetObjectByKey<ApplicationItem>(key);
                        if (live != null && !live.IsDeleted && live.CurrentPassport == null)
                            itemsMissingCurrentPassport++;
                    }
                }
            }

            bool passportZipWillSkip = opts.IncludePassportCopies && itemsMissingCurrentPassport > 0;

            var factory = Application.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using (var os = factory.CreateNonSecuredObjectSpace<PdfGenerationBatch>())
            {
                var batch = os.CreateObject<PdfGenerationBatch>();
                batch.RequestedBy = SecuritySystem.CurrentUserName;
                batch.RequestedCulture = VisaUiMessages.NormalizeCultureName(CultureInfo.CurrentUICulture.Name);
                batch.ItemKeyType = keyType.AssemblyQualifiedName ?? keyType.FullName ?? keyType.Name;
                batch.ItemKeysJson = JsonSerializer.Serialize(keyStrings);
                batch.TotalItems = keyStrings.Count;
                batch.Status = PdfGenerationBatchStatus.Queued;
                opts.CopyTo(batch);
                os.CommitChanges();
            }

            if (passportZipWillSkip)
            {
                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Format("Pdf.QueuedPassportWarning", keyStrings.Count, itemsMissingCurrentPassport),
                    InformationType.Warning);
            }
            else
            {
                Application.ShowViewStrategy.ShowMessage(
                    BuildQueuedSuccessMessage(opts, keyStrings.Count),
                    InformationType.Success);
            }
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

        private static string BuildQueuedSuccessMessage(PdfBatchEnqueueOptions opts, int itemCount)
        {
            var parts = new List<string> { VisaUiMessages.Format("Pdf.QueuedSuccess", itemCount) };
            bool mergeBatchPdfs = opts.SupportingZipMergeOption != PdfSupportingZipMergeOption.IndividualFilesOnly;

            if (opts.SupportingZipMergeOption == PdfSupportingZipMergeOption.MergedPdfSummariesOnly)
            {
                parts.Add(VisaUiMessages.Get("Pdf.QueuedNote.SummariesOnly"));
            }

            if (opts.IncludePassportCopies && mergeBatchPdfs)
            {
                parts.Add(VisaUiMessages.Get("Pdf.QueuedNote.Passport"));
            }

            if (opts.IncludeVisaCopies && mergeBatchPdfs)
            {
                parts.Add(VisaUiMessages.Get("Pdf.QueuedNote.Visa"));
            }

            if (opts.IncludeDiplomaFiles && mergeBatchPdfs)
            {
                string diplomaNote = VisaUiMessages.Get("Pdf.QueuedNote.Diploma");
                if (opts.IncludeMergedDiplomaPdf)
                {
                    diplomaNote += opts.SupportingZipMergeOption == PdfSupportingZipMergeOption.MergedPdfSummariesOnly
                        ? VisaUiMessages.Get("Pdf.QueuedNote.DiplomaMergedByLineSummariesOnly")
                        : VisaUiMessages.Get("Pdf.QueuedNote.DiplomaMergedByLine");
                }

                parts.Add(diplomaNote);
            }

            if (opts.IncludeWorkPermitCopies && mergeBatchPdfs)
            {
                parts.Add(VisaUiMessages.Get("Pdf.QueuedNote.WorkPermit"));
            }

            return string.Join("\r\n\r\n", parts);
        }

        /// <summary>
        /// Persists <see cref="ApplicationItem.CurrentPassport"/> from <see cref="Person.CurrentPassport"/> when the
        /// line link is missing, so PDF/ZIP workers read only the application line (frozen at queue time).
        /// </summary>
        private void SnapshotCurrentPassportOntoLinesFromPersonIfMissing(IEnumerable<ApplicationItem> items)
        {
            using var os = Application.CreateObjectSpace(typeof(ApplicationItem));
            bool changed = false;
            foreach (var shell in items)
            {
                if (shell == null)
                    continue;
                var key = View.ObjectSpace.GetKeyValue(shell);
                if (key == null)
                    continue;
                var item = os.GetObjectByKey<ApplicationItem>(key);
                if (item == null || item.IsDeleted || item.CurrentPassport != null || item.Person == null)
                    continue;
                var person = os.GetObject(item.Person);
                if (person?.CurrentPassport == null)
                    continue;
                item.CurrentPassport = os.GetObject(person.CurrentPassport);
                changed = true;
            }

            if (changed)
                os.CommitChanges();
        }
    }
}
