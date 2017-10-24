using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.VisualStudio.Services.ClientNotification;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Licensing;
using Microsoft.VisualStudio.Services.Licensing.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Users.Client;
using Microsoft.VisualStudio.Services.Common;

namespace VstsManager
{
    public class VstsManager
    {
        public enum LicenceTypes
        {
            Stakeholder,
            Basic,
            Professional,
            Advanced,
            Msdn
        }

        VssConnection Connection;
        public VstsManager(string collectionUrl,string personalAccessToken)
        {
            Connection=new VssConnection(new Uri(collectionUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        }

        public void AddUserToTeam(string userEmail, string teamId)
        {
            var client = Connection.GetClient<IdentityHttpClient>();
            IdentitiesCollection identities = Task.Run(async () => await client.ReadIdentitiesAsync(IdentitySearchFilter.MailAddress, userEmail)).Result;

            if (!identities.Any() || identities.Count > 1)throw new InvalidOperationException("User not found or could not get an exact match based on email");

            var userIdentity = identities.Single();
            var groupIdentity = Task.Run(async () => await client.ReadIdentityAsync(teamId)).Result;
            var success = Task.Run(async () => await client.AddMemberToGroupAsync(groupIdentity.Descriptor, userIdentity.Id)).Result;
        }

        public void AddUserToAccount(string accountName, string userEmail, LicenceTypes licenceType=LicenceTypes.Basic)
        {
            try
            {
                // We need the clients for two services: Licensing and Identity
                var licensingClient = Connection.GetClient<LicensingHttpClient>();
                var identityClient = Connection.GetClient<IdentityHttpClient>();

                // The first call is to see if the user already exists in the account.
                // Since this is the first call to the service, this will trigger the sign-in window to pop up.
                Console.WriteLine("Sign in as the admin of account {0}. You will see a sign-in window on the desktop.",accountName);
                var userIdentity = identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName, userEmail).Result.FirstOrDefault();

                // If the identity is null, this is a user that has not yet been added to the account.
                // We'll need to add the user as a "bind pending" - meaning that the email address of the identity is 
                // recorded so that the user can log into the account, but the rest of the details of the identity 
                // won't be filled in until first login.
                if (userIdentity == null)
                {
                    Console.WriteLine("Creating a new identity and adding it to the collection's licensed users group.");

                    // We are adding the user to a collection, and at the moment only one collection is supported per
                    // account in VSTS.
                    var collectionScope = identityClient.GetScopeAsync(accountName).Result;

                    // First get the descriptor for the licensed users group, which is a well known (built in) group.
                    var licensedUsersGroupDescriptor = new IdentityDescriptor(IdentityConstants.TeamFoundationType,GroupWellKnownSidConstants.LicensedUsersGroupSid);

                    // Now convert that into the licensed users group descriptor into a collection scope identifier.
                    var identifier = String.Concat(SidIdentityHelper.GetDomainSid(collectionScope.Id),SidIdentityHelper.WellKnownSidType,licensedUsersGroupDescriptor.Identifier.Substring(SidIdentityHelper.WellKnownSidPrefix.Length));

                    // Here we take the string representation and create the strongly-type descriptor
                    var collectionLicensedUsersGroupDescriptor = new IdentityDescriptor(IdentityConstants.TeamFoundationType,identifier);


                    // Get the domain from the user that runs this code. This domain will then be used to construct
                    // the bind-pending identity. The domain is either going to be "Windows Live ID" or the Azure 
                    // Active Directory (AAD) unique identifier, depending on whether the account is connected to
                    // an AAD tenant. Then we'll format this as a UPN string.
                    var currUserIdentity = Connection.AuthorizedIdentity.Descriptor;
                    var directory = "Windows Live ID"; // default to an MSA (fka Live ID)
                    if (currUserIdentity.Identifier.Contains('\\'))
                    {
                        // The identifier is domain\userEmailAddress, which is used by AAD-backed accounts.
                        // We'll extract the domain from the admin user.
                        directory = currUserIdentity.Identifier.Split(new char[] { '\\' })[0];
                    }
                    var upnIdentity = string.Format("upn:{0}\\{1}", directory, userEmail);

                    // Next we'll create the identity descriptor for a new "bind pending" user identity.
                    var newUserDesciptor = new IdentityDescriptor(IdentityConstants.BindPendingIdentityType,upnIdentity);

                    // We are ready to actually create the "bind pending" identity entry. First we have to add the
                    // identity to the collection's licensed users group. Then we'll retrieve the Identity object
                    // for this newly-added user. Without being added to the licensed users group, the identity 
                    // can't exist in the account.
                    bool result = identityClient.AddMemberToGroupAsync(collectionLicensedUsersGroupDescriptor,newUserDesciptor).Result;
                    userIdentity = identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName,userEmail).Result.FirstOrDefault();
                }

                Console.WriteLine("Assigning license to user.");

                License licence = AccountLicense.Express;
                switch (licenceType)
                {
                    case LicenceTypes.Stakeholder:
                        licence = AccountLicense.Stakeholder;
                        break;
                    case LicenceTypes.Professional:
                        licence = AccountLicense.Professional;
                        break;
                    case LicenceTypes.Advanced:
                        licence = AccountLicense.Advanced;
                        break;
                    case LicenceTypes.Msdn:
                        licence = MsdnLicense.Eligible;
                        break;
                }
                var entitlement = licensingClient.AssignEntitlementAsync(userIdentity.Id, licence).Result;

                Console.WriteLine("Success!");
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\nSomething went wrong...");
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                }
            }
        }
    }
}
