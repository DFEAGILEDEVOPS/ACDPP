using System;
using System.Reflection;
using Extensions;
using System.Web;

namespace Dashboard.Models
{
    [Serializable]
    public class CustomErrorViewModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string CallToAction { get; set; }
        public string ActionText { get; set; } = "Continue";
        public string ActionUrl { get; set; }
    }
}