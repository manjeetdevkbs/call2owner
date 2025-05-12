using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Utilities
{
    public static class Helper
    {
        public static List<string> possibleKeys = new List<string>() { "Email", "emailAddress", "clientemailaddress", "customeremail", "customer_email",
                                                    "InsurerEmail", "BrokerEmail"}; 


        public static List<string> ExtractMatchingValues(string json)
        {
            var result = new List<string>();
            var jObject = JObject.Parse(json);

            foreach (var key in possibleKeys)
            {
                // Try to find the key (case-insensitive match)
                var match = jObject.Properties()
                                   .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

                if (match != null && Convert.ToString(match.Value) != "")
                {
                    result.Add(match.Value?.ToString() ?? string.Empty);
                }
                //else
                //{
                //    result.Add(match.Value?.ToString() ?? string.Empty);
                //}
            }

            return result;
        }
        public static List<string> possibleKeyss = new List<string>
    {
        "Email", "emailAddress", "clientemailaddress", "customeremail", "customer_email",
        "InsurerEmail", "BrokerEmail"
    };

        public static List<string> ExtractMatchingValuess(string json)
        {
            var result = new List<string>();
            var jObject = JObject.Parse(json);
            ExtractKeysRecursive(jObject, result);
            return result;
        }

        private static void ExtractKeysRecursive(JToken token, List<string> result)
        {
            if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    if (possibleKeyss.Any(k => string.Equals(property.Name, k, StringComparison.OrdinalIgnoreCase)))
                    {
                        var value = property.Value?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            result.Add(value);
                        }
                    }

                    // Recurse into children
                    ExtractKeysRecursive(property.Value, result);
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    ExtractKeysRecursive(item, result);
                }
            }
        }
    }
}
