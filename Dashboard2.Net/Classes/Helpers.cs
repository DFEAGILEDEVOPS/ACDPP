using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Dashboard.Net.Classes
{
    public static class Helpers
    {
        //Removes all but the specified properties from the model state
        public static void Include(this ModelStateDictionary modelState, params string[] properties)
        {
            foreach (var key in modelState.Keys.ToList())
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (properties.ContainsI(key)) continue;
                modelState.Remove(key);
            }
        }

        //Removes all the specified properties from the model state
        public static void Exclude(this ModelStateDictionary modelState, params string[] properties)
        {
            foreach (var key in modelState.Keys.ToList())
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!properties.ContainsI(key)) continue;
                modelState.Remove(key);
            }
        }
        public static MvcHtmlString CustomValidationSummary(this HtmlHelper helper, bool excludePropertyErrors = true, string validationSummaryMessage = "The following errors were detected", object htmlAttributes = null)
        {
            helper.ViewBag.ValidationSummaryMessage = validationSummaryMessage;
            helper.ViewBag.ExcludePropertyErrors = excludePropertyErrors;

            return helper.Partial("_ValidationSummary");
        }

    }
}