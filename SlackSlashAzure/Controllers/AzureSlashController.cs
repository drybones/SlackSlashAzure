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
    enum AttachmentStyle { Verbose, Summary, NameOnly };

    public class AzureSlashController : ApiController
    {
        public IHttpActionResult Post(SlashRequest req)
        {
            string[] sep = new string[] { ",", ", ", " " };
            if (!ConfigurationManager.AppSettings["slack:validTokens"].Split(sep, StringSplitOptions.RemoveEmptyEntries).Contains(req.token))
            {
                return Unauthorized();
            }

            var result = new SlashResponse() { text = $"Sorry, I don't know how to `{req.command} {req.text}`" };

            if (req.command == "/azure" && req.text != null)
            {
                switch(req.text.Trim())
                {
                    case "dw":
                        result = new SlashResponse() { text = $"_Getting data from Azure..._", response_type = "in_channel" };
                        HostingEnvironment.QueueBackgroundWorkItem(ct => GetAllDataWarehouses(req.response_url));
                        break;
                    case "dw pause":
                        result = new SlashResponse() { text = $"_Getting data from Azure..._", response_type = "in_channel" };
                        HostingEnvironment.QueueBackgroundWorkItem(ct => PauseAllDataWarehouses(req.response_url));
                        break;
                }
            }

            return Ok(result);
        }

        private async void GetAllDataWarehouses(string responseUrl)
        {
            var dataWarehouses = AzureRMContext.GetAllDataWarehouses();
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

        private SlackAttachment CreateAttachmentForDataWarehouse(Database dw, AttachmentStyle attachmentStyle = AttachmentStyle.Summary)
        {
            var attachment = new SlackAttachment() { title = dw.Name, title_link = AzureResourceHelper.GetResourceUrl(dw.Id), fallback = $"{dw.Name} {dw.Status} {dw.ServiceObjective}" };

            switch (dw.Status)
            {
                case "Paused":
                case "Pausing":
                    attachment.color = "good";
                    break;
                case "Online":
                case "Resuming":
                    // Whitelist the "cheap" plans
                    if (dw.ServiceObjective == "DW100" || dw.ServiceObjective == "DW200")
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

            if (attachmentStyle != AttachmentStyle.NameOnly)
            {
                var fields = new List<SlackField>();
                fields.Add(new SlackField() { title = "Status", value = dw.Status, IsShort = true });
                fields.Add(new SlackField() { title = "Service level", value = dw.ServiceObjective, IsShort = true });
                if (attachmentStyle == AttachmentStyle.Verbose)
                {
                    fields.Add(new SlackField() { title = "Server", value = $"<{AzureResourceHelper.GetResourceUrl(dw.DatabaseServer.Id)}|{dw.DatabaseServer.Name}>", IsShort = true });
                    fields.Add(new SlackField() { title = "Resource Group", value = $"<{AzureResourceHelper.GetResourceGroupUrl(dw.DatabaseServer.ResourceGroup.Id)}|{dw.DatabaseServer.ResourceGroup.Name}>", IsShort = true });
                }
                attachment.fields = fields.ToArray();
            }
            return attachment;
        }

        private async void PauseAllDataWarehouses(string responseUrl)
        {
            var onlineWarehouses = AzureRMContext.GetOnlineDataWarehouses();
            SlashResponse resp = null;

            if (onlineWarehouses.Count() < 1)
            {
                resp = new SlashResponse() { text = "Couldn't find any online data warehouses.", response_type = "in_channel" };
            }
            else
            {
                resp = new SlashResponse() { text = "Found these online ones. I'll pause them.", response_type = "in_channel" };
                var attachments = new List<SlackAttachment>();
                foreach (var dw in onlineWarehouses)
                {
                    attachments.Add(CreateAttachmentForDataWarehouse(dw, AttachmentStyle.NameOnly));
                }
                resp.attachments = attachments.ToArray();
            }

            using (var client = new HttpClient())
            {
                await client.PostAsJsonAsync(responseUrl, resp);
            }

            foreach (var dw in onlineWarehouses)
            {
                HostingEnvironment.QueueBackgroundWorkItem(ct => PauseDataWarehouse(responseUrl, dw));
            }
        }

        private async void PauseDataWarehouse(string responseUrl, Database dataWarehouse)
        {
            using (var client = new HttpClient())
            {
                var requestId = AzureRMContext.PauseDataWarehouse(dataWarehouse);
                var resp = new SlashResponse() { text = $"Request to pause `{dataWarehouse.Name}` accepted, request_id `{requestId}`", response_type = "in_channel" };
                await client.PostAsJsonAsync(responseUrl, resp);
            }
        }
    }
}
