using Refit;

namespace LunaTV.Base.Api;

public interface IWebApi
{
    [Get("/j/search_tags?type=movie")]
    Task<string> GetPlayList(CancellationToken cancellationToken = default);
}