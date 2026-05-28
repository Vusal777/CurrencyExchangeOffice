# User Manual

## Opening the Project

1. Open `CurrencyExchangeOffice.sln` in Visual Studio.
2. Make sure the projects target .NET Framework 4.8.
3. Start `ExchangeOffice.ServiceHost`. This console application hosts the WCF service.
4. If you use another service URL, copy the address and update the endpoint address in:
   - `ExchangeOffice.ConsoleClient/App.config`
   - `ExchangeOffice.WpfClient/App.config`

The default client endpoint is:

```text
http://localhost:8080/ExchangeOfficeService.svc
```

## Running Without Database

The service works without database setup by default. In this mode, user accounts, balances, and transactions are stored in memory and are reset when the service restarts.

In `ExchangeOffice.Service/Web.config`:

```xml
<add key="UseDatabase" value="false" />
```

## Running With Database

1. Open SQL Server Management Studio or Visual Studio SQL Server Object Explorer.
2. Execute `Database/01_create_database.sql`.
3. Optionally execute `Database/02_seed_demo_data.sql`.
4. In `ExchangeOffice.Service/Web.config`, set:

```xml
<add key="UseDatabase" value="true" />
```

5. Check the connection string:

```xml
Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=ExchangeOfficeDb;Integrated Security=True
```

## Console Client Test

Run `ExchangeOffice.ConsoleClient` after `ExchangeOffice.ServiceHost` is running. Expected behavior:

1. The client prints a service ping response.
2. It retrieves the current USD rate.
3. It creates a demo user with password `demo1234`.
4. It tops up the PLN balance.
5. It buys USD.
6. It prints balances and transaction history.

## WPF Client Test

Run `ExchangeOffice.WpfClient` after `ExchangeOffice.ServiceHost` is running.

Recommended demo flow:

1. Click `Connect`.
2. Enter full name, email, and password.
3. Click `Create user`, or select an existing user, enter the password, and click `Login`.
4. Check the current `USD` rate.
5. Top up PLN with `1000`.
6. Buy `100` USD by selecting `USD`, entering `100`, and clicking `Buy`.
7. Refresh balances and transactions.
8. Check a historical rate, for example `2024-01-02`.
