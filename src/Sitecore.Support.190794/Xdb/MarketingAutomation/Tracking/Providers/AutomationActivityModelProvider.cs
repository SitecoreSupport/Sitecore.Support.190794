namespace Sitecore.Support.Xdb.MarketingAutomation.Tracking.Providers
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using Sitecore.Framework.Conditions;
  using Sitecore.Marketing.Definitions;
  using Sitecore.Marketing.Definitions.AutomationPlans.Model;
  using Sitecore.Xdb.MarketingAutomation.Locators.AutomationPlans;
  using Sitecore.Xdb.MarketingAutomation.Tracking.Filters;
  using Sitecore.Xdb.MarketingAutomation.Tracking.Models;
  using Sitecore.Xdb.MarketingAutomation.Tracking.Providers;

  public class AutomationActivityModelProvider : IAutomationActivityModelProvider
  {
    private readonly IDefinitionManager<IAutomationPlanDefinition> _automationPlanDefinitionManager;

    private readonly IActivityDescriptorLocator _activityDescriptorLocator;

    public AutomationActivityModelProvider(
        IDefinitionManager<IAutomationPlanDefinition> automationPlanDefinitionManager,
        IActivityDescriptorLocator activityDescriptorLocator)
    {

      Condition
          .Requires(automationPlanDefinitionManager, nameof(automationPlanDefinitionManager))
          .IsNotNull();
      Condition
          .Requires(activityDescriptorLocator, nameof(activityDescriptorLocator))
          .IsNotNull();

      _automationPlanDefinitionManager = automationPlanDefinitionManager;
      _activityDescriptorLocator = activityDescriptorLocator;
    }


    public IEnumerable<AutomationActivityModel> GetPlans(
        CultureInfo cultureInfo)
    {
      Condition.Requires(cultureInfo, nameof(cultureInfo)).IsNotNull();

      return _automationPlanDefinitionManager
          .GetAll(cultureInfo, new RetrievalParameters<IAutomationPlanDefinition, string>(null, 1, int.MaxValue))
          .Select(result => result.Data)
          .Select(p => new AutomationActivityModel(p.Name, FormattableString.Invariant($"{p.Name} - {p.Id}")));
    }

    #region Modified Code

    public IEnumerable<AutomationActivityModel> GetActivities(
        CultureInfo cultureInfo,
        ActivityFilter activityFilter)
    {
      Condition.Requires(cultureInfo, nameof(cultureInfo)).IsNotNull();

      var automationPlanDefinitions = _automationPlanDefinitionManager
          .GetAll(cultureInfo, new RetrievalParameters<IAutomationPlanDefinition, string>(null, 1, int.MaxValue))
          .Select(result => result.Data);

      foreach (IAutomationPlanDefinition automationPlanDefinition in automationPlanDefinitions)
      {
        var automationActivityDefinitions = automationPlanDefinition.GetActivities();

        foreach (IAutomationActivityDefinition automationActivityDefinition in automationActivityDefinitions)
        {
          var automationActivityDescriptor = _activityDescriptorLocator
              .GetDescriptor(automationActivityDefinition.ActivityTypeId, cultureInfo);

          if (activityFilter.Validate(automationActivityDescriptor))
          {
            // Sitecore Support fix #190794
            string goalName =
            (!String.IsNullOrEmpty(automationActivityDefinition.Parameters["Name"].ToString()))
                ? automationActivityDefinition.Parameters["Name"].ToString()
                : automationActivityDescriptor.Name;
            // Sitecore Support fix #190794
            yield return new AutomationActivityModel(
                FormattableString.Invariant($"{automationPlanDefinition.Name} > {goalName}"),
                FormattableString.Invariant($"{automationPlanDefinition.Name} - {automationPlanDefinition.Id} - {automationActivityDefinition.Id}"));
          }
        }
      }
    }
    
    #endregion

  }
}