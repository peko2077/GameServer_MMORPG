using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GameServer;
using GameServer.controller;
using GameServer.tools;
using Google.Protobuf;
using Mymmorpg;  // .proto 生成的命名空间

namespace GameServer
{
    // 客户端状态信息
    public class ClientState
    {
        public TcpClient Client { get; set; }                       // 客户端连接对象
        public NetworkStream Stream { get; set; }                   // 与客户端通信的流
        public QueuedSender Sender { get; set; }                    // 排队发送工具类
        public DateTime LastHeartbeat { get; set; } = DateTime.Now; // 最后一次心跳时间，初始化为当前时间
    }


    class Server
    {
        private TcpListener listener;                                           // 用于监听传入的客户端连接
        private ConcurrentDictionary<TcpClient, ClientState> clients = new();   // 存储所有连接的客户端，线程安全
        private ControllerManage controllerManage = new ControllerManage();     // 控制器管理对象，用于分发业务处理

        // 启动服务器
        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);    // 创建监听器，监听任意IP地址上的指定端口
            listener.Start();                                   // 启动监听
            Console.WriteLine($"服务器已启动，监听端口: {port}");

            // 启动心跳检查线程
            new Thread(HeartbeatCheckLoop) { IsBackground = true }.Start();

            // 主循环：持续接受客户端连接
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();                      // 阻塞，直到有客户端连接
                Console.WriteLine("客户端已连接: " + client.Client.RemoteEndPoint);

                var stream = client.GetStream();    // 获取网络流

                // 创建客户端状态对象
                var state = new ClientState
                {
                    Client = client,
                    Stream = stream,
                    Sender = new QueuedSender(client.GetStream()),  // 关联流
                    LastHeartbeat = DateTime.Now    // 设置初始心跳时间
                };

                // 初始化 QueuedSender
                state.Sender = new QueuedSender(stream);
                state.Sender.OnSendError += (ex) =>
                {
                    Console.WriteLine($"[发送失败] {ex.Message}");
                    CleanupClient(client);
                };
                state.Sender.Start();

                clients.TryAdd(client, state);      // 添加到客户端字典中

                // 启动客户端处理线程
                new Thread(() => HandleClient(state)) { IsBackground = true }.Start();
            }
        }

        // 心跳检查线程
        private void HeartbeatCheckLoop()
        {
            while (true)
            {
                foreach (var kvp in clients)
                {
                    var state = kvp.Value;

                    // 如果超过 60 秒未收到心跳，关闭连接
                    if ((DateTime.Now - state.LastHeartbeat).TotalSeconds > 60)
                    {
                        Console.WriteLine($"客户端心跳超时，关闭连接: {state.Client.Client.RemoteEndPoint}");
                        CleanupClient(state.Client);
                    }
                    else
                    {
                        SendPing(state);    // 向客户端发送 ping
                    }

                }
                Thread.Sleep(60000);        // 每 60 秒检查一次
            }
        }

        // 向客户端发送 ping 消息以检测连接
        private void SendPing(ClientState state)
        {
            try
            {
                Heartbeat ping = new Heartbeat { Type = "ping" };
                byte[] protoBytes = ping.ToByteArray();

                // 1 字节类型 + Protobuf 数据
                byte[] message = new byte[1 + protoBytes.Length];
                message[0] = 0x01; // 类型 0x01 代表心跳
                Array.Copy(protoBytes, 0, message, 1, protoBytes.Length);

                byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(message.Length));
                byte[] full = lengthPrefix.Concat(message).ToArray();

                state.Sender.Enqueue(full); // 排队发送
            }
            catch
            {
                // 如果发送失败，说明客户端可能掉线，清理连接
                CleanupClient(state.Client);
            }
        }

        // 清理客户端
        private void CleanupClient(TcpClient client)
        {
            if (clients.TryRemove(client, out var state))
            {
                try
                {
                    state.Sender?.Dispose();    // 停止发送器线程
                    state.Stream.Close();       // 关闭流
                    client.Close();             // 关闭 TCP 连接
                }
                catch { }

                Console.WriteLine("客户端连接已关闭");
            }
        }

        // 处理客户端的业务逻辑
        private void HandleClient(ClientState state)
        {
            var helper = new ConnectionHelper(state.Stream); // 使用封装好的 helper
            while (true)
            {
                try
                {
                    // 每次尝试接收一条完整消息
                    if (!helper.TryReceiveMessage(out byte messageType, out byte[] requestBytes))
                    {
                        // 如果没有读到任何数据或连接关闭，就清理并退出
                        Console.WriteLine("客户端关闭连接或读取失败");
                        CleanupClient(state.Client);
                        break;
                    }

                    switch (messageType)
                    {
                        case 0x01: // 心跳
                            try
                            {
                                var heartbeat = Heartbeat.Parser.ParseFrom(requestBytes);
                                if (heartbeat.Type == "pong")
                                {
                                    state.LastHeartbeat = DateTime.Now;
                                    Console.WriteLine("收到 pong");
                                }
                                else if (heartbeat.Type == "ping")
                                {
                                    Console.WriteLine("收到 ping，回复 pong");

                                    Heartbeat pong = new Heartbeat { Type = "pong" };
                                    byte[] pongBytes = pong.ToByteArray();

                                    byte[] message = new byte[1 + pongBytes.Length];
                                    message[0] = 0x01; // 心跳类型
                                    Array.Copy(pongBytes, 0, message, 1, pongBytes.Length);

                                    byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(message.Length));
                                    byte[] full = lengthPrefix.Concat(message).ToArray();

                                    state.Sender.Enqueue(full); // 回复 pong
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("心跳解析失败: " + ex.Message);
                            }
                            break;

                        case 0x02: // 业务请求
                            try
                            {
                                ApiRequest request = ApiRequest.Parser.ParseFrom(requestBytes);
                                controllerManage.HandleRequest(request, state);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[Program.cs] [Server] 业务数据解析失败: " + ex.Message);
                                Console.WriteLine(ex.StackTrace);  // 打印堆栈跟踪，找出具体位置
                            }
                            break;

                        default:
                            Console.WriteLine($"未知消息类型: {messageType}");
                            break;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("处理客户端异常: " + e.Message);
                    CleanupClient(state.Client);
                    break;
                }
            }
        }

    }

    // 主程序入口
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();   // 创建服务器实例
            server.Start(12345);            // 启动服务器，监听端口 12345
        }
    }
}