using System;
using System.ServiceModel;
using ExchangeOffice.Service;

namespace ExchangeOffice.ServiceHost
{
    internal static class Program
    {
        private static void Main()
        {
            using (var host = new System.ServiceModel.ServiceHost(typeof(ExchangeOfficeService)))
            {
                try
                {
                    host.Open();
                    Console.WriteLine("Exchange Office WCF service is running.");
                    Console.WriteLine("Service URL: http://localhost:8080/ExchangeOfficeService.svc");
                    Console.WriteLine("Press ENTER to stop the service.");
                    Console.ReadLine();
                    host.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not start service: " + ex.Message);
                    Console.WriteLine("If this is an access error, run Visual Studio as Administrator or reserve the URL with netsh.");
                    host.Abort();
                    Console.ReadLine();
                }
            }
        }
    }
}
