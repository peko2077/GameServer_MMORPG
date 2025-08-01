using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace GameServer.tools
{
    public class QueuedSender : IDisposable
    {
        private readonly Queue<byte[]> sendQueue = new Queue<byte[]>();
        private readonly AutoResetEvent sendEvent = new AutoResetEvent(false);
        private readonly object queueLock = new object();

        private readonly Stream stream; // 发送用的 Stream
        private readonly object sendLock = new object(); // 发送锁，避免流写入冲突

        private Thread sendThread;
        private volatile bool isSending = false;

        public event Action<Exception> OnSendError; // 发送异常回调

        public QueuedSender(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        // 启动发送线程
        public void Start()
        {
            if (isSending) return;

            isSending = true;
            sendThread = new Thread(SendLoop);
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        // 停止发送线程，等待退出
        public void Stop()
        {
            if (!isSending) return;

            isSending = false;
            sendEvent.Set(); // 唤醒线程退出
            sendThread?.Join(2000);
        }

        // 入队一个消息，等待发送
        public void Enqueue(byte[] data)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("发送数据不能为空");

            lock (queueLock)
            {
                sendQueue.Enqueue(data);
            }
            sendEvent.Set();
        }

        private void SendLoop()
        {
            while (isSending)
            {
                sendEvent.WaitOne();

                while (true)
                {
                    byte[] data = null;
                    lock (queueLock)
                    {
                        if (sendQueue.Count > 0)
                            data = sendQueue.Dequeue();
                        else
                            break;
                    }

                    if (data != null)
                    {
                        try
                        {
                            lock (sendLock)
                            {
                                if (stream == null)
                                    throw new IOException("发送流已关闭");

                                stream.Write(data, 0, data.Length);
                                stream.Flush();
                            }
                        }
                        catch (Exception ex)
                        {
                            OnSendError?.Invoke(ex);
                            isSending = false; // 出错停止发送
                            break;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
            sendEvent.Dispose();
        }
    }
}