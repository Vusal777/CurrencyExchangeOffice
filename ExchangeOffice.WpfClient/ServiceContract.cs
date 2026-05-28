using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ExchangeOffice.WpfClient
{
    [ServiceContract(Namespace = "http://exchangeoffice/course")]
    public interface IExchangeOfficeService
    {
        [OperationContract] string Ping();
        [OperationContract] ExchangeRateDto GetCurrentRate(string currencyCode);
        [OperationContract] ExchangeRateDto GetHistoricalRate(string currencyCode, DateTime date);
        [OperationContract] UserDto CreateUser(string fullName, string email, string password);
        [OperationContract] UserDto LoginUser(string email, string password);
        [OperationContract] IList<UserDto> GetUsers();
        [OperationContract] BalanceDto TopUpPln(int userId, decimal amount);
        [OperationContract] IList<BalanceDto> GetBalances(int userId);
        [OperationContract] TransactionDto BuyCurrency(int userId, string currencyCode, decimal currencyAmount);
        [OperationContract] TransactionDto SellCurrency(int userId, string currencyCode, decimal currencyAmount);
        [OperationContract] IList<TransactionDto> GetTransactions(int userId);
    }

    [DataContract(Namespace = "http://exchangeoffice/course")]
    public class ExchangeRateDto
    {
        [DataMember] public string CurrencyCode { get; set; }
        [DataMember] public string CurrencyName { get; set; }
        [DataMember] public decimal MidRate { get; set; }
        [DataMember] public decimal BuyRate { get; set; }
        [DataMember] public decimal SellRate { get; set; }
        [DataMember] public DateTime EffectiveDate { get; set; }
        [DataMember] public string Source { get; set; }
    }

    [DataContract(Namespace = "http://exchangeoffice/course")]
    public class UserDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string FullName { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public DateTime CreatedAt { get; set; }
    }

    [DataContract(Namespace = "http://exchangeoffice/course")]
    public class BalanceDto
    {
        [DataMember] public int UserId { get; set; }
        [DataMember] public string CurrencyCode { get; set; }
        [DataMember] public decimal Amount { get; set; }
    }

    [DataContract(Namespace = "http://exchangeoffice/course")]
    public class TransactionDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public int UserId { get; set; }
        [DataMember] public string Type { get; set; }
        [DataMember] public string CurrencyCode { get; set; }
        [DataMember] public decimal CurrencyAmount { get; set; }
        [DataMember] public decimal PlnAmount { get; set; }
        [DataMember] public decimal Rate { get; set; }
        [DataMember] public DateTime CreatedAt { get; set; }
    }
}
