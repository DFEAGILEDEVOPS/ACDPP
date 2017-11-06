using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
    }
}