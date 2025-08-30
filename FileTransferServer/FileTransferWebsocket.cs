using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Rpc;
using TouchSocket.Sockets;
using HttpMethod = TouchSocket.Http.HttpMethod;
using TouchSocketConfig = TouchSocket.Core.TouchSocketConfig;

namespace FileTransferServer;

public class FileTransferWebsocket
{
    public static async Task<HttpService> CreateHttpService()
    {
        var service = new HttpService();
        await service.SetupAsync(new TouchSocketConfig() //加载配置
            .SetListenIPHosts(7789).ConfigureContainer(a =>
            {
                a.AddConsoleLogger();
                a.AddRpcStore(store => { store.RegisterServer<FileTransferServer>(); });
            })
            .ConfigurePlugins(a =>
            {
                a.UseWebSocket() //添加WebSocket功能
                    .SetWSUrl("/ws") //设置url直接可以连接。
                    // .SetVerifyConnection(VerifyConnection)
                    .UseAutoPong() //当收到ping报文时自动回应pong
                    ;

                a.UseWebApi();
            }));

        await service.StartAsync();

        service.Logger.Info("服务器已启动");
        service.Logger.Info("直接连接地址=>ws://127.0.0.1:7789/ws");
        service.Logger.Info("通过query连接地址=>ws://127.0.0.1:7789/wsquery?token=123456");
        service.Logger.Info("通过header连接地址=>ws://127.0.0.1:7789/wsheader"); //使用此连接时，需要在header中包含token的项
        service.Logger.Info("WebApi支持的连接地址=>ws://127.0.0.1:7789/MyServer/ConnectWS");
        service.Logger.Info("WebApi支持的连接地址=>ws://127.0.0.1:7789/MyServer/ws");

        return service;
    }

    /// <summary>
    ///     验证websocket的连接
    /// </summary>
    /// <param name="client"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static async Task<bool> VerifyConnection(IHttpSessionClient client, HttpContext context)
    {
        if (!context.Request.IsUpgrade()) //如果不包含升级协议的header，就直接返回false。
            return false;

        //使用Post连接
        if (context.Request.Method == HttpMethod.Post)
            if (context.Request.UrlEquals("/postws"))
                return true;

        if (context.Request.UrlEquals("/ws")) //以此连接，则直接可以连接
            return true;

        if (context.Request.UrlEquals("/wsquery")) //以此连接，则需要传入token才可以连接
        {
            if (context.Request.Query.Get("token") == "123456") return true;

            await context.Response
                .SetStatus(403, "token不正确")
                .AnswerAsync();
        }
        else if (context.Request.UrlEquals("/wsheader")) //以此连接，则需要从header传入token才可以连接
        {
            if (context.Request.Headers.Get("token") == "123456") return true;

            await context.Response
                .SetStatus(403, "token不正确")
                .AnswerAsync();
        }

        return false;
    }
}