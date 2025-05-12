using System.Security.Claims;

namespace Utilities
{
    public class ModulePermissionData
    {
        public int ModuleId { get; set; }
        public List<PermissionData> Permissions { get; set; } = new();
    }

    public class PermissionData
    {
        public int PermissionId { get; set; }
    }

    public class UserRoles
    {
        public const string SuperAdmin = "301";
        public const string OverWriter = "302";
        public const string UnderWriter = "303";
        public const string MasterBroker = "304";
        public const string Broker = "305";
        public const string ClaimUser = "306";
        public const string FinanceUser = "307";
        public const string InsurerAdmin = "308";
        public const string InsurerCustomer = "309";
        public const string Customer = "311";
        public const string CLAIMS_INITITATOR = "312";
        public const string CLAIMS_ASSESSOR = "313";
        public const string CLAIMS_PROCESSOR = "314";
        public const string CLAIMS_MANAGER = "315";
        public const string FINANCE_CLERK = "316";
        public const string FINANCE_MANAGER = "317";

    }

    public class Module
    {
        public const string User = "101";
        public const string Quotation = "102";
        public const string Claim = "103";
        public const string Product = "104";
        public const string Customers = "105";
        public const string Policy = "106";
        public const string Insurer = "107";
        public const string Auth = "108";
        public const string Roleclaims = "109";
        public const string Test = "110";
        public const string Client = "113";
    }

    public class Permission
    {
        public const string Add = "11";
        public const string Update = "12";
        public const string Delete = "13";
        public const string GetSingle = "14";
        public const string GetAll = "15";
        public const string GetOwn = "16";
        public const string Transaction = "17";
        public const string SendEmail = "18";
        public const string PolicyUpdate = "19";
        public const string ResendVerificationEmail = "24";
        public const string ForgetEmailPwd = "25";
        public const string Approve = "20";
        public const string Upload = "22";
        public const string Pay = "21";
        public const string ApproveQuotation = "26";
    }
}