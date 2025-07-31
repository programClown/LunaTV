using Refit;

namespace LunaTV.Base.Api;

public interface IWebApiManager
{
    IWebApi Client { get; }
    RefitSettings? RefitSettings { get; init; }
    string? BaseUrl { get; set; }
    void ResetClient();
}