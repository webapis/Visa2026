using System.Collections.Generic;

namespace Visa2026.Module.BusinessObjects
{
    public interface IPersonLinkParent
    {
        ApplicationProfileInstance ApplicationProfileInstance { get; }
        IList<Person> AvailablePeople { get; }
    }
}