# Currency Exchange Office System

## Course

Network Application Development

## Project Title

Currency Exchange Office System

## Author

Name: **YOUR NAME HERE**  
Student ID: **YOUR STUDENT ID HERE**

## Short Description

This project is a network-based currency exchange office application built on the .NET platform. It uses a WCF Web Service for business logic and a WPF desktop client for user interaction.

The system allows users to:

- create an account and log in with a password,
- check current and historical currency exchange rates,
- retrieve exchange rates from the National Bank of Poland API,
- top up a virtual PLN balance,
- buy and sell currencies,
- view balances with PLN equivalents,
- view transaction history,
- calculate total account value in selected currencies.

## Repository Structure

```text
CurrencyExchangeOffice
|
|-- ExchangeOffice.Service
|   |-- WCF service contract, DTOs, NBP API client, repositories, and business logic
|
|-- ExchangeOffice.ServiceHost
|   |-- Console host used to run the WCF service locally
|
|-- ExchangeOffice.WpfClient
|   |-- WPF desktop client application
|
|-- ExchangeOffice.ConsoleClient
|   |-- Console client for basic service communication testing
|
|-- Database
|   |-- SQL Server scripts for database schema and demo data
|
|-- Docs
|   |-- Architecture description, user manual, and lab schedule mapping
|
|-- CurrencyExchangeOffice.sln
|   |-- Visual Studio solution file
```

## Technologies Used

- C#
- .NET Framework 4.8
- Windows Communication Foundation (WCF)
- WPF
- SQL Server / LocalDB scripts
- National Bank of Poland public API

## How to Run the Project

1. Open `CurrencyExchangeOffice.sln` in Visual Studio.
2. Build the solution.
3. Start `ExchangeOffice.ServiceHost` first.
4. Keep the service host console window open.
5. Start `ExchangeOffice.WpfClient`.
6. The WPF client connects to the service automatically when it opens.

Default service address:

```text
http://localhost:8080/ExchangeOfficeService.svc
```

If Windows blocks the service URL, run Visual Studio as Administrator or reserve the URL with:

```cmd
netsh http add urlacl url=http://+:8080/ExchangeOfficeService.svc/ user=%USERDOMAIN%\%USERNAME%
```

## Database Mode

By default, the service uses in-memory storage, so it can be tested without setting up SQL Server.

To use the database:

1. Run `Database/01_create_database.sql`.
2. Optionally run `Database/02_seed_demo_data.sql`.
3. In the service configuration, set:

```xml
<add key="UseDatabase" value="true" />
```

## Main Service Features

- `GetCurrentRate`
- `GetHistoricalRate`
- `CreateUser`
- `LoginUser`
- `TopUpPln`
- `BuyCurrency`
- `SellCurrency`
- `GetBalances`
- `GetTransactions`

## Moodle Submission

Submit the public GitHub repository link in Moodle.

Optional comment:

```text
The project includes a WCF service, WPF client, console test client, SQL database scripts, NBP API integration, and documentation.
```

