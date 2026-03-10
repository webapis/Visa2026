using System.Collections.Generic;

namespace Visa2026.Module.BusinessObjects
{
    public interface IPersonLinkParent
    {
        Application Application { get; }
        IList<Person> AvailablePeople { get; }
    }
}