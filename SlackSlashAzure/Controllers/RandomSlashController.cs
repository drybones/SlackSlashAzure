using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using SlackSlashAzure.Models;

namespace SlackSlashAzure.Controllers
{
    public class RandomSlashController : ApiController
    {
        public IHttpActionResult Post(SlashRequest req)
        {
            var resp = new SlashResponse() { text = $"{req.user_name} sighs...", response_type = "in_channel" };
            return Ok(resp);
        }

    }
}
