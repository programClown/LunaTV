using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;

namespace LunaTV.Base.Web;

public class LunaWebsocketServer
{
    private readonly HttpService service;

    public LunaWebsocketServer()
    {
        service = new HttpService();
    }

    public List<IHttpSession> ClinetList { get; set; } = new();

    public async Task Start()
    {
        await service.SetupAsync(new TouchSocketConfig() //加载配置
            .SetListenIPHosts(4040)
            .ConfigureContainer(a => { a.AddConsoleLogger(); })
            .ConfigurePlugins(a =>
            {
                a.UseWebSocket().SetWSUrl(null).UseAutoPong();
                //a.Add<LunaWebSocketPlugin>();
                a.Add(typeof(IWebSocketHandshakedPlugin), async (IWebSocket client, HttpContextEventArgs e) =>
                {
                    ClinetList.Add(client.Client);
                    await e.InvokeNext();
                });
                a.Add(typeof(IWebSocketClosingPlugin), async (IWebSocket client, ClosedEventArgs e) =>
                {
                    ClinetList.Remove(client.Client);
                    await e.InvokeNext();
                });

                a.Add(typeof(IWebSocketReceivedPlugin), async (IWebSocket client, WSDataFrameEventArgs e) =>
                {
                    switch (e.DataFrame.Opcode)
                    {
                        case WSDataType.Close:
                        {
                            await client.CloseAsync("断开");
                        }
                            return;
                        case WSDataType.Ping:
                            await client.PongAsync(); //收到ping时，一般需要响应pong
                            break;
                        case WSDataType.Pong:
                            break;
                    }

                    await e.InvokeNext();
                });

                a.UseWebSocketReconnection(); //a.Add<MyWebSocketPlugin>();
            }));
        await service.StartAsync();
    }


    public void SendDatas()
    {
        for (var i = 0; i < 200; i++)
            Task.Run(async () =>
            {
                while (true)
                    try
                    {
                        var clientList = ClinetList.ToList();
                        for (var j = 0; j < clientList.Count; j++)
                        {
                            var sock = (HttpSessionClient)clientList[j];
                            if (sock.Online)
                                await sock.WebSocket.SendAsync(
                                    $"Dev[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff")}, 12.34, 34.56, 56.78, \"77705683\"]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("出现异常：" + ex.Message + "\r\n" + ex.StackTrace);
                    }
                    finally
                    {
                        await Task.Delay(10);
                    }
            });
    }

    public class LunaWebSocketPlugin : PluginBase, IWebSocketHandshakingPlugin, IWebSocketHandshakedPlugin,
        IWebSocketReceivedPlugin
    {
        private readonly ILog m_logger;

        public LunaWebSocketPlugin(ILog logger)
        {
            m_logger = logger;
        }

        public async Task OnWebSocketHandshaked(IWebSocket client, HttpContextEventArgs e)
        {
            m_logger.Info("WebSocket成功连接");
            await e.InvokeNext();
        }

        public async Task OnWebSocketHandshaking(IWebSocket client, HttpContextEventArgs e)
        {
            if (client.Client is IHttpSessionClient socketClient)
            {
                //服务端
                var id = socketClient.Id;
            }
            else if (client.Client is IHttpClient httpClient)
            {
                //客户端
            }

            m_logger.Info("WebSocket正在连接");
            await e.InvokeNext();
        }

        public async Task OnWebSocketReceived(IWebSocket client, WSDataFrameEventArgs e)
        {
            switch (e.DataFrame.Opcode)
            {
                case WSDataType.Close:
                {
                    await client.CloseAsync("断开");
                }
                    return;
                case WSDataType.Ping:
                    await client.PongAsync(); //收到ping时，一般需要响应pong
                    break;
                case WSDataType.Pong:
                    m_logger.Info("Pong");
                    break;
                default:
                {
                    //其他报文，需要考虑中继包的情况。所以需要手动合并 WSDataType.Cont类型的包。
                    //或者使用消息合并器
                    //获取消息组合器
                    var messageCombinator = client.GetMessageCombinator();
                    try
                    {
                        //尝试组合
                        if (messageCombinator.TryCombine(e.DataFrame, out var webSocketMessage))
                            //组合成功，必须using释放模式
                            using (webSocketMessage)
                            {
                                //合并后的消息
                                var dataType = webSocketMessage.Opcode;

                                //合并后的完整消息
                                var data = webSocketMessage.PayloadData;

                                if (dataType == WSDataType.Text)
                                {
                                    //按文本处理
                                }
                                else if (dataType == WSDataType.Binary)
                                {
                                    //按字节处理
                                }
                                //可能是其他自定义协议
                            }
                    }
                    catch (Exception ex)
                    {
                        m_logger.Exception(ex);
                        messageCombinator.Clear(); //当组合发生异常时，应该清空组合器数据
                    }
                }
                    break;
            }

            await e.InvokeNext();
        }
    }
}