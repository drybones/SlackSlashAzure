using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using SlackSlashAzure.Models;
using Redgate.Azure.ResourceManangement;
using System.Web.Hosting;
using System.Configuration;
using Microsoft.Azure.Management.Sql.Models;

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

            if(req.command != "/azure" || req.text == null || req.text.Trim() != "dw")
            {
                return Ok(new SlashResponse() { text = $"Sorry, I don't know how to `{req.command} {req.text}`" });
            }

            var resp = new SlashResponse() { text = $"Getting data from Azure...", response_type = "in_channel" };
            HostingEnvironment.QueueBackgroundWorkItem(ct => GetDataWarehousesFromAzure(req.response_url));

            return Ok(resp);
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
                resp = new SlashResponse() { text = "Here are the data warehouses I found:", response_type = "in_channel" };
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
            var attachment = new SlackAttachment() { title = dw.Name, fallback = $"{dw.Name} {dw.Properties.Status} {dw.Properties.ServiceObjective}" };

            if(dw.Properties.Status == "Paused" || dw.Properties.Status == "Pausing")
            {
                attachment.color = "good";
            }
            else if (dw.Properties.Status == "Online" || dw.Properties.ServiceObjective == "DW100" || dw.Properties.ServiceObjective == "DW200")
            {
                attachment.color = "warning";
            }
            else if(dw.Properties.Status == "Online")
            {
                attachment.color = "danger";
            }
            // else leave it grey -- not sure what state it's in

            var fields = new SlackField[] {
                new SlackField() { title = "Status", value = dw.Properties.Status, IsShort = true },
                new SlackField() { title = "Service level", value = dw.Properties.ServiceObjective, IsShort = true }
            };
            attachment.fields = fields;
            return attachment;
        }
    }
}
