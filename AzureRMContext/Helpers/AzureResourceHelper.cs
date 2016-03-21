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

        public static string GetUrlFromId(string id)
        {
            return String.Format(ResourceUrlFormatString, id);
        }
    }
}
