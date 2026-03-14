using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.ShowReportFrom;
using DevExpress.Persistent.BaseImpl;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.ReportsV2;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Microsoft.Extensions.DependencyInjection;
using DevExpress.XtraGauges.Core.Base;

namespace Visa2026.Module.Controllers
{
    public class ShowReportController : ViewController
    {
        private IReportVisibilityCacheService _reportVisibilityCacheService;

        public ShowReportController()
        {            
             TargetObjectType = typeof(BaseObject);
            ShowReportFromController showReportController = new ShowReportFromController();
            showReportController.CustomizeShowReportAction += ShowReportController_CustomizeShowReportAction;
            Frame.RegisterController(showReportController);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
             _reportVisibilityCacheService = Application.ServiceProvider.GetRequiredService<IReportVisibilityCacheService>();
            if (_reportVisibilityCacheService == null)
            {
                throw new Exception("IReportVisibilityCacheService is not registered in the dependency injection container.");
            }
        }

        private void ShowReportController_CustomizeShowReportAction(object sender, CustomizeShowReportActionEventArgs e)
        {
            if (!(View is DetailView || View is ListView)) return;

            foreach (var item in e.ShowReportAction.Items.ToList())
            {
                if (item is ChoiceActionItem choiceActionItem)
                {
                    var reportName = choiceActionItem.Id;
                    var targetType = View.ObjectTypeInfo.Type;

                    // Retrieve report visibility from cache
                    var reportVisibility = _reportVisibilityCacheService.GetReportVisibility(reportName, targetType);

                    // Check if report visibility is defined and criteria is met
                    if (reportVisibility != null)
                    {
                        if (!string.IsNullOrEmpty(reportVisibility.VisibilityCriteria))
                        {
                            try
                            {
                                // Evaluate visibility criteria
                                var criteria = CriteriaOperator.Parse(reportVisibility.VisibilityCriteria);
                                var isVisible = criteria.Evaluate(View.CurrentObject);
                                if (isVisible is bool visible && !visible)
                                {
                                    e.ShowReportAction.Items.Remove(choiceActionItem);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"Error evaluating visibility criteria for report '{reportName}': {ex.Message}", ex);
                            }
                        }
                    }
                }
            }
        }
    }
}