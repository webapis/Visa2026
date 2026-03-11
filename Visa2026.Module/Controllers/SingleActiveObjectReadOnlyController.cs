using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class SingleActiveObjectReadOnlyController : ViewController<DetailView>
    {
        private SimpleAction reactivateAction;

        public SingleActiveObjectReadOnlyController()
        {
            TargetObjectType = typeof(BaseObject);

            reactivateAction = new SimpleAction(this, "ReactivateObject", PredefinedCategory.View);
            reactivateAction.Caption = "Reactivate";
            reactivateAction.ConfirmationMessage = "Are you sure you want to reactivate this record? This will deactivate the currently active record.";
            reactivateAction.ImageName = "Action_Grant";
            reactivateAction.TargetObjectsCriteria = "!IsActive";
            reactivateAction.Execute += ReactivateAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            // Check if the current object type inherits from SingleActiveBaseObject<,>
            if (!IsSingleActiveObject(View.ObjectTypeInfo.Type))
            {
                Active["IsSingleActiveObject"] = false;
                return;
            }

            ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
            UpdateReadOnlyState();
        }

        protected override void OnDeactivated()
        {
            ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
            base.OnDeactivated();
        }

        private bool IsSingleActiveObject(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SingleActiveBaseObject<,>))
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        private void ReactivateAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var isActiveMember = View.ObjectTypeInfo.FindMember("IsActive");
            if (View.CurrentObject != null && isActiveMember != null)
            {
                isActiveMember.SetValue(View.CurrentObject, true);
                // The ObjectChanged event will fire automatically, triggering UpdateReadOnlyState
            }
        }

        private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
        {
            if (e.Object == View.CurrentObject && e.PropertyName == "IsActive")
            {
                UpdateReadOnlyState();
            }
        }

        private void UpdateReadOnlyState()
        {
            if (View.CurrentObject == null) return;

            var isActiveMember = View.ObjectTypeInfo.FindMember("IsActive");
            if (isActiveMember == null) return;

            bool isActive = (bool)isActiveMember.GetValue(View.CurrentObject);

            foreach (var item in View.GetItems<PropertyEditor>())
            {
                item.AllowEdit["InactiveRecord"] = isActive;
            }
        }
    }
}