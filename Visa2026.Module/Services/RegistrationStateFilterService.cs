using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services
{
    public class RegistrationStateFilterService
    {
        private IReadOnlyList<Guid> _pendingPersonIds;
        private string _pendingCaption;

        public event Action<IReadOnlyList<Guid>, string> CriteriaRequested;

        public void SetPending(IReadOnlyList<Guid> personIds, string caption)
        {
            _pendingPersonIds = personIds ?? Array.Empty<Guid>();
            _pendingCaption = caption;
            CriteriaRequested?.Invoke(_pendingPersonIds, caption);
        }

        public (IReadOnlyList<Guid> PersonIds, string Caption) TakeAndClear()
        {
            var val = (_pendingPersonIds ?? Array.Empty<Guid>(), _pendingCaption);
            _pendingPersonIds = null;
            _pendingCaption = null;
            return val;
        }
    }
}
