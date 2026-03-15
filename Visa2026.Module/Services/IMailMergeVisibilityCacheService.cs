using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Module_Interface
{
    public interface IMailMergeVisibilityCacheService
    {
        IEnumerable<MailMergeVisibility> GetVisibilityRules(string templateName, Type targetType);
        void ClearCache();
    }
}