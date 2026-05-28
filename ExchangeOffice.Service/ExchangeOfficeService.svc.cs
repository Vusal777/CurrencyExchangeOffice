using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;

namespace ExchangeOffice.Service
{
    public class ExchangeOfficeService : IExchangeOfficeService
    {
        private readonly NbpApiClient _nbpClient = new NbpApiClient();
        private readonly IExchangeOfficeRepository _repository = CreateRepository();

        public string Ping()
        {
            return "Exchange office service is running.";
        }

        public ExchangeRateDto GetCurrentRate(string currencyCode)
        {
            return _nbpClient.GetCurrentRate(currencyCode, GetSpreadPercent());
        }

        public ExchangeRateDto GetHistoricalRate(string currencyCode, DateTime date)
        {
            if (date.Date > DateTime.Today)
            {
                throw new FaultException("Historical rate date cannot be in the future.");
            }

            return _nbpClient.GetHistoricalRate(currencyCode, date.Date, GetSpreadPercent());
        }

        public UserDto CreateUser(string fullName, string email, string password)
        {
            try
            {
                return _repository.CreateUser(fullName, email, password);
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        public UserDto LoginUser(string email, string password)
        {
            try
            {
                return _repository.LoginUser(email, password);
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        public IList<UserDto> GetUsers()
        {
            try
            {
                return _repository.GetUsers();
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        public BalanceDto TopUpPln(int userId, decimal amount)
        {
            if (amount <= 0m) throw new FaultException("Top-up amount must be greater than zero.");

            try
            {
                var balance = _repository.AddToBalance(userId, "PLN", amount);
                _repository.AddTransaction(new TransactionDto
                {
                    UserId = userId,
                    Type = "TOP_UP",
                    CurrencyCode = "PLN",
                    CurrencyAmount = amount,
                    PlnAmount = amount,
                    Rate = 1m
                });
                return balance;
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        public IList<BalanceDto> GetBalances(int userId)
        {
            try
            {
                return _repository.GetBalances(userId);
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        public TransactionDto BuyCurrency(int userId, string currencyCode, decimal currencyAmount)
        {
            if (currencyAmount <= 0m) throw new FaultException("Currency amount must be greater than zero.");

            currencyCode = NbpApiClient.NormalizeCurrency(currencyCode);
            if (currencyCode == "PLN") throw new FaultException("Buying PLN with PLN is not supported.");

            try
            {
                _repository.GetUser(userId);
                var rate = GetCurrentRate(currencyCode);
                var plnAmount = NbpApiClient.RoundMoney(currencyAmount * rate.SellRate);

                _repository.AddToBalance(userId, "PLN", -plnAmount);
                _repository.AddToBalance(userId, currencyCode, currencyAmount);

                return _repository.AddTransaction(new TransactionDto
                {
                    UserId = userId,
                    Type = "BUY",
                    CurrencyCode = currencyCode,
                    CurrencyAmount = currencyAmount,
                    PlnAmount = plnAmount,
                    Rate = rate.SellRate
                });
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        public TransactionDto SellCurrency(int userId, string currencyCode, decimal currencyAmount)
        {
            if (currencyAmount <= 0m) throw new FaultException("Currency amount must be greater than zero.");

            currencyCode = NbpApiClient.NormalizeCurrency(currencyCode);
            if (currencyCode == "PLN") throw new FaultException("Selling PLN to PLN is not supported.");

            try
            {
                _repository.GetUser(userId);
                var rate = GetCurrentRate(currencyCode);
                var plnAmount = NbpApiClient.RoundMoney(currencyAmount * rate.BuyRate);

                _repository.AddToBalance(userId, currencyCode, -currencyAmount);
                _repository.AddToBalance(userId, "PLN", plnAmount);

                return _repository.AddTransaction(new TransactionDto
                {
                    UserId = userId,
                    Type = "SELL",
                    CurrencyCode = currencyCode,
                    CurrencyAmount = currencyAmount,
                    PlnAmount = plnAmount,
                    Rate = rate.BuyRate
                });
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        public IList<TransactionDto> GetTransactions(int userId)
        {
            try
            {
                return _repository.GetTransactions(userId);
            }
            catch (Exception ex)
            {
                throw ToFault(ex);
            }
        }

        private static IExchangeOfficeRepository CreateRepository()
        {
            var useDatabase = string.Equals(ConfigurationManager.AppSettings["UseDatabase"], "true", StringComparison.OrdinalIgnoreCase);
            return useDatabase ? (IExchangeOfficeRepository)new SqlExchangeOfficeRepository() : new MemoryExchangeOfficeRepository();
        }

        private static decimal GetSpreadPercent()
        {
            decimal value;
            return decimal.TryParse(ConfigurationManager.AppSettings["ExchangeSpreadPercent"], out value) ? value : 2m;
        }

        private static FaultException ToFault(Exception ex)
        {
            return ex as FaultException ?? new FaultException(ex.Message);
        }
    }
}
