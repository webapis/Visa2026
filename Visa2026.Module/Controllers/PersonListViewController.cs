using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    public class PersonListViewController : ViewController<ListView>
    {
        public PersonListViewController()
        {
            TargetObjectType = typeof(Person);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            var newObjectController = Frame.GetController<NewObjectViewController>();
            if (newObjectController != null)
            {
                newObjectController.ObjectCreated += OnObjectCreated;
            }
        }

        private void OnObjectCreated(object sender, ObjectCreatedEventArgs e)
        {
            if (e.CreatedObject is Person person && View != null)
            {
                if (View.Id == "Person_ListView_Employees")
                {
                    person.IsEmployee = true;
                    VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);
                }
                else if (View.Id == "Person_ListView_FamilyMembers")
                {
                    person.IsEmployee = false;
                }
            }
        }

        protected override void OnDeactivated()
        {
            var newObjectController = Frame.GetController<NewObjectViewController>();
            if (newObjectController != null)
            {
                newObjectController.ObjectCreated -= OnObjectCreated;
            }
            base.OnDeactivated();
        }
    }
}