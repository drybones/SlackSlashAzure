using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Redgate.Azure.ResourceManagement;

namespace AzureRMConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var warehouses = AzureRMContext.GetAllDataWarehouses();
            foreach (var warehouse in warehouses)
            {
                Console.WriteLine($"{warehouse.Name}\n{warehouse.Status}\n{warehouse.ServiceObjective}\n{warehouse.SqlServerName}\n{warehouse.ResourceGroupName}\n");
                if (warehouse.Tags != null)
                {
                    foreach (var t in warehouse.Tags)
                    {
                        Console.WriteLine($"{t.Key}: {t.Value}");
                    }
                }
            }
            Console.WriteLine("\nDone. Press any key to close.");
            Console.ReadKey();
        }
    }
}
