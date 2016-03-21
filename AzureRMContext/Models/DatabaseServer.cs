namespace Redgate.Azure.ResourceManagement.Models
{
    public class DatabaseServer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Version { get; set; }
        public ResourceGroup ResourceGroup { get; set; }
    }
}