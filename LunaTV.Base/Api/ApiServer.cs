using TouchSocket.Rpc;
using TouchSocket.WebApi;

namespace LunaTV.Base.Api;

public class ApiServer : SingletonRpcServer
{
    [WebApi(Method = HttpMethodType.Get)]
    public int Sum(int a, int b)
    {
        return a + b;
    }
}