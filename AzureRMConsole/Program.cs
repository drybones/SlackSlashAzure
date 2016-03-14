using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Sql.Models;

using Redgate.Azure.ResourceManangement;

namespace AzureRMConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var warehouses = AzureRMContext.GetDataWarehouses();
            foreach (var warehouse in warehouses)
            {
                Console.WriteLine($"{warehouse.Name}\n{warehouse.Properties.Status}\n{warehouse.Properties.ServiceObjective}\n");
            }
            Console.WriteLine("Done. Press any key to close.");
            Console.ReadKey();
        }
    }
}
