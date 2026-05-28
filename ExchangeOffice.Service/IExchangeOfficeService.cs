using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace ExchangeOffice.Service
{
    [ServiceContract(Namespace = "http://exchangeoffice/course")]
    public interface IExchangeOfficeService
    {
        [OperationContract]
        string Ping();

        [OperationContract]
        ExchangeRateDto GetCurrentRate(string currencyCode);

        [OperationContract]
        ExchangeRateDto GetHistoricalRate(string currencyCode, DateTime date);

        [OperationContract]
        UserDto CreateUser(string fullName, string email, string password);

        [OperationContract]
        UserDto LoginUser(string email, string password);

        [OperationContract]
        IList<UserDto> GetUsers();

        [OperationContract]
        BalanceDto TopUpPln(int userId, decimal amount);

        [OperationContract]
        IList<BalanceDto> GetBalances(int userId);

        [OperationContract]
        TransactionDto BuyCurrency(int userId, string currencyCode, decimal currencyAmount);

        [OperationContract]
        TransactionDto SellCurrency(int userId, string currencyCode, decimal currencyAmount);

        [OperationContract]
        IList<TransactionDto> GetTransactions(int userId);
    }
}
