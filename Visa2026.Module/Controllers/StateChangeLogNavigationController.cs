using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class StateChangeLogNavigationController : ViewController
    {
        public StateChangeLogNavigationController()
        {
            TargetObjectType = typeof(StateChangeLog);

            var openSourceAction = new SimpleAction(this, "OpenSourceObject", PredefinedCategory.View);
            openSourceAction.Caption = "Open Source Object";
            openSourceAction.ToolTip = "Navigate to the object that triggered this log entry.";
            openSourceAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
            openSourceAction.ImageName = "Action_Link"; 
            openSourceAction.Execute += OpenSourceAction_Execute;

            var openTargetAction = new SimpleAction(this, "OpenTargetObject", PredefinedCategory.View);
            openTargetAction.Caption = "Open Target Object";
            openTargetAction.ToolTip = "Navigate to the object affected by this log entry.";
            openTargetAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
            openTargetAction.ImageName = "Action_Link";
            openTargetAction.Execute += OpenTargetAction_Execute;
        }

        private void OpenSourceAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var log = (StateChangeLog)e.CurrentObject;
            if (log.SourceBoType == null || string.IsNullOrEmpty(log.SourceObjectId))
            {
                Application.ShowViewStrategy.ShowMessage("No source object information available.", InformationType.Warning);
                return;
            }

            // Create an ObjectSpace for the target type
            IObjectSpace os = Application.CreateObjectSpace(log.SourceBoType);
            object objectKey = null;

            // Determine the Key Type of the source object to parse the string ID correctly
            ITypeInfo typeInfo = Application.TypesInfo.FindTypeInfo(log.SourceBoType);
            if (typeInfo.KeyMember != null)
            {
                try
                {
                    if (typeInfo.KeyMember.MemberType == typeof(Guid))
                        objectKey = Guid.Parse(log.SourceObjectId);
                    else if (typeInfo.KeyMember.MemberType == typeof(int))
                        objectKey = int.Parse(log.SourceObjectId);
                    else
                        objectKey = log.SourceObjectId;
                }
                catch
                {
                    Application.ShowViewStrategy.ShowMessage("Error parsing object ID.", InformationType.Error);
                    return;
                }
            }

            var obj = os.GetObjectByKey(log.SourceBoType, objectKey);
            if (obj != null)
            {
                var detailView = Application.CreateDetailView(os, obj);
                e.ShowViewParameters.CreatedView = detailView;
            }
            else
            {
                Application.ShowViewStrategy.ShowMessage("The source object cannot be found (it may have been deleted).", InformationType.Warning);
            }
        }

        private void OpenTargetAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var log = (StateChangeLog)e.CurrentObject;
            if (log.TargetBoType == null || string.IsNullOrEmpty(log.TargetObjectId))
            {
                Application.ShowViewStrategy.ShowMessage("No target object information available.", InformationType.Warning);
                return;
            }

            // Create an ObjectSpace for the target type
            IObjectSpace os = Application.CreateObjectSpace(log.TargetBoType);
            object objectKey = null;

            // Determine the Key Type of the target object to parse the string ID correctly
            ITypeInfo typeInfo = Application.TypesInfo.FindTypeInfo(log.TargetBoType);
            if (typeInfo.KeyMember != null)
            {
                try
                {
                    if (typeInfo.KeyMember.MemberType == typeof(Guid))
                        objectKey = Guid.Parse(log.TargetObjectId);
                    else if (typeInfo.KeyMember.MemberType == typeof(int))
                        objectKey = int.Parse(log.TargetObjectId);
                    else
                        objectKey = log.TargetObjectId;
                }
                catch
                {
                    Application.ShowViewStrategy.ShowMessage("Error parsing object ID.", InformationType.Error);
                    return;
                }
            }

            var obj = os.GetObjectByKey(log.TargetBoType, objectKey);
            if (obj != null)
            {
                var detailView = Application.CreateDetailView(os, obj);
                e.ShowViewParameters.CreatedView = detailView;
            }
            else
            {
                Application.ShowViewStrategy.ShowMessage("The target object cannot be found (it may have been deleted).", InformationType.Warning);
            }
        }
    }
}