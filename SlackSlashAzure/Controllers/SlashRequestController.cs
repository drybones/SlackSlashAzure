using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using SlackSlashAzure.Models;

namespace SlackSlashAzure.Controllers
{
    public class SlashRequestController : ApiController
    {
        public IHttpActionResult Post(SlashRequest req)
        {
            var resp = new SlashResponse() { text = $"You said {req.command} {req.text}\nBut did you really mean it?" };
            return Ok(resp);
        }
    }
}
