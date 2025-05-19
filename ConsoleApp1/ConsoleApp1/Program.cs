using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class HelloWorld
{
    public static void Main(string[] args)
    {
        var setting = new EmailEventSetting
        {
            ModuleName = "policy",
            Activity = "Expiration",
            ModuleId = 1
        };

        // Anonymous object with a CompletionDate
        var anonymousPolicy = new
        {
            Id = 1,
            CompletionDate = new DateTime(2025, 12, 31)
        };

        var result = CalculateActivityDate(setting, anonymousPolicy);


        DateTime? actionDate = CalculateActionDate(
                    moduleName: "policy",
                    DateTime.Today,
                    activityName: "Expiration",
                    eventValue: "before",
                    period: "10"
                );

        Console.WriteLine(actionDate);
    }

    public static DateTime? CalculateActionDate(string moduleName, DateTime activityDate, string activityName, string eventValue, string period)
    {
        if (!int.TryParse(period, out int days))
        {
            return null;
        }

        DateTime actionDate;

        switch (eventValue?.ToLower())
        {
            case "before":
                actionDate = activityDate.AddDays(-days);
                break;
            case "after":
                actionDate = activityDate.AddDays(days);
                break;
            case "on":
                actionDate = activityDate;
                break;
            default:
                Console.WriteLine("Invalid event value. Must be 'before', 'after', or 'on'.");
                return null;
        }

        return actionDate;
    }

    public static object? CalculateActivityDate(EmailEventSetting setting, object moduleObject)
    {
        object property = null;

        var activityDaateLabel = "";

        if (setting == null || moduleObject == null)
            return null;

        switch (setting.ModuleName?.ToLower())
        {
            case "policy":
                // Match the module ID to object's "Id" property
                if (setting.Activity?.ToLower() == "expiration")
                {
                    activityDaateLabel = "CompletionDate";
                }

                var idProperty = moduleObject.GetType().GetProperty(activityDaateLabel);

                if (idProperty == null) return null;

                 property = (DateTime?)idProperty.GetValue(moduleObject);
                //if (moduleId != setting.ModuleId) return null;

                //// Determine expected date property name from activity
                //string expectedProperty = setting.Activity?.ToLower() switch
                //{
                //    "expiration" => "CompletionDate",
                //    // Add more activity-to-property mappings here
                //    _ => null
                //};

                //if (expectedProperty == null) return null;

                //var dateProperty = moduleObject.GetType().GetProperty(expectedProperty);
                //if (dateProperty != null && dateProperty.PropertyType == typeof(DateTime))
                //{
                //    return (DateTime?)dateProperty.GetValue(moduleObject);
                //}

                break;

            default:
                return property;
        }

        return property;
    }

}

public class EmailEventSetting
{
    public int Id { get; set; }
    public string ModuleName { get; set; }
    public int ModuleId { get; set; }
    public string Activity { get; set; }
    public int Period { get; set; }
    public string Event { get; set; }
    public int TemplateId { get; set; }
    public string TemplateName { get; set; }
}

public class Policy
{
    public int Id { get; set; }
    public DateTime CompletionDate { get; set; }
}
