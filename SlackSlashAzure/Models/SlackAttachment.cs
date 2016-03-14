using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlackSlashAzure.Models
{
    public class SlackAttachment
    {
        public string fallback { get; set; }
        public string color { get; set; }
        public string title { get; set; }
        public string title_link { get; set; }
        public string text { get; set; }
        public SlackField[] fields { get; set; }
    }
}