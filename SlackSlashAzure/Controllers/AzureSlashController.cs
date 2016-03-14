using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using SlackSlashAzure.Models;
using Redgate.Azure.ResourceManangement;
using System.Threading;
using System.Web.Hosting;
using System.Configuration;

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
                resp = new SlashResponse() { text = "Sorry, didn't find Data Warehouses.", response_type = "in_channel" };
            }
            else
            {
                resp = new SlashResponse() { response_type = "in_channel" };
                foreach (var dw in dataWarehouses)
                {
                    resp.text += $"{dw.Name}: {dw.Properties.Status} {dw.Properties.ServiceObjective}\n";
                }
            }
            using (var client = new HttpClient())
            {
                await client.PostAsJsonAsync(responseUrl, resp);
            }
        }
    }
}
