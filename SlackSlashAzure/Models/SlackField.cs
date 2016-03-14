using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SlackSlashAzure.Models
{
    [DataContract]
    public class SlackField
    {
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string value { get; set; }
        [DataMember(Name = "short")]
        public bool IsShort {get;set;}
    }
}