using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace LunaTV.Utils;

public class HtmlServerProxy : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _rootDirectory;

    /// <summary>
    ///     实例化
    /// </summary>
    /// <param name="serviceIp">代理的ip地址带端口,例子：http://localhost:8080/ </param>
    /// <param name="folderPath">需要代理的文件夹，例子：AppDomain.CurrentDomain.BaseDirectory + "offline-exam-player" </param>
    public HtmlServerProxy(string serviceIp, string folderPath)
    {
        _rootDirectory = folderPath;

        _listener = new HttpListener();
        _listener.Prefixes.Add(serviceIp);
    }

    public void Dispose()
    {
        Stop();
        ((IDisposable)_listener).Dispose();
    }

    public async Task Start()
    {
        _listener.Start();

        await Task.Run(ProcessRequests);
    }

    public void Stop()
    {
        _listener.Stop();
        _listener.Close();
        Console.WriteLine("Proxy server stopped.");
    }

    private void ProcessRequests()
    {
        try
        {
            while (_listener.IsListening)
            {
                var context = _listener.GetContext();
                var requestPath = context.Request.Url?.AbsolutePath;
                var filePath = _rootDirectory + requestPath;

                // Serve the requested file if it exists
                if (File.Exists(filePath))
                {
                    var extension = Path.GetExtension(filePath);
                    string contentType;
                    switch (extension)
                    {
                        case ".html":
                            contentType = "text/html";
                            break;
                        case ".js":
                            contentType = "application/javascript";
                            break;
                        case ".less":
                        case ".css":
                            contentType = "text/css";
                            break;
                        case ".svg":
                            contentType = "image/svg+xml";
                            break;
                        default:
                            contentType = "application/octet-stream";
                            break;
                    }

                    context.Response.ContentType = contentType;
                    //context.Response.ContentType = "text/html";
                    var responseBuffer = File.ReadAllBytes(filePath);
                    context.Response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                    context.Response.Close();
                }
                else
                {
                    // Return a 404 response if the file does not exist
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that may occur
            Console.WriteLine(ex.ToString());
        }
    }
}