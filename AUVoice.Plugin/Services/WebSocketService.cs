using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AUVoice.Plugin.Services;

public class WebSocketService
{
    private static WebSocketService _instance;
    public static WebSocketService Instance => _instance ??= new WebSocketService();

    private ManualLogSource _logger;
    
    // Lock for client list management
    private readonly object _clientsLock = new();
    private readonly List<WebSocket> _clients = new();
    
    // Data management
    private byte[] _latestData; 
    private readonly object _dataLock = new();
    
    private CancellationTokenSource _cancellationTokenSource;
    private Task _serverTask;

    public void Initialize(ManualLogSource logger)
    {
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _serverTask = StartServer(_cancellationTokenSource.Token);
        _logger.LogInfo("WebSocket server initialized.");
    }

    public void Stop()
    {
        if (_cancellationTokenSource == null) return;
        
        _logger?.LogInfo("Stopping WebSocket server...");
        _cancellationTokenSource.Cancel();
        try
        {
            _serverTask?.Wait(2000);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Error while stopping server task: {ex.Message}");
        }
        _logger?.LogInfo("Server stopped.");
    }

    public void Broadcast(byte[] data)
    {
        if (data == null) return;

        lock (_dataLock)
        {
            if (_latestData == null || _latestData.Length != data.Length)
            {
                _latestData = new byte[data.Length];
            }
            Array.Copy(data, _latestData, data.Length);
        }
    }

    private async Task StartServer(CancellationToken cancellationToken)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:7878/");

        try
        {
            listener.Start();
            _logger.LogInfo("Listening on http://127.0.0.1:7878/");
            _ = BroadcastLoop(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                try 
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        _ = ProcessWebSocketRequest(context, cancellationToken);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Server error: {ex}");
        }
        finally
        {
            if (listener.IsListening) listener.Stop();
        }
    }

    private async Task ProcessWebSocketRequest(HttpListenerContext context, CancellationToken cancellationToken)
    {
        WebSocket webSocket = null;
        try
        {
            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
            webSocket = wsContext.WebSocket;
            
            lock (_clientsLock)
            {
                _clients.Add(webSocket);
            }
            
            _logger.LogInfo("WebSocket client connected.");
            
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // Ignore disconnect errors
        }
        finally
        {
            if (webSocket != null)
            {
                lock (_clientsLock)
                {
                    _clients.Remove(webSocket);
                }
                webSocket.Dispose();
                _logger.LogInfo("WebSocket client disconnected.");
            }
        }
    }

    private async Task BroadcastLoop(CancellationToken cancellationToken)
    {
        byte[] dataToSend = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            // 1. Get data
            lock (_dataLock)
            {
                if (_latestData != null)
                {
                    if (dataToSend == null || dataToSend.Length != _latestData.Length)
                    {
                        dataToSend = new byte[_latestData.Length];
                    }
                    Array.Copy(_latestData, dataToSend, _latestData.Length);
                }
            }

            if (dataToSend != null)
            {
                // 2. Get active sockets
                List<WebSocket> socketsSnapshot;
                lock (_clientsLock)
                {
                    socketsSnapshot = new List<WebSocket>(_clients);
                }

                if (socketsSnapshot.Count > 0)
                {
                    var buffer = new ArraySegment<byte>(dataToSend);
                    var tasks = new List<Task>(socketsSnapshot.Count);

                    foreach (var ws in socketsSnapshot)
                    {
                        if (ws.State == WebSocketState.Open)
                        {
                            tasks.Add(ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken));
                        }
                    }

                    try 
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch 
                    { 
                        // Individual socket errors are handled in their own context or ignored here
                    }
                }
            }
            
            // 16ms ~= 60fps
            await Task.Delay(16, cancellationToken); 
        }
    }
}