using RestSharp;
using Newtonsoft.Json.Linq;

namespace e_Vent.tools;

public class Paystack
{
    private readonly string _secretKey;
    private readonly string payStackUrl = "https://api.paystack.co";

    public Paystack()
    {
        _secretKey = Environment.GetEnvironmentVariable("PAYSTACK_SECRET_KEY") ?? throw new InvalidOperationException("Set your Paystack Secret Key in the .env file, Reference the README.md for more information");
    }

    public async Task<JObject> InitializeTransaction(string email, int amountInKobo, string callbackUrl)
    {
        var client = new RestClient(payStackUrl);
        var request = new RestRequest("/transaction/initialize", Method.Post);
        request.AddHeader("Authorization", $"Bearer {_secretKey}");
        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            email = email,
            amount = amountInKobo,
            callback_url = callbackUrl
        };

        request.AddJsonBody(body);

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Error initializing transaction: {response.Content}");
        }

        return JObject.Parse(response.Content!);
    }

    public async Task<JObject> VerifyPayment(string reference)
    {
        var client = new RestClient(payStackUrl);
        var request = new RestRequest($"transaction/verify/{reference}", Method.Get);
        request.AddHeader("Authorization", $"Bearer {_secretKey}");

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Error initializing transaction: {response.Content}");
        }
        return JObject.Parse(response.Content!);
    }
}