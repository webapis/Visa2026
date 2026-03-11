using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    // This controller ensures the PdfFormMapping cache is automatically refreshed
    // whenever a PdfFormMapping is created, modified, or deleted.
    public class PdfFormMappingCacheController : WindowController
    {
        private bool _cacheNeedsInvalidation = false;

        protected override void OnActivated()
        {
            base.OnActivated();
            Application.ObjectSpaceCreated += Application_ObjectSpaceCreated;
        }

        protected override void OnDeactivated()
        {
            Application.ObjectSpaceCreated -= Application_ObjectSpaceCreated;
            base.OnDeactivated();
        }

        private void Application_ObjectSpaceCreated(object sender, ObjectSpaceCreatedEventArgs e)
        {
            SubscribeToObjectSpaceEvents(e.ObjectSpace);
        }

        private void SubscribeToObjectSpaceEvents(IObjectSpace objectSpace)
        {
            objectSpace.Committing += ObjectSpace_Committing;
            objectSpace.Committed += ObjectSpace_Committed;
            objectSpace.Disposed += ObjectSpace_Disposed;
        }

        private void UnsubscribeFromObjectSpaceEvents(IObjectSpace objectSpace)
        {
            objectSpace.Committing -= ObjectSpace_Committing;
            objectSpace.Committed -= ObjectSpace_Committed;
            objectSpace.Disposed -= ObjectSpace_Disposed;
        }

        private void ObjectSpace_Committing(object sender, CancelEventArgs e)
        {
            var objectSpace = (IObjectSpace)sender;
            if (objectSpace.GetObjectsToSave(false).OfType<PdfFormMapping>().Any() ||
                objectSpace.GetObjectsToDelete(false).OfType<PdfFormMapping>().Any())
            {
                _cacheNeedsInvalidation = true;
            }
        }

        private void ObjectSpace_Committed(object sender, EventArgs e)
        {
            if (_cacheNeedsInvalidation)
            {
                PdfMappingHelper.RefreshMappingCache((IObjectSpace)sender);
                _cacheNeedsInvalidation = false;
            }
        }

        private void ObjectSpace_Disposed(object sender, EventArgs e) => UnsubscribeFromObjectSpaceEvents((IObjectSpace)sender);
    }
}