using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerContextGate
{
    public static void Set(
        XafApplication application,
        ApplicationProfilePickerOpenContext context,
        Frame? sourceFrame = null)
    {
        if (application?.ServiceProvider == null || context == null)
            return;

        if (application.ServiceProvider.GetService(typeof(IApplicationProfilePickerContext))
            is IApplicationProfilePickerContext holder)
        {
            holder.Context = context;
            holder.SourceFrame = sourceFrame;
        }
    }

    public static ApplicationProfilePickerOpenContext? Get(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return null;

        return application.ServiceProvider.GetService(typeof(IApplicationProfilePickerContext))
            is IApplicationProfilePickerContext holder
            ? holder.Context
            : null;
    }

    public static Frame? GetSourceFrame(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return null;

        return application.ServiceProvider.GetService(typeof(IApplicationProfilePickerContext))
            is IApplicationProfilePickerContext holder
            ? holder.SourceFrame
            : null;
    }
}
