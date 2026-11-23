using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Copy of a record draft's editable values, so Cancel in the record detail popup can put the draft back
/// the way it was. List properties (copies, order documents) are restored item by item.
/// </summary>
public sealed class PersonIntakeDraftSnapshot
{
    private readonly object _draft;
    private readonly List<(PropertyInfo Property, object Value)> _values = new();
    private readonly List<(IList List, object[] Items)> _lists = new();

    private PersonIntakeDraftSnapshot(object draft)
    {
        _draft = draft;
        foreach (var property in draft.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            if (typeof(IList).IsAssignableFrom(property.PropertyType) && !property.PropertyType.IsArray)
            {
                if (property.GetValue(draft) is IList list)
                    _lists.Add((list, list.Cast<object>().ToArray()));
                continue;
            }

            if (property.CanWrite && property.SetMethod?.IsPublic == true)
                _values.Add((property, property.GetValue(draft)));
        }
    }

    public static PersonIntakeDraftSnapshot Take(object draft) =>
        new(draft ?? throw new ArgumentNullException(nameof(draft)));

    public bool IsFor(object draft) => ReferenceEquals(_draft, draft);

    public void Restore()
    {
        foreach (var (property, value) in _values)
            property.SetValue(_draft, value);

        foreach (var (list, items) in _lists)
        {
            list.Clear();
            foreach (var item in items)
                list.Add(item);
        }
    }
}
