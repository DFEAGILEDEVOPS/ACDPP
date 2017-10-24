using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Classes
{
    public enum RoleTypes
    {
        Unknown,
        Developer,
        Releaser
    }

    public enum StateFilters{
        WellFormed,
        CreatePending,
        Deleting,
        New,
        All
    }
}
