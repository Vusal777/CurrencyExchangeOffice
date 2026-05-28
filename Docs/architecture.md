# Currency Exchange Office System

## Purpose

The project is a network application developed on the .NET platform. It simulates an online currency exchange office and demonstrates WCF service creation, service consumption by client applications, integration with the National Bank of Poland API, and optional database persistence.

## Components

### ExchangeOffice.Service

Classic WCF service targeting .NET Framework 4.8. The service exposes a `basicHttpBinding` endpoint and implements the business logic of the exchange office.

Main operations:

- `Ping()` verifies communication between client and service.
- `GetCurrentRate(currencyCode)` retrieves the current exchange rate from the NBP API.
- `GetHistoricalRate(currencyCode, date)` retrieves a historical exchange rate.
- `CreateUser(fullName, email)` creates a simulated user account.
- `TopUpPln(userId, amount)` performs a virtual PLN account top-up.
- `BuyCurrency(userId, currencyCode, currencyAmount)` buys the requested amount of foreign currency using PLN.
- `SellCurrency(userId, currencyCode, currencyAmount)` sells foreign currency for PLN.
- `GetBalances(userId)` returns user currency balances.
- `GetTransactions(userId)` returns operation history.

### ExchangeOffice.ConsoleClient

Console client used to verify that a separate .NET application can consume the WCF service. It calls the service, creates a user, tops up the balance, buys USD, and prints balances and transactions.

### ExchangeOffice.WpfClient

Graphical WPF client application. It supports user account creation, PLN top-up, current and historical exchange-rate lookup, buying and selling currencies, balance display, and transaction display.

### Database

SQL Server scripts are included for optional database integration:

- `01_create_database.sql` creates `ExchangeOfficeDb` with `Users`, `Balances`, and `Transactions` tables.
- `02_seed_demo_data.sql` inserts demo data.
- `03_drop_database.sql` removes the database.

By default, the WCF service uses an in-memory repository to make classroom testing easy. To enable SQL Server persistence, run the database script and set `UseDatabase` to `true` in `ExchangeOffice.Service/Web.config`.

## Exchange Rate Source

The service uses the National Bank of Poland public API:

- Current rate endpoint: `http://api.nbp.pl/api/exchangerates/rates/a/{code}/?format=json`
- Historical rate endpoint: `http://api.nbp.pl/api/exchangerates/rates/a/{code}/{yyyy-MM-dd}/?format=json`

The service uses the NBP mid rate and applies a configurable spread. With the default `ExchangeSpreadPercent` value of `2.00`, the buy rate is 2% below the NBP mid rate and the sell rate is 2% above it.

## Data Flow

1. The client sends a SOAP request to the WCF service.
2. The WCF service validates the request.
3. For exchange-rate operations, the service calls the NBP API and maps the JSON response to DTO objects.
4. For account and transaction operations, the service updates the repository.
5. The service returns DTO objects to the client application.

## Security Notes

The course requirement states that the exchange-rate method must be accessible without authorization. Therefore the sample service does not implement authentication. In a production system, user authentication, authorization, HTTPS, audit logging, and stronger input validation would be required.
