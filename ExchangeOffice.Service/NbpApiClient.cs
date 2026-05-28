using System;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;

namespace ExchangeOffice.Service
{
    public class NbpApiClient
    {
        private const string BaseUrl = "http://api.nbp.pl/api/exchangerates/rates/a";

        public ExchangeRateDto GetCurrentRate(string currencyCode, decimal spreadPercent)
        {
            return FetchRate(currencyCode, null, spreadPercent);
        }

        public ExchangeRateDto GetHistoricalRate(string currencyCode, DateTime date, decimal spreadPercent)
        {
            return FetchRate(currencyCode, date, spreadPercent);
        }

        private ExchangeRateDto FetchRate(string currencyCode, DateTime? date, decimal spreadPercent)
        {
            currencyCode = NormalizeCurrency(currencyCode);
            if (currencyCode == "PLN")
            {
                return BuildPlnRate(spreadPercent, date ?? DateTime.Today);
            }

            var dateSegment = date.HasValue ? "/" + date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
            var url = string.Format("{0}/{1}{2}/?format=json", BaseUrl, currencyCode.ToLowerInvariant(), dateSegment);

            try
            {
                var request = WebRequest.CreateHttp(url);
                request.Accept = "application/json";
                request.UserAgent = "ExchangeOfficeCourseProject/1.0";

                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(NbpRateResponse));
                    var data = (NbpRateResponse)serializer.ReadObject(stream);
                    if (data == null || data.Rates == null || data.Rates.Length == 0)
                    {
                        throw new InvalidOperationException("The NBP API returned no exchange rate data.");
                    }

                    var rate = data.Rates[0];
                    var mid = rate.Mid;
                    return new ExchangeRateDto
                    {
                        CurrencyCode = data.Code.ToUpperInvariant(),
                        CurrencyName = data.Currency,
                        MidRate = mid,
                        BuyRate = RoundMoney(mid * (1 - spreadPercent / 100m)),
                        SellRate = RoundMoney(mid * (1 + spreadPercent / 100m)),
                        EffectiveDate = DateTime.Parse(rate.EffectiveDate, CultureInfo.InvariantCulture),
                        Source = "National Bank of Poland API"
                    };
                }
            }
            catch (WebException ex)
            {
                throw new FaultException("Could not retrieve rate for " + currencyCode + " from NBP API: " + ex.Message);
            }
        }

        private static ExchangeRateDto BuildPlnRate(decimal spreadPercent, DateTime date)
        {
            return new ExchangeRateDto
            {
                CurrencyCode = "PLN",
                CurrencyName = "polski zloty",
                MidRate = 1m,
                BuyRate = RoundMoney(1m * (1 - spreadPercent / 100m)),
                SellRate = RoundMoney(1m * (1 + spreadPercent / 100m)),
                EffectiveDate = date.Date,
                Source = "Local base currency"
            };
        }

        public static string NormalizeCurrency(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                throw new FaultException("Currency code is required.");
            }

            currencyCode = currencyCode.Trim().ToUpperInvariant();
            if (currencyCode.Length != 3)
            {
                throw new FaultException("Currency code must have exactly three letters, for example USD or EUR.");
            }

            return currencyCode;
        }

        public static decimal RoundMoney(decimal value)
        {
            return decimal.Round(value, 4, MidpointRounding.AwayFromZero);
        }

        [DataContract]
        private class NbpRateResponse
        {
            [DataMember(Name = "currency")] public string Currency { get; set; }
            [DataMember(Name = "code")] public string Code { get; set; }
            [DataMember(Name = "rates")] public NbpRate[] Rates { get; set; }
        }

        [DataContract]
        private class NbpRate
        {
            [DataMember(Name = "effectiveDate")] public string EffectiveDate { get; set; }
            [DataMember(Name = "mid")] public decimal Mid { get; set; }
        }
    }
}
