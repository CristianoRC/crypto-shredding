using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Orders.Api.Services;

public record CustomerInfo(Guid Id, string Name, string Document, string Address);

public record CustomerLookupResult(bool Found, string Status, CustomerInfo? Customer);

public class CustomersClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private record CustomerResponse(string Status, CustomerInfo? Decrypted);

    public async Task<CustomerLookupResult> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/customers/{customerId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new CustomerLookupResult(false, "notfound", null);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions, cancellationToken);
        return new CustomerLookupResult(true, payload?.Status ?? "shredded", payload?.Decrypted);
    }
}
