using LunaTV.Base.Constants;
using Refit;

namespace LunaTV.Base.Api;

public class ApiFactory : IApiFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    public RefitSettings? RefitSettings { get; init; }

    public ApiFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public T CreateRefitClient<T>(Uri baseAddress)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(T));

        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent.GetRandomUserAgent());
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        httpClient.BaseAddress = baseAddress;
        return RestService.For<T>(httpClient, RefitSettings);
    }

    public T CreateRefitClient<T>(Uri baseAddress, RefitSettings refitSettings)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(T));
        httpClient.BaseAddress = baseAddress;
        List<string> headers =
        [
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15"
        ];
        Random random = new Random();
        httpClient.DefaultRequestHeaders.Add("User-Agent", headers[random.Next(0, headers.Count)]);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        return RestService.For<T>(httpClient, refitSettings);
    }
}