using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace GameServer.tools
{
    public class ConnectionHelper
    {
        private const int MAX_ALLOWED_LENGTH = 1024 * 1024; // 1MB最大消息大小
        private NetworkStream stream;
        private List<byte> recvBuffer = new List<byte>();

        public ConnectionHelper(NetworkStream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// 尝试从流中读取一条完整消息（长度+类型+protobuf体）
        /// 返回是否成功读取一条消息，messageType 和 messageBody 作为输出参数
        /// </summary>
        public bool TryReceiveMessage(out byte messageType, out byte[] messageBody)
        {
            messageType = 0;
            messageBody = Array.Empty<byte>();

            try
            {
                // 从流中读取数据（阻塞等待）
                if (stream.DataAvailable || stream.CanRead)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("[ConnectionHelper] 连接关闭");
                        return false;
                    }
                    recvBuffer.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));
                }

                while (recvBuffer.Count >= 4)
                {
                    // 拿长度字段，避免每次ToArray的开销
                    byte[] lengthBytes = new byte[4];
                    recvBuffer.CopyTo(0, lengthBytes, 0, 4);
                    int totalLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

                    if (totalLength <= 1 || totalLength > MAX_ALLOWED_LENGTH)
                    {
                        Console.WriteLine("[ConnectionHelper] 长度非法: " + totalLength);
                        // 只丢弃4字节，尝试继续解析后续数据
                        recvBuffer.RemoveRange(0, 4);
                        return false;
                    }

                    int fullPacketLength = 4 + totalLength;
                    if (recvBuffer.Count < fullPacketLength)
                    {
                        // 消息不完整，等待下一次接收更多数据
                        break;
                    }

                    // 解析消息类型和消息体
                    messageType = recvBuffer[4];
                    int bodyLength = totalLength - 1;
                    messageBody = recvBuffer.GetRange(5, bodyLength).ToArray();

                    // 移除已经解析的字节
                    recvBuffer.RemoveRange(0, fullPacketLength);

                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[ConnectionHelper] 接收请求时出错: " + e);
                return false;
            }

            return false;
        }
    }
}