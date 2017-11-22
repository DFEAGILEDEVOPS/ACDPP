using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Fluent;
using System;
using System.Collections.Generic;

namespace AzureApi.Net
{

    public class SqlDatabaseBuilder
    {
        #region Sql Server
        public static ISqlServer GetServer(string serverName, string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            var server = azure.SqlServers.GetByResourceGroup(resourceGroup, serverName);
            return server;
        }

        public static IEnumerable<ISqlServer> ListServers(string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            var servers = azure.SqlServers.ListByResourceGroup(resourceGroup);
            return servers;
        }

        public static ISqlServer CreateServer(string serverName, string resourceGroup, string adminUsername, string adminPassword, Region region=null, IAzure azure=null)
        {
            if (azure == null) azure = Core.Authenticate();

            if (region == null) region = Core.GetResourceGroup(resourceGroup,azure)?.Region;

            var sqlServer = azure.SqlServers.Define(serverName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithAdministratorLogin(adminUsername)
                .WithAdministratorPassword(adminPassword)
                .Create();
            return sqlServer;
        }

        public static void DeleteServer(string serverName, string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            azure.SqlServers.DeleteByResourceGroup(resourceGroup, serverName);
        }

        public static void DeleteServer(ISqlServer server, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            azure.SqlServers.DeleteById(server.Id);
        }
        #endregion

        #region Firewall rules
        public static ISqlFirewallRule GetFirewallRule(ISqlServer server, string ruleName)
        {
            return server.FirewallRules.Get(ruleName);
        }

        public static IEnumerable<ISqlFirewallRule> ListFirewallRules(ISqlServer server)
        {
            return server.FirewallRules.List();
        }

        public static ISqlFirewallRule CreateFirewallRule(ISqlServer server, string ruleName, string startIP, string endIP = null)
        {
            ISqlFirewallRule rule;
            if (string.IsNullOrWhiteSpace(endIP) || endIP.Equals(startIP))
                rule = server.FirewallRules.Define(ruleName)
                    .WithIPAddress(startIP)
                    .Create();
            else
                rule = server.FirewallRules.Define(ruleName)
                    .WithIPAddressRange(startIP,endIP)
                    .Create();

            return rule;
        }

        public static void DeleteFirewallRule(ISqlServer server, string ruleName)
        {
            server.FirewallRules.Delete(ruleName);
        }
        #endregion

        #region Sql Databases
        public static ISqlDatabase GetDatabase(ISqlServer server,string databaseName)
        {
            return server.Databases.Get(databaseName);
        }

        public static IEnumerable<ISqlDatabase> ListDatabases(ISqlServer server)
        {
            return server.Databases.List();
        }

        public static ISqlDatabase CreateDatabase(ISqlServer sqlServer, string databaseName, string serviceObjective="BASIC")
        {
            if (string.IsNullOrWhiteSpace(databaseName)) databaseName = SdkContext.RandomResourceName("sqldatabase", 20);

            var database = sqlServer.Databases
                           .Define(databaseName)
                           .WithServiceObjective(serviceObjective)
                           .Create();
            return database;
        }

        public static void DeleteDatabase(ISqlServer sqlServer, string databaseName)
        {
            sqlServer.Databases.Delete(databaseName);
        }
        #endregion

        public static ISqlServer CreateSqlServer(string serverName, string resourceGroup, string adminUsername, string adminPassword, string startIP, string endIP)
        {
            //Create the server if it doesnt already exist
            var server = CreateServer(serverName,resourceGroup,adminUsername,adminPassword);

            //Create the firewall rule if it doesnt already exist
            var ruleName = string.IsNullOrWhiteSpace(endIP) || startIP==endIP ? $"{startIP.Replace(".", "_")}" : $"{startIP.Replace(".","_")}-{endIP.Replace(".", "_")}";
            var rule = GetFirewallRule(server, ruleName);
            if (rule == null) rule=CreateFirewallRule(server, ruleName, startIP, endIP);

            //return the server name
            return server;
        }

        public static ISqlDatabase CreateSqlDatabase(string resourceGroup, string serverName, string databaseName)
        {
            //Create the server if it doesnt already exist
            var server = GetServer(serverName, resourceGroup);
            if (server == null) throw new ArgumentException($"Cannot find server '{serverName}' in resource group '{resourceGroup}'", nameof(serverName));

            //Create the database if it doesnt already exist
            var database = GetDatabase(server, databaseName);
            if (database == null) database=CreateDatabase(server, databaseName);
            
            //return the server name
            return database;
        }

    }
}
