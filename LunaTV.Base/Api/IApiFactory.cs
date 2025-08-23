using Refit;

namespace LunaTV.Base.Api;

public interface IApiFactory
{
    public T CreateRefitClient<T>(Uri baseAddress);

    public T CreateRefitClient<T>(Uri baseAddress, RefitSettings refitSettings);
}