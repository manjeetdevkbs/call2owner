using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class PolicyStatus
    {
        public const string NewBusiness = "New Business";
        public const string Active = "Active";
        public const string Expired = "Expired";
        public const string Renewal = "Renewal";
        public const string Endorsed = "Endorsed";
        public const string Cancelled = "Cancelled";

        // Active/Valid Statuses
        //public const string Active = "Active";
        //public const string Pending = "Pending";
        //public const string Endorsed = "Endorsed";
        //public const string Renewed = "Renewed";

        //// Inactive/Invalid Statuses
        //public const string Lapsed = "Lapsed";
        //public const string Cancelled = "Cancelled";
        //public const string Expired = "Expired";
        //public const string Suspended = "Suspended";
        //public const string Reinstated = "Reinstated";

        //// Life Insurance-Specific
        //public const string PaidUp = "Paid-Up";
        //public const string Surrendered = "Surrendered";

        //// Claim-Related
        //public const string InClaim = "In Claim";
    }
}
