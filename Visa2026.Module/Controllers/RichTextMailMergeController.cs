using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using System;

namespace Visa2026.Module.Controllers
{
    // This controller ensures that when a new RichTextMailMergeData object is created
    // within the Application's "MailMergeTemplates" list, its DataType is automatically
    // set to typeof(Application), enabling the Field List and Mail Merge features.
    public class ApplicationMailMergeTemplatesController : ViewController<ListView>
    {
        public ApplicationMailMergeTemplatesController()
        {
            // Target the specific nested List View for Application.MailMergeTemplates
            TargetViewId = "Application_MailMergeTemplates_ListView";
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            NewObjectViewController newObjectController = Frame.GetController<NewObjectViewController>();
            if (newObjectController != null)
            {
                newObjectController.ObjectCreated += NewObjectController_ObjectCreated;
            }
        }

        protected override void OnDeactivated()
        {
            NewObjectViewController newObjectController = Frame.GetController<NewObjectViewController>();
            if (newObjectController != null)
            {
                newObjectController.ObjectCreated -= NewObjectController_ObjectCreated;
            }
            base.OnDeactivated();
        }

        private void NewObjectController_ObjectCreated(object sender, ObjectCreatedEventArgs e)
        {
            if (e.CreatedObject is RichTextMailMergeData mailMergeData)
            {
                // Automatically set the DataType to Application so the Field List appears
                mailMergeData.DataType = typeof(Application);
                mailMergeData.Name = "New Template";
            }
        }
    }
}