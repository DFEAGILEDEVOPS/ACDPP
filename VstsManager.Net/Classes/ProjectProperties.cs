namespace VstsApi.Net.Classes
{

    public class ProjectProperties
        {
            public const string CreatedBy = "Created By";
            public const string CreatedDate = "Created Date";
            public const string CostCode = "Cost Code";
            public const string AppUrl = "App Url";

            public static string[] All = new[] { CreatedBy, CreatedDate, CostCode, AppUrl };
        }
}