using Robot; // 需要这个 using 来访问 LogWindow
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using LitJson;
using UnityEngine;

public class RobotModeTcpListener : MonoBehaviour
{
    public static event Action<string> OnRobotModeReceived;

    private TcpListener _server;
    private Thread _listenerThread;
    private bool _isRunning;
    private const int Port = 13580; // 为Robot Mode选择一个新端口

    void Start()
    {
        // 向App内的Log面板发送一条消息，证明脚本已启动
        LogWindow.Info($"[RobotModeTcpListener] Starting server on port {Port}...");
        StartServer();
    }

    void OnDestroy()
    {
        StopServer();
    }

    private void StartServer()
    {
        _listenerThread = new Thread(ListenForClients);
        _listenerThread.IsBackground = true;
        _listenerThread.Start();
        _isRunning = true;
    }

    private void StopServer()
    {
        _isRunning = false;
        if (_server != null)
        {
            _server.Stop();
        }
        if (_listenerThread != null && _listenerThread.IsAlive)
        {
            _listenerThread.Join();
        }
        LogWindow.Info("[RobotModeTcpListener] Server stopped.");
    }

    private void ListenForClients()
    {
        try
        {
            _server = new TcpListener(IPAddress.Any, Port);
            _server.Start();

            // 使用主线程Dispatcher来打印日志，确保线程安全
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                LogWindow.Info($"[RobotModeTcpListener] Server is now listening on port {Port}.");
            });

            while (_isRunning)
            {
                if (!_server.Pending())
                {
                    Thread.Sleep(100); // 避免CPU空转
                    continue;
                }
                TcpClient client = _server.AcceptTcpClient();

                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    LogWindow.Info("[RobotModeTcpListener] Client connected.");
                });

                Thread clientThread = new Thread(HandleClientComm);
                clientThread.IsBackground = true;
                clientThread.Start(client);
            }
        }
        catch (SocketException ex)
        {
            if (_isRunning)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    LogWindow.Error($"[RobotModeTcpListener] SocketException: {ex.Message}");
                });
            }
        }
        catch (Exception ex)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                LogWindow.Error($"[RobotModeTcpListener] Exception: {ex.Message}");
            });
        }
    }

    private void HandleClientComm(object clientObj)
    {
        using (TcpClient client = (TcpClient)clientObj)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string receivedString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        LogWindow.Info($"[RobotModeTcpListener] Received: {receivedString}");
                    });

                    try
                    {
                        JsonData json = JsonMapper.ToObject(receivedString);
                        if (json.ContainsKey("functionName") && json["functionName"].ToString() == "robotMode" && json.ContainsKey("value"))
                        {
                            string mode = json["value"].ToString();
                            OnRobotModeReceived?.Invoke(mode);
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            LogWindow.Error($"[RobotModeTcpListener] JSON parsing error: {jsonEx.Message}");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    LogWindow.Warn($"[RobotModeTcpListener] Client connection error: {ex.Message}");
                });
            }
        }
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            LogWindow.Info("[RobotModeTcpListener] Client disconnected.");
        });
    }
}