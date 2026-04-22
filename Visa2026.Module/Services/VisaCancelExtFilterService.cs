namespace Visa2026.Module.Services
{
    public class VisaCancelExtFilterService
    {
        private string _pendingCriteria;
        private string _pendingCaption;

        public event Action<string, string> CriteriaRequested;

        public void SetPending(string criteria, string caption)
        {
            _pendingCriteria = criteria;
            _pendingCaption = caption;
            CriteriaRequested?.Invoke(criteria, caption);
        }

        public (string Criteria, string Caption) TakeAndClear()
        {
            var val = (_pendingCriteria, _pendingCaption);
            _pendingCriteria = null;
            _pendingCaption = null;
            return val;
        }
    }
}
