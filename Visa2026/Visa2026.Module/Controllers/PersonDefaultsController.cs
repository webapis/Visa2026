using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // This controller sets the default value for the Person.IsEmployee flag
    // when a new Person is created from within an ApplicationItem.
    public class PersonDefaultsController : ObjectViewController<DetailView, Person>
    {
        protected override void OnActivated()
        {
            base.OnActivated();

            // We are only interested in new objects.
            if (View.ObjectSpace.IsNewObject(View.CurrentObject))
            {
                // The context (the view that opened this one) is passed via the ObjectSpace.Owner.
                // When creating a new object from a related property, the owner is a 'Link' object.
                if (View.ObjectSpace.Owner is Link link && link.Owner is ApplicationItem appItem)
                {
                    // If we have the ApplicationItem and its parent Application, we can set the default.
                    if (appItem.Application != null)
                    {
                        var person = (Person)View.CurrentObject;
                        person.IsEmployee = !appItem.Application.IsForFamily;
                    }
                }
            }
        }
    }
}