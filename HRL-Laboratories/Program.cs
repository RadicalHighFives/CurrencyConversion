using System.Net;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Configuration;


public class CurrencyConversion {

    // ===== Exchange rate data structure
    private Dictionary<string, double> exchangeRates;

    public CurrencyConversion() {

        exchangeRates = LoadFromSQLDatabase();
    }

    // ===== Convert Currency
    public decimal ConvertCurrency(string currencyFrom, string currencyTo, decimal amount) {


        // ===== Error checking/Validation 
        if (!exchangeRates.ContainsKey(currencyFrom) || !exchangeRates.ContainsKey(currencyTo)) {
            throw new ArgumentException("Invalid currency codes.");
        }
        double rateFrom = exchangeRates[currencyFrom];
        double rateTo = exchangeRates[currencyTo];

        decimal convertedResult = (amount * (decimal)rateFrom) / (decimal)rateTo;
        return convertedResult;
    }

    // ===== Load exchange rates from Database
    private Dictionary<string, double> LoadFromSQLDatabase()
    {
        // ===== Add DB connection string here!
        Dictionary<string, double> rates = new Dictionary<string, double>();
        string connectionString = "CONNECTION_STRING_HERE";

        string sqlQuery = "SELECT CurrencyCode, ExchangeRate FROM WebAppDb.dbo.Currency";
        using (SqlConnection connection = new SqlConnection(connectionString)) {

            SqlCommand command = new SqlCommand(sqlQuery, connection);
            try {
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string currencyCode = reader["CurrencyCode"].ToString();
                    double exchangeRate = Convert.ToDouble(reader["ExchangeRate"]);
                    rates[currencyCode] = exchangeRate;
                }
                reader.Close();
            }
            catch (Exception ex) {
                throw new Exception("Error loading exchange rates from the database: " + ex.Message);
            }
        }

        return rates;
    }

    // ===== Get Exchange rate for a specific currency
    public double GetExchangeRate(string specificCurrencyCode) {

        if (!exchangeRates.ContainsKey(specificCurrencyCode)) {
            throw new ArgumentException("Currency Code does not exist. ");
        }
        return exchangeRates[specificCurrencyCode];
    }

    // ================================================================================
    // (CRUD)
    // ================================================================================

    // ===== Create Exchange Rate 
    public void CreateExchangeRate(string currencyCode, double exchangeRate)
    {
        Dictionary<string, double> rates = new Dictionary<string, double>();
        string connectionString = "CONNECTION_STRING_HERE";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {

            string query = "INSERT INTO dbo.Currency (CurrencyCode, ExchangeRate) VALUES (@CurrencyCode, @ExchangeRate)";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CurrencyCode", currencyCode);
            command.Parameters.AddWithValue("@ExchangeRate", exchangeRate);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating exchange rate record! " + ex.Message);
            }
        }
    }

    // ===== Read the most recent exchange rate for a currency from the database
    public double ReadExchangeRate(string currencyCode)
    {
        string connectionString = "CONNECTION_STRING_HERE";
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            string query = "SELECT TOP 1 ExchangeRate FROM dbo.Currency WHERE CurrencyCode = @CurrencyCode";
            // SELECT TOP 1 ExchangeRate FROM dbo.Currency WHERE CurrencyCode = @CurrencyCode ORDER BY Timestamp DESC
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CurrencyCode", currencyCode);

            try
            {
                connection.Open();
                object result = command.ExecuteScalar();
                if (result != null)
                {
                    return Convert.ToDouble(result);
                }
                else
                {
                    throw new Exception("Exchange rate not found for the specified currency.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading exchange rate record! " + ex.Message);
            }
        }
    }

    // ===== Update an existing exchange rate record in the database
    public void UpdateExchangeRate(string currencyCode, double newExchangeRate)
    {
        string connectionString = "CONNECTION_STRING_HERE";
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            string query = "UPDATE dbo.Currency SET ExchangeRate = @NewExchangeRate WHERE CurrencyCode = @CurrencyCode";
            // UPDATE dbo.Currency SET ExchangeRate = @NewExchangeRate, Timestamp = GETDATE() WHERE CurrencyCode = @CurrencyCode";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@NewExchangeRate", newExchangeRate);
            command.Parameters.AddWithValue("@CurrencyCode", currencyCode);

            try
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new Exception("Exchange rate not found for the specified currency.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating exchange rate record! " + ex.Message);
            }
        }
    }

    // ===== Delete an exchange rate record from the database (not recommended for historical data)
    public void DeleteExchangeRate(string currencyCode)
    {
        string connectionString = "CONNECTION_STRING_HERE";
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            string query = "DELETE FROM dbo.Currency WHERE CurrencyCode = @CurrencyCode";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CurrencyCode", currencyCode);

            try
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new Exception("Exchange rate not found for the specified currency.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting exchange rate record: " + ex.Message);
            }
        }
    }

    // ================================================================================
    // Function to GET exchange rates from a public API
    // ================================================================================
    private List<ExchangeRateApiResponse> GetExchangeRatesFromApi()
    {
        List<ExchangeRateApiResponse> rates = new List<ExchangeRateApiResponse>();

        try
        {
            // ===== MOCK URL for the exchange rate API
            string endPoint = "https://mocki.io/v1/7b65f16c-072a-453c-a538-a694d7be0ccc";
         
            using (WebClient client = new WebClient())
            {
                string json = client.DownloadString(endPoint);
                var apiResponse = JsonConvert.DeserializeObject<List<ExchangeRateApiResponse>>(json);
                Console.WriteLine(apiResponse);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error getting exchange rates from API! " + ex.Message);
        }

        return rates;
    }

    // ===== Unit Tests in Class
    public static void UnitTests(){


        CurrencyConversion currencyConversion = new CurrencyConversion();

        var currentPHPExchange = currencyConversion.ReadExchangeRate("PHP");
        Console.WriteLine("Current exchange rate for PHP is: " + currentPHPExchange);

        decimal usdConversion = currencyConversion.ConvertCurrency("USD", "PHP", 1000);
        Console.WriteLine("1000 PHP to USD is: " + usdConversion);

        decimal phpConversion = currencyConversion.ConvertCurrency("PHP", "USD", 10000);
        Console.WriteLine("10,000 USD to PHP is: " + phpConversion);

        //currencyConversion.CreateExchangeRate("MXN", 17.19);
        //Console.WriteLine("MXN: " + currencyConversion.ReadExchangeRate("MXN"));

        //currencyConversion.UpdateExchangeRate("PHP", 56.81);
        //Console.WriteLine("PHP: " + currencyConversion.ReadExchangeRate("PHP"));

        //currencyConversion.GetExchangeRatesFromApi();

    }
}

// ===== Model for the API
public class ExchangeRateApiResponse {

    public int CurrencyID { get; set; }

    public string CurrencyCode { get; set; }

    public double ExchangeRate { get; set; }
}

class Program {

    static void Main(string[] args) {
        CurrencyConversion.UnitTests();
    }
}