using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VstsApi.Net.Classes
{
    public static class Entensions
    {
        public static LicenceTypes ToLicenceType(this RoleTypes roleType)
        {
            switch (roleType)
            {
                case RoleTypes.Developer:
                case RoleTypes.Releaser:
                    return LicenceTypes.Basic;
                default:
                    return LicenceTypes.Stakeholder;
            }
        }
    }
}
