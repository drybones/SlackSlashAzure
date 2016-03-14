﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;

using Microsoft.Azure;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Models;
using Microsoft.Rest;

namespace Redgate.Azure.ResourceManangement
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
                Console.WriteLine(
                    String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\n",
                    DateTime.Now.ToString(),
                    ex.ToString()));
            }

            return authResult;
        }

        public static IEnumerable<Database> GetDataWarehouses()
        {
            var authResult = GetAuthenticationResult();
            var tokenCredentials = new TokenCredentials(authResult.AccessToken);
            var rmClient = new ResourceManagementClient(tokenCredentials);

            var results = new List<Database>();

            string[] sep = new string[] { ",", ", ", " " };
            foreach (var subscriptionId in subscriptionIds.Split(sep, StringSplitOptions.RemoveEmptyEntries))
            {
                rmClient.SubscriptionId = subscriptionId;
                var resourceGroups = rmClient.ResourceGroups.List();

                var tokenCloudCredentials = new TokenCloudCredentials(subscriptionId, authResult.AccessToken);
                var sqlClient = new SqlManagementClient(tokenCloudCredentials);

                foreach (var resourceGroup in resourceGroups)
                {
                    var servers = sqlClient.Servers.List(resourceGroup.Name);
                    foreach (var server in servers)
                    {
                        var databases = sqlClient.Databases.List(resourceGroup.Name, server.Name);
                        var warehouses = databases.Where(d => d.Properties.Edition == "DataWarehouse");
                        results.AddRange(warehouses);
                    }
                }
            }
            return results;
        }
    }
}