using Refit;

namespace LunaTV.Base.Api;

public interface IWebApi
{
    [Get("/playList")]
    Task<List<string>> GetPlayList(CancellationToken cancellationToken = default);
}