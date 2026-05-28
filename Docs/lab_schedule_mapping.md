# Lab Schedule Mapping

## Lab 1

- WCF service project: `ExchangeOffice.Service`
- Simple service method: `Ping()`
- Separate console client: `ExchangeOffice.ConsoleClient`
- Service consumption: `ChannelFactory<IExchangeOfficeService>` in the console client

## Labs 2-4

- Currency-code parameter: `GetCurrentRate(string currencyCode)`
- Public access without authorization: default `basicHttpBinding` endpoint
- NBP API integration: `NbpApiClient`
- Supported examples: `USD`, `EUR`, `GBP`

## Labs 5-14

- Architecture and service design: service contract and DTO classes
- Exchange logic: `BuyCurrency` and `SellCurrency`
- NBP integration: current and historical exchange-rate methods
- WPF client: `ExchangeOffice.WpfClient`
- User account management: `CreateUser`
- Transactions: `Transactions` table and `GetTransactions`
- Database integration: SQL scripts and `SqlExchangeOfficeRepository`
- Balances: `Balances` table and `GetBalances`
- Historical exchange rates: `GetHistoricalRate`
- Testing and debugging support: console client plus documented WPF demo flow

## Lab 15

The project can be presented by showing:

1. The WCF service contract.
2. A current rate retrieved from the NBP API.
3. User creation in WPF.
4. PLN top-up.
5. Buy and sell operations.
6. Balance and transaction history.
7. Optional SQL Server persistence.
