using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class ApplicationProgressController : ViewController
    {
        public ApplicationProgressController()
        {
            TargetObjectType = typeof(ApplicationProgress);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            ObjectSpace.Committing += ObjectSpace_Committing;
        }

        protected override void OnDeactivated()
        {
            ObjectSpace.Committing -= ObjectSpace_Committing;
            base.OnDeactivated();
        }

        private void ObjectSpace_Committing(object sender, CancelEventArgs e)
        {
            IObjectSpace objectSpace = (IObjectSpace)sender;
            var modifiedObjects = objectSpace.GetObjectsToSave(true);

            HashSet<Application> applicationsToUpdate = new HashSet<Application>();

            foreach (var obj in modifiedObjects)
            {
                if (obj is ApplicationProgress progress && progress.Application != null)
                {
                    applicationsToUpdate.Add(progress.Application);
                }
            }

            foreach (var app in applicationsToUpdate)
            {
                if (!objectSpace.IsDeletedObject(app))
                {
                    UpdateApplicationState(objectSpace, app);
                }
            }
        }

        private void UpdateApplicationState(IObjectSpace objectSpace, Application app)
        {
            var activeHistory = app.ProgressHistory.Where(p => !objectSpace.IsDeletedObject(p)).ToList();
            var latestProgress = activeHistory.OrderByDescending(p => p.Date).FirstOrDefault();

            if (app.CurrentState != latestProgress)
            {
                app.CurrentState = latestProgress;
                objectSpace.SetModified(app);
            }
        }
    }
}