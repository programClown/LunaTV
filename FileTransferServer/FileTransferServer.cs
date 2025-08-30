using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Rpc;
using TouchSocket.WebApi;

namespace FileTransferServer;

public class FileTransferServer : SingletonRpcServer
{
    private readonly ILog m_logger;

    public FileTransferServer(ILog logger)
    {
        m_logger = logger;
    }

    [Router("/[api]/ws")]
    [Router("/[api]/[action]")]
    [WebApi(Method = HttpMethodType.Get)]
    public async Task ConnectWS(IWebApiCallContext callContext)
    {
        if (callContext.Caller is HttpSessionClient socketClient)
        {
            var result = await socketClient.SwitchProtocolToWebSocketAsync(callContext.HttpContext);
            if (!result.IsSuccess)
            {
                m_logger.Error(result.Message);
                return;
            }

            m_logger.Info("WS通过WebApi连接");
        }
    }
}