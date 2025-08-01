using Google.Protobuf;
using System.Net.Sockets;
using System.Net;
using Mymmorpg;
using System.Text;

namespace GameServer.tools
{
    public class ResponseHelper
    {
        /// <summary>
        /// 发送业务响应（messageType = 0x02）
        /// </summary>
        public static void SendResponse(ApiResponse response, QueuedSender sender)
        {
            Send(response, 0x02, sender); // 业务响应统一使用类型 0x02

            Console.WriteLine("[ResponseHelper] 异步排队发送响应: " + (response is IMessage ? response.GetType().Name : response.ToString()));
        }

        /// <summary>
        /// 泛型发送方法，支持自定义 messageType
        /// </summary>
        public static void Send(object messageObj, byte messageType, QueuedSender sender)
        {
            byte[] bodyBytes = SerializeResponse(messageObj);

            // 添加消息类型前缀
            byte[] fullBody = new byte[1 + bodyBytes.Length];
            fullBody[0] = messageType;
            Buffer.BlockCopy(bodyBytes, 0, fullBody, 1, bodyBytes.Length);

            // 添加 4 字节长度前缀（包含 messageType）
            byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(fullBody.Length));
            byte[] fullPacket = new byte[4 + fullBody.Length];
            Buffer.BlockCopy(lengthPrefix, 0, fullPacket, 0, 4);
            Buffer.BlockCopy(fullBody, 0, fullPacket, 4, fullBody.Length);

            sender.Enqueue(fullPacket); // 使用发送队列异步发送

            Console.WriteLine($"[ResponseHelper] 已发送，类型=0x{messageType:X2}, 长度={fullPacket.Length}");
        }

        // 辅助方法，序列化响应对象为字节数组
        private static byte[] SerializeResponse(object response)
        {
            if (response is IMessage responseMessage)
            {
                return responseMessage.ToByteArray();
            }
            else
            {
                // 对非 protobuf 对象做简单封装
                string str = response.ToString();
                if (!str.StartsWith("ERROR:") && !str.StartsWith("OK:"))
                {
                    str = "ERROR:" + str;
                }
                return Encoding.UTF8.GetBytes(str);
            }
        }
    }
}