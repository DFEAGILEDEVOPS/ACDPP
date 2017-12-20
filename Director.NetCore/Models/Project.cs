using Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Director.NetCore.Models
{
    public class Project
    {
        public class Team
        {
            public class Member
            {
                [XmlAttribute]
                public string Email { get; set; }
                [XmlAttribute]
                public string Role { get; set; }

                internal void Validate(Project project,List<Exception> exceptions)
                {
                    if (string.IsNullOrWhiteSpace(Email))
                        exceptions.Add(new ArgumentNullException(nameof(Email), $"Team {nameof(Email)} cannot be empty"));
                    else if (!Email.IsEmailAddress())
                        exceptions.Add(new ArgumentNullException(nameof(Email), $"Team member {nameof(Email)} '{Email}' is not a valid email address"));

                    if (string.IsNullOrWhiteSpace(Role))
                        exceptions.Add(new ArgumentNullException(nameof(Role), $"Team {nameof(Role)} cannot be empty"));

                }
            }

            [XmlAttribute]
            public string Name { get; set; }
            public List<Member> Members { get; set; }

            internal void Validate(Project project,List<Exception> exceptions)
            {
                if (string.IsNullOrWhiteSpace(Name)) exceptions.Add(new ArgumentNullException(nameof(Name), $"Team {nameof(Name)} cannot be empty"));

                if (Members == null || Members.Count == 0) exceptions.Add(new ArgumentNullException(nameof(Members), $"{nameof(Members)} you must have at least one team member"));
                foreach (var member in Members) member.Validate(project,exceptions);
            }
        }

        public class Repository
        {
            [XmlAttribute]
            public string Name { get; set; }

            internal void Validate(Project project,List<Exception> exceptions)
            {
                throw new NotImplementedException();
            }
        }

        public class AppRegistration
        {
            [XmlAttribute]
            public string Name { get; set; }
            [XmlAttribute]
            public string Url { get; set; }

            internal void Validate(Project project,List<Exception> exceptions)
            {
                throw new NotImplementedException();
            }
        }


        public class ResourceGroup
        {
            public class KeyVault
            {
                [XmlAttribute]
                public string Name { get; set; }

                internal void Validate(Project project,List<Exception> exceptions)
                {
                    throw new NotImplementedException();
                }
            }


            public class SqlServer
            {
                public class Database
                {
                    [XmlAttribute] public string Name { get; set; }
                }

                public class FirewallRule
                {
                    [XmlAttribute]
                    public string Name { get; set; }
                    [XmlAttribute]
                    public string StartIP { get; set; }
                    [XmlAttribute]
                    public string EndIP { get; set; }
                }

                [XmlAttribute]
                public string Name { get; set; }
                [XmlAttribute]
                public string PricingTier { get; set; }
                [XmlAttribute]
                public string KeyVaultName { get; set; }
                [XmlAttribute]
                public string UsernameSecretName { get; set; }
                [XmlAttribute]
                public string PasswordSecretName { get; set; }
                public List<Database> Databases { get; set; }
                public List<FirewallRule> FirewallRules { get; set; }
                [XmlAttribute]
                public bool AllowAzureAccess { get; set; }

                internal void Validate(Project project,List<Exception> exceptions)
                {
                    throw new NotImplementedException();
                }
            }

            public class StorageAccount
            {
                public class BlobContainer
                {
                    [XmlAttribute]
                    public string Name { get; set; }
                }

                public class FilesShare
                {
                    [XmlAttribute]
                    public string Name { get; set; }
                }

                public class Table
                {
                    [XmlAttribute]
                    public string Name { get; set; }
                }

                public class Queue
                {
                    [XmlAttribute]
                    public string Name { get; set; }
                }
                [XmlAttribute]
                public string Name { get; set; }
                [XmlAttribute]
                public string KeyVaultName { get; set; }
                [XmlAttribute]
                public string AccessKeySecretName { get; set; }
                public List<BlobContainer> BlobContainers { get; set; }
                public List<FilesShare> FilesShares { get; set; }
                public List<Table> Tables { get; set; }
                public List<Queue> Queues { get; set; }

                internal void Validate(Project project,List<Exception> exceptions)
                {
                    throw new NotImplementedException();
                }
            }


            public class RedisCache
            {
                [XmlAttribute]
                public string Name { get; set; }
                [XmlAttribute]
                public string PricingTier { get; set; }
                [XmlAttribute]
                public string KeyVaultName { get; set; }
                [XmlAttribute]
                public string AccessKeySecretName { get; set; }

                internal void Validate(Project project,List<Exception> exceptions)
                {
                    throw new NotImplementedException();
                }
            }
            [XmlAttribute]
            public string Name { get; set; }
            [XmlAttribute]
            public string Region { get; set; }
            public List<KeyVault> KeyVaults { get; set; }
            public List<SqlServer> SqlServers { get; set; }
            public List<StorageAccount> StorageAccounts { get; set; }
            public List<RedisCache> RedisCaches { get; set; }

            internal void Validate(Project project,List<Exception> exceptions)
            {
                if (string.IsNullOrWhiteSpace(Name)) exceptions.Add(new ArgumentNullException(nameof(Name), $"Resource Group {nameof(Name)} cannot be empty"));
                if (string.IsNullOrWhiteSpace(Region)) exceptions.Add(new ArgumentNullException(nameof(Region), $"Resource Group {nameof(Region)} cannot be empty"));

                if (KeyVaults == null || KeyVaults.Count == 0) exceptions.Add(new ArgumentNullException(nameof(KeyVaults), $"{nameof(KeyVaults)} you must have at least one Key Vault"));
                foreach (var vault in KeyVaults) vault.Validate(project,exceptions);
                foreach (var server in SqlServers) server.Validate(project,exceptions);
                foreach (var storage in StorageAccounts) storage.Validate(project,exceptions);
                foreach (var cache in RedisCaches) cache.Validate(project,exceptions);
            }
        }

        internal void Execute()
        {
            throw new NotImplementedException();
        }

        public class Deletion
        {
            [XmlAttribute]
            public string Type { get; set; }
            [XmlAttribute]
            public string Name { get; set; }

            internal void Validate(Project project, List<Exception> exceptions)
            {
                throw new NotImplementedException();
            }
        }

        internal void Validate()
        {
            var exceptions = new List<Exception>();

            if (string.IsNullOrWhiteSpace(ProjectName)) exceptions.Add(new ArgumentNullException(nameof(ProjectName), $"{nameof(ProjectName)} cannot be empty"));

            if (Teams == null || Teams.Count == 0) exceptions.Add(new ArgumentNullException(nameof(Teams), $"{nameof(Teams)} you must have at least one team"));
            foreach (var team in Teams) team.Validate(this,exceptions);

            if (ResourceGroups==null || Teams.Count==0) exceptions.Add(new ArgumentNullException(nameof(ResourceGroups), $"{nameof(ResourceGroups)} you must have at least one resource group"));
            foreach (var group in ResourceGroups) group.Validate(this,exceptions);

            if (Repositories==null || Repositories.Count==0) exceptions.Add(new ArgumentNullException(nameof(Repositories), $"{nameof(Repositories)} you must have at least one repository"));
            foreach (var repo in Repositories) repo.Validate(this,exceptions);

            if (AppRegistrations == null || AppRegistrations.Count==0) exceptions.Add(new ArgumentNullException(nameof(AppRegistrations), $"{nameof(AppRegistrations)} you must have at least one App Registration"));
            foreach (var app in AppRegistrations) app.Validate(this,exceptions);

            foreach (var deletion in Deletions) deletion.Validate(this,exceptions);

            if (exceptions.Count > 0) throw new AggregateException(exceptions);
        }

        public string ProjectName { get; set; }
        public List<Team> Teams { get; set; }
        public List<Repository> Repositories { get; set; }
        public List<ResourceGroup> ResourceGroups { get; set; }
        public List<AppRegistration> AppRegistrations { get; set; }
        public List<Deletion> Deletions { get; set; }
    }
}
