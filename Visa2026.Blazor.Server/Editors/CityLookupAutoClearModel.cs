using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Editors;

public class CityLookupAutoClearModel : ComponentModelBase {
    public IEnumerable<City> Data {
        get => GetPropertyValue<IEnumerable<City>>();
        set => SetPropertyValue(value);
    }

    public City Value {
        get => GetPropertyValue<City>();
        set => SetPropertyValue(value);
    }

    public EventCallback<City> ValueChanged {
        get => GetPropertyValue<EventCallback<City>>();
        set => SetPropertyValue(value);
    }

    public Expression<Func<City>> ValueExpression {
        get => GetPropertyValue<Expression<Func<City>>>();
        set => SetPropertyValue(value);
    }

    public string Text {
        get => GetPropertyValue<string>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> TextChanged {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public bool ReadOnly {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public override Type ComponentType => typeof(CityLookupAutoClearComponent);
}

