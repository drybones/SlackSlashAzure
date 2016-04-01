using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;
using System.Diagnostics;

using Microsoft.Azure;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Sql;
using Microsoft.Rest;

using Redgate.Azure.ResourceManagement.Models;

namespace Redgate.Azure.ResourceManagement
{
    // Use this guide to set up the AD application, the service principal, and to grant it
    // the right role. (I used the portal to do the app and service principal, but needed
    // the psh commandlet for the role). Also be aware you only grant the reader role to 
    // the in-context subscription. Use Select-AzureRmSubscription to change the subscription.
    // https://azure.microsoft.com/en-us/documentation/articles/resource-group-authenticate-service-principal/

    // Also useful:
    // https://azure.microsoft.com/en-gb/documentation/articles/sql-database-client-library/
    // https://github.com/Azure-Samples/active-directory-dotnet-daemon

    public static class AzureRMContext
    {
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static string subscriptionIds = ConfigurationManager.AppSettings["ida:SubscriptionIds"];

        static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
        private static AuthenticationContext authContext = null;
        private static ClientCredential clientCredential = null;

        static AzureRMContext()
        {
            authContext = new AuthenticationContext(authority);
            clientCredential = new ClientCredential(clientId, appKey);
        }

        static AuthenticationResult GetAuthenticationResult()
        {
            // Get an access token from Azure AD using client credentials.
            AuthenticationResult authResult = null;

            try
            {
                // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                authResult = authContext.AcquireToken("https://management.core.windows.net/", clientCredential);
            }
            catch (AdalException ex)
            {
                Trace.TraceError(
                    String.Format("AzureRMContext:GetAuthenticationResult: An error occurred while acquiring a token.\nError: {1}\n",
                    ex.ToString()));
            }

            return authResult;
        }

        public static IEnumerable<Database> GetAllDataWarehouses()
        {
            var authResult = GetAuthenticationResult();
            var tokenCredentials = new TokenCredentials(authResult.AccessToken);
            var rmClient = new ResourceManagementClient(tokenCredentials);

            var results = new List<Database>();
            
            string[] sep = new string[] { ",", ", ", " " };
            foreach (var subscriptionId in subscriptionIds.Split(sep, StringSplitOptions.RemoveEmptyEntries))
            {
                Trace.TraceInformation($"AzureRmContext:GetAllDataWarehouses: Searching subscription {subscriptionId}");
                var subscription = new Subscription() { Id = subscriptionId };
                rmClient.SubscriptionId = subscriptionId;
                var resourceGroups = rmClient.ResourceGroups.List();

                var tokenCloudCredentials = new TokenCloudCredentials(subscriptionId, authResult.AccessToken);
                var sqlClient = new SqlManagementClient(tokenCloudCredentials);

                foreach (var rg in resourceGroups)
                {
                    Trace.TraceInformation($"AzureRmContext:GetAllDataWarehouses: Searching resourceGroup {rg.Name}");
                    var resourceGroup = new ResourceGroup() { Id = rg.Id, Name = rg.Name, Location = rg.Location, Subscription = subscription };
                    var servers = sqlClient.Servers.List(rg.Name);
                    foreach (var s in servers)
                    {
                        Trace.TraceInformation($"AzureRmContext:GetAllDataWarehouses: Searching server {s.Name}");
                        var databaseServer = new DatabaseServer() { Id = s.Id, Name = s.Name, Location = s.Location, Version = s.Properties.Version, ResourceGroup = resourceGroup };
                        var databases = sqlClient.Databases.List(rg.Name, s.Name);
                        var warehouses = databases.Where(d => d.Properties.Edition == "DataWarehouse");
                        foreach(var w in warehouses)
                        {
                            Trace.TraceInformation($"AzureRmContext:GetAllDataWarehouses: Found warehouse {w.Name}");
                            var warehouse = new Database() { Id = w.Id, Name = w.Name, Location = w.Location, Status = w.Properties.Status, Edition = w.Properties.Edition, ServiceObjective = w.Properties.ServiceObjective, DatabaseServer = databaseServer };
                            results.Add(warehouse);                            
                        }
                    }
                }
            }
            return results;
        }

        public static IEnumerable<Database> PauseAllDataWarehouses()
        {
            var authResult = GetAuthenticationResult();

            var onlineWarehouses = GetAllDataWarehouses().Where(dw => dw.Status == "Online");
            Trace.TraceInformation($"AzureRMContext:PauseAllDataWarehouses: Found {onlineWarehouses.Count()} datawarehouse(s) to pause.");
            foreach (var dw in onlineWarehouses)
            {
                var tokenCloudCredentials = new TokenCloudCredentials(dw.DatabaseServer.ResourceGroup.Subscription.Id, authResult.AccessToken);
                var sqlClient = new SqlManagementClient(tokenCloudCredentials);
                sqlClient.DatabaseActivation.BeginPauseAsync(dw.DatabaseServer.ResourceGroup.Name, dw.DatabaseServer.Name, dw.Name);
                Trace.TraceInformation($"AzureRMContext:PauseAllDataWarehouses: Called BeginPauseAsync for {dw.Name}");
            }
            return onlineWarehouses;
        }
    }
}
