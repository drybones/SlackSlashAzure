using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Hosting;
using System.Configuration;

using SlackSlashAzure.Models;
using Redgate.Azure.ResourceManagement;
using Redgate.Azure.ResourceManagement.Models;
using Redgate.Azure.ResourceManagement.Helpers;

namespace SlackSlashAzure.Controllers
{
    public class AzureSlashController : ApiController
    {
        public IHttpActionResult Post(SlashRequest req)
        {
            string[] sep = new string[] { ",", ", ", " " };
            if (!ConfigurationManager.AppSettings["slack:validTokens"].Split(sep, StringSplitOptions.RemoveEmptyEntries).Contains(req.token))
            {
                return Unauthorized();
            }

            if (req.command != "/azure" || req.text == null)
            {
                return Ok(new SlashResponse() { text = $"Sorry, I don't know how to `{req.command} {req.text}`" });
            }

            if (req.text.Trim() == "dw")
            {
                var resp = new SlashResponse() { text = $"_Getting data from Azure..._", response_type = "in_channel" };
                HostingEnvironment.QueueBackgroundWorkItem(ct => GetDataWarehousesFromAzure(req.response_url));
                return Ok(resp);
            }
            else if (req.text.Trim() == "dw pause")
            {
                var resp = new SlashResponse() { text = $"_Pausing all online data warehouses..._", response_type = "in_channel" };
                HostingEnvironment.QueueBackgroundWorkItem(ct => PauseAllDataWarehouses(req.response_url));
                return Ok(resp);
            }
            else
            {
                return Ok(new SlashResponse() { text = $"Sorry, I don't know how to `{req.command} {req.text}`" });
            }
        }
        private async void PauseAllDataWarehouses(string responseUrl)
        {
            var pausedWarehouses = AzureRMContext.PauseAllDataWarehouses();
            SlashResponse resp = null;

            if (pausedWarehouses.Count() < 1)
            {
                resp = new SlashResponse() { text = "Couldn't find any online data warehouses.", response_type = "in_channel" };
            }
            else
            {
                resp = new SlashResponse() { text = "Found these online ones. I'll try to pause them.", response_type = "in_channel" };
                var attachments = new List<SlackAttachment>();
                foreach (var dw in pausedWarehouses)
                {
                    attachments.Add(CreateAttachmentForDataWarehouse(dw));
                }
                resp.attachments = attachments.ToArray();
            }
            using (var client = new HttpClient())
            {
                await client.PostAsJsonAsync(responseUrl, resp);
            }
        }

        private async void GetDataWarehousesFromAzure(string responseUrl)
        {
            var dataWarehouses = AzureRMContext.GetDataWarehouses();
            SlashResponse resp = null;

            if(dataWarehouses.Count() < 1)
            {
                resp = new SlashResponse() { text = "Sorry, didn't find data warehouses.", response_type = "in_channel" };
            }
            else
            {
                resp = new SlashResponse() { response_type = "in_channel" };
                var attachments = new List<SlackAttachment>();
                foreach (var dw in dataWarehouses)
                {
                    attachments.Add(CreateAttachmentForDataWarehouse(dw));
                }
                resp.attachments = attachments.ToArray();
            }
            using (var client = new HttpClient())
            {
                await client.PostAsJsonAsync(responseUrl, resp);
            }
        }

        private SlackAttachment CreateAttachmentForDataWarehouse(Database dw)
        {
            var attachment = new SlackAttachment() { title = dw.Name, title_link = AzureResourceHelper.GetResourceUrl(dw.Id), fallback = $"{dw.Name} {dw.Status} {dw.ServiceObjective}" };

            switch(dw.Status)
            {
                case "Paused":
                case "Pausing":
                    attachment.color = "good";
                    break;
                case "Online":
                case "Resuming":
                    // Whitelist the "cheap" plans
                    if(dw.ServiceObjective == "DW100" || dw.ServiceObjective == "DW200")
                    {
                        attachment.color = "warning";
                    }
                    else
                    {
                        attachment.color = "danger";
                    }
                    break;
                case "Scaling":
                default:
                    // Leave it gray
                    break;
            }

            var fields = new SlackField[] {
                new SlackField() { title = "Status", value = dw.Status, IsShort = true },
                new SlackField() { title = "Service level", value = dw.ServiceObjective, IsShort = true },
                new SlackField() { title = "Server", value = $"<{AzureResourceHelper.GetResourceUrl(dw.DatabaseServer.Id)}|{dw.DatabaseServer.Name}>", IsShort = true },
                new SlackField() { title = "Resource Group", value = $"<{AzureResourceHelper.GetResourceGroupUrl(dw.DatabaseServer.ResourceGroup.Id)}|{dw.DatabaseServer.ResourceGroup.Name}>", IsShort = true }
            };
            attachment.fields = fields;
            return attachment;
        }
    }
}
