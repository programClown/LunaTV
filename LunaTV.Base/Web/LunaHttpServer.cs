using LunaTV.Base.Api;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Rpc;
using TouchSocket.Sockets;

namespace LunaTV.Base.Web;

public class LunaHttpServer
{
    private readonly HttpService _httpService = new();

    public async Task Start(int port = 7799)
    {
        await _httpService.SetupAsync(new TouchSocketConfig() //加载配置
            .SetListenIPHosts(new IPHost(port))
            .ConfigureContainer(a =>
            {
                a.AddConsoleLogger();
                a.AddRpcStore(store => { store.RegisterServer<ApiServer>(); });
            })
            .ConfigurePlugins(a =>
            {
                a.UseTcpSessionCheckClear();
                a.UseWebApi();

                //此插件是http的兜底插件，应该最后添加。作用是当所有路由不匹配时返回404.且内部也会处理Option请求。可以更好的处理来自浏览器的跨域探测。
                a.UseDefaultHttpServicePlugin();
            })
        );

        _ = _httpService.StartAsync(); // 异步运行（避免阻塞UI线程）
    }

    public async Task Stop()
    {
        await _httpService.StopAsync();
    }
}

public class LunaHttpStaticPageServer
{
    private readonly HttpService _httpService = new();

    public async Task Start(string staticFolder, int port = 7799)
    {
        await _httpService.SetupAsync(new TouchSocketConfig() //加载配置
            .SetListenIPHosts(new IPHost(port))
            .ConfigureContainer(a => { a.AddConsoleLogger(); })
            .ConfigurePlugins(a =>
            {
                a.UseTcpSessionCheckClear();
                a.UseHttpStaticPage() //添加静态页面文件夹
                    .SetNavigateAction(request =>
                    {
                        if (request.RelativeURL.EndsWith("/")) return $"{request.RelativeURL}/index.html";

                        //此处可以设置重定向
                        return request.RelativeURL;
                    })
                    .AddFolder(staticFolder);

                //此插件是http的兜底插件，应该最后添加。作用是当所有路由不匹配时返回404.且内部也会处理Option请求。可以更好的处理来自浏览器的跨域探测。
                a.UseDefaultHttpServicePlugin();
            })
        );

        _ = _httpService.StartAsync(); // 异步运行（避免阻塞UI线程）
    }

    public async Task Stop()
    {
        await _httpService.StopAsync();
    }
}