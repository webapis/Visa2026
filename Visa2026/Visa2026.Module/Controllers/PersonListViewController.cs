using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

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
                newObjectController.ObjectCreating += OnObjectCreating;
            }
        }

        private void OnObjectCreating(object sender, ObjectCreatingEventArgs e)
        {
            if (e.ObjectType == typeof(Person) && View != null)
            {
                var person = (Person)e.NewObject;
                if (View.Id == "Person_ListView_Employees")
                {
                    person.IsEmployee = true;
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
                newObjectController.ObjectCreating -= OnObjectCreating;
            }
            base.OnDeactivated();
        }
    }
}