using System;
using System.ServiceModel;

namespace ExchangeOffice.ConsoleClient
{
    internal static class Program
    {
        private static void Main()
        {
            var factory = new ChannelFactory<IExchangeOfficeService>("ExchangeOfficeEndpoint");
            var client = factory.CreateChannel();

            try
            {
                Console.WriteLine(client.Ping());

                var usd = client.GetCurrentRate("USD");
                Console.WriteLine("USD mid: {0}, buy: {1}, sell: {2}, date: {3:yyyy-MM-dd}", usd.MidRate, usd.BuyRate, usd.SellRate, usd.EffectiveDate);

                var user = client.CreateUser("Demo Student", "student@example.com", "demo1234");
                Console.WriteLine("Created user #{0}: {1}", user.Id, user.FullName);

                client.TopUpPln(user.Id, 1000m);
                var buy = client.BuyCurrency(user.Id, "USD", 200m);
                Console.WriteLine("Bought {0} {1} for {2} PLN at rate {3}", buy.CurrencyAmount, buy.CurrencyCode, buy.PlnAmount, buy.Rate);

                Console.WriteLine("Balances:");
                foreach (var balance in client.GetBalances(user.Id))
                {
                    Console.WriteLine(" - {0}: {1}", balance.CurrencyCode, balance.Amount);
                }

                Console.WriteLine("Transactions:");
                foreach (var transaction in client.GetTransactions(user.Id))
                {
                    Console.WriteLine(" - {0}: {1} {2}, PLN {3}", transaction.Type, transaction.CurrencyAmount, transaction.CurrencyCode, transaction.PlnAmount);
                }

                ((IClientChannel)client).Close();
                factory.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                ((IClientChannel)client).Abort();
                factory.Abort();
            }

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }
    }
}
