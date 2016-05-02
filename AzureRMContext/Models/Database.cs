using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redgate.Azure.ResourceManagement.Models
{
    public class Database
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Edition { get; set; }
        public string ServiceObjective { get; set; }
        public string Status { get; set; }
        public DatabaseServer DatabaseServer { get; set; }
        public IDictionary<string, string> Tags { get; set; }
    }
}
