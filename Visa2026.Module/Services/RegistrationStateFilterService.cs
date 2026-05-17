using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services
{
    public class RegistrationStateFilterService
    {
        private IReadOnlyList<Guid> _pendingVisaIds;
        private string _pendingCaption;
        private string _pendingStateKey;

        public event Action<IReadOnlyList<Guid>, string, string> CriteriaRequested;

        public void SetPending(IReadOnlyList<Guid> visaIds, string caption, string stateKey)
        {
            _pendingVisaIds = visaIds ?? Array.Empty<Guid>();
            _pendingCaption = caption;
            _pendingStateKey = stateKey;
            CriteriaRequested?.Invoke(_pendingVisaIds, caption, stateKey);
        }

        /// <returns>Null tuple when no registration navigation filter was queued.</returns>
        public (IReadOnlyList<Guid>? VisaIds, string? Caption, string? StateKey)? TakeAndClear()
        {
            if (_pendingVisaIds == null && _pendingCaption == null && _pendingStateKey == null)
                return null;

            var val = (_pendingVisaIds ?? Array.Empty<Guid>(), _pendingCaption, _pendingStateKey);
            _pendingVisaIds = null;
            _pendingCaption = null;
            _pendingStateKey = null;
            return val;
        }
    }
}
