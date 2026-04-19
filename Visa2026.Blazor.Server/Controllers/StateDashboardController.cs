using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Visa2026.Blazor.Server.Controllers
{
    public class StateDashboardController : WindowController
    {
        private readonly SimpleAction _action;

        public StateDashboardController()
        {
            _action = new SimpleAction(this, "OpenStateDashboard", PredefinedCategory.View)
            {
                Caption = "State Dashboard",
                ImageName = "Dashboard",
                ToolTip = "Open the Visa State Dashboard"
            };
            _action.Execute += OnExecute;
        }

        private void OnExecute(object sender, SimpleActionExecuteEventArgs e)
        {
            var nav = Application.ServiceProvider?.GetService<NavigationManager>();
            nav?.NavigateTo("/state-dashboard");
        }
    }
}
