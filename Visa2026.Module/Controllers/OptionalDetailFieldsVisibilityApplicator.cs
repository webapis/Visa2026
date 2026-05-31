using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

public static class OptionalDetailFieldsVisibilityApplicator
{
    public static void Apply(DetailView view)
    {
        if (view?.CurrentObject is not IOptionalDetailFields optionalFields)
        {
            return;
        }

        HashSet<string> optionalMemberNames = OptionalDetailFieldsMetadata
            .GetOptionalMemberNames(view.ObjectTypeInfo)
            .ToHashSet(StringComparer.Ordinal);

        bool showOptional = optionalFields.ShowOptionalFields;
        foreach (ViewItem item in view.GetItems<ViewItem>())
        {
            if (item is not PropertyEditor editor)
            {
                if (item is IAppearanceVisibility layoutVisibility
                    && optionalMemberNames.Contains(item.Id))
                {
                    layoutVisibility.Visibility = showOptional
                        ? ViewItemVisibility.Show
                        : ViewItemVisibility.Hide;
                }

                continue;
            }

            string propertyName = editor.PropertyName ?? editor.MemberInfo?.Name;
            if (string.IsNullOrEmpty(propertyName)
                || !optionalMemberNames.Contains(propertyName))
            {
                continue;
            }

            if (editor is IAppearanceVisibility visibility)
            {
                visibility.Visibility = showOptional
                    ? ViewItemVisibility.Show
                    : ViewItemVisibility.Hide;
            }
        }
    }
}
