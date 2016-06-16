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
            return String.Format(ResourceGroupUrlFormatString, resourceGroupId);
        }

        // Random helper bits copied from 
        // https://github.com/Azure/azure-powershell/blob/83203f0c2ab4988661f570991b4b26a6c8ee8338/src/ResourceManager/Resources/Commands.ResourceManager/Cmdlets/Components/ResourceIdUtility.cs
        // https://github.com/Azure/azure-powershell/blob/49fb96fe46ce8b759d4ff4b4fa1b39e134514f39/src/ResourceManager/Resources/Commands.ResourceManager/Cmdlets/Extensions/ResourceExtensions.cs
        // https://github.com/Azure/azure-powershell/blob/49fb96fe46ce8b759d4ff4b4fa1b39e134514f39/src/ResourceManager/Resources/Commands.ResourceManager/Cmdlets/Extensions/StringExtensions.cs
        public static string GetResourceGroupName(string resourceId)
        {
            return GetNextSegmentAfter(resourceId: resourceId, segmentName: "ResourceGroups");
        }
        private static string GetNextSegmentAfter(string resourceId, string segmentName, bool selectLastSegment = false)
        {
            var segment = 
                GetSubstringAfterSegment(
                    resourceId: resourceId,
                    segmentName: segmentName,
                    selectLastSegment: selectLastSegment)
                .SplitRemoveEmpty('/')
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(segment)
                ? null
                : segment;
        }
        private static string GetSubstringAfterSegment(string resourceId, string segmentName, bool selectLastSegment = true)
        {
            var segment = string.Format("/{0}/", segmentName.Trim('/').ToUpperInvariant());

            var index = selectLastSegment
                ? resourceId.LastIndexOf(segment, StringComparison.InvariantCultureIgnoreCase)
                : resourceId.IndexOf(segment, StringComparison.InvariantCultureIgnoreCase);

            return index < 0
                ? null
                : resourceId.Substring(index + segment.Length);
        }
        private static string[] SplitRemoveEmpty(this string source, params char[] separator)
        {
            return source.CoalesceString().Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }
        private static string CoalesceString(this string original)
        {
            return original ?? string.Empty;
        }
    }
}
