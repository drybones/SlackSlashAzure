using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlackSlashAzure.Models
{
    public class SlashResponse
    {
        public string response_type { get; set; }
        public string text { get; set; }
        public SlackAttachment[] attachments { get; set; }
    }
}