using Refit;

namespace LunaTV.Base.Api;

public class WebApiManager : IWebApiManager
{
    private IWebApi? _client;

    public IWebApi Client
    {
        get
        {
            // Return the existing client if it exists
            if (_client != null)
            {
                return _client;
            }

            // Create a new client and store it otherwise
            _client = CreateClient();
            return _client;
        }
    }

    private readonly IHttpClientFactory _httpClientFactory;
    public RefitSettings? RefitSettings { get; init; }
    public string? BaseUrl { get; set; }

    public WebApiManager(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public void ResetClient()
    {
        _client = null;
    }

    private IWebApi CreateClient()
    {
        BaseUrl = "http://localhost:7860";

        var httpClient = _httpClientFactory.CreateClient("A3Client");
        httpClient.BaseAddress = new Uri(BaseUrl);
        var api = RestService.For<IWebApi>(httpClient, RefitSettings);
        return api;
    }
}