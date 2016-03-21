using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redgate.Azure.ResourceManagement.Helpers
{
    public static class AzureResourceHelper
    {
        private const string ResourceUrlFormatString = @"https://portal.azure.com/#resource{0}";
        private const string ResourceGroupUrlFormatString = @"https://portal.azure.com/#asset/HubsExtension/ResourceGroups{0}";

        public static string GetResourceUrl(string resourceId)
        {
            return String.Format(ResourceUrlFormatString, resourceId);
        }
        public static string GetResourceGroupUrl(string resourceGroupId)
        {
            return String.Format(ResourceUrlFormatString, resourceGroupId);
        }
    }
}
