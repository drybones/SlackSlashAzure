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
using Redgate.Azure.ResourceManagement.Helpers;

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

        private const string PauseSupressionKey = "DoNotPause";

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
                    String.Format("AzureRMContext:GetAuthenticationResult: An error occurred while acquiring a token.\nError: {0}\n",
                    ex.ToString()));
                throw ex;
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
                rmClient.SubscriptionId = subscriptionId;

                var resourceGroups = rmClient.ResourceGroups.List();
                var allDatabases = rmClient.Resources.List(new Microsoft.Rest.Azure.OData.ODataQuery<Microsoft.Azure.Management.Resources.Models.GenericResourceFilter>("$filter=ResourceType eq 'Microsoft.Sql/servers/databases'") ).Where(r => r.Type == "Microsoft.Sql/servers/databases");

                var tokenCloudCredentials = new TokenCloudCredentials(subscriptionId, authResult.AccessToken);
                var sqlClient = new SqlManagementClient(tokenCloudCredentials);

                foreach(var db in allDatabases)
                {
                    var databaseName = AzureResourceHelper.GetDatabaseName(db.Id);
                    var resourceGroupName = AzureResourceHelper.GetResourceGroupName(db.Id);
                    var sqlServerName = AzureResourceHelper.GetSqlServerName(db.Id);
                    var w = sqlClient.Databases.Get(resourceGroupName, sqlServerName, databaseName).Database;
                    if (w.Properties.Edition == "DataWarehouse")
                    {
                        Trace.TraceInformation($"AzureRmContext:GetAllDataWarehouses: Found warehouse {w.Name}");
                        var warehouse = new Database() { Id = w.Id, Name = w.Name, Location = w.Location, Status = w.Properties.Status, Edition = w.Properties.Edition, ServiceObjective = w.Properties.ServiceObjective, SqlServerName = sqlServerName, ResourceGroupName = resourceGroupName, SubscriptionId = subscriptionId };
                        warehouse.Tags = allDatabases.First(r => r.Id == w.Id)?.Tags; // Tags only get returned on the orginal resources API call
                        results.Add(warehouse);
                    }
                    else
                    {
                        Trace.TraceInformation($"AzureRmContext:GetAllDataWarehouses: Tried {w.Name}. It wasn't a warehouse.");
                    }
                }
            }
            return results;
        }

        public static IEnumerable<Database> GetOnlineDataWarehouses()
        {
            var authResult = GetAuthenticationResult();
            var onlineWarehouses = GetAllDataWarehouses().Where(dw => dw.Status == "Online" && (dw.Tags == null || !dw.Tags.Keys.Contains(PauseSupressionKey)));
            Trace.TraceInformation($"AzureRMContext:PauseAllDataWarehouses: Found {onlineWarehouses.Count()} online datawarehouse(s).");
            return onlineWarehouses;
        }

        public static string PauseDataWarehouse(Database dataWarehouse)
        {
            var authResult = GetAuthenticationResult();
            var tokenCloudCredentials = new TokenCloudCredentials(dataWarehouse.SubscriptionId, authResult.AccessToken);
            var sqlClient = new SqlManagementClient(tokenCloudCredentials);
            var response = sqlClient.DatabaseActivation.BeginPause(dataWarehouse.ResourceGroupName, dataWarehouse.SqlServerName, dataWarehouse.Name);
            Trace.TraceInformation($"AzureRMContext:PauseAllDataWarehouses: Called BeginPauseAsync for {dataWarehouse.Name}");
            return response.RequestId;
        }
    }
}
