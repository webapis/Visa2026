﻿﻿﻿using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Blazor.Server.Components;

namespace Visa2026.Blazor.Server.Editors
{
    // This attribute registers the View Item so it appears in the Model Editor
    [ViewItem(typeof(IModelViewItem))]
    public class VersionViewItem : ViewItem
    {
        // We use the (Type, string) constructor to avoid overload resolution issues.
        // model.Id ensures we bind to the correct ID defined in the Model Editor.
        public VersionViewItem(IModelViewItem model, Type objectType) : base(objectType, model?.Id) { }

        protected override object CreateControlCore()
        {
            return new VersionViewItemAdapter();
        }
    }

    public class VersionViewItemAdapter : IComponentAdapter
    {
        // IComponentAdapter requires implementing IComponentModelHolder.
        // Since we render a raw Razor component without a DxModel wrapper, we can return null here.
        public IComponentModel ComponentModel => null;

        public RenderFragment ComponentContent => builder =>
        {
            // Renders the Razor Component we created
            builder.OpenComponent<VersionDisplayComponent>(0);
            builder.CloseComponent();
        };

        public object GetValue()
        {
            return null;
        }

        public void SetValue(object value) { }

        public event EventHandler ValueChanged;
    }
}