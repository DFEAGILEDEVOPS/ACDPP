using System;
using Microsoft.VisualStudio.Services.Common;

namespace VstsApi.Net.Classes
{
    public class Identity
    {
        public Guid Id { get; set; }
        public bool IsContainer { get; set; }
        public bool IsExternalUser { get; set; }
        public string DisplayName { get; set; }
        public string ProviderDisplayName { get; set; }
        public string CustomDisplayName { get; set; }
        public int UniqueUserId { get; set; }
        public bool IsActive { get; set; }
        public Guid LocalScopeId { get; set; }
        public int ResourceVersion { get; set; }
        public SubjectDescriptor SubjectDescriptor { get; set; }
    }
}