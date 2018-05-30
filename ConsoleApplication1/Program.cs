using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ConsoleApplication1
{
    internal class Program
    {
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;
        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        private static void Main(string[] args)
        {
            PullMQ();
        }

        private static void PushMQ()
        {
            int i = 1;
            while (i < 10000)
            {
                int count = 0;
                var message = new MQMessage(i);
                while (!MQUtils.Push(message))
                {
                    Console.WriteLine("{0} Có lỗi khi gửi lên queue. Thử lại lần {1}", i, count++);
                    Thread.Sleep(3000);
                }
                Console.WriteLine(" [x] Sent {0}", i++);
            }
            Console.ReadKey();
        }

        private static void PullMQ()
        {
            var MQQueueName = ConfigurationManager.AppSettings["MQQueueName"];
            var factory = MQUtils.MQFactory;
            using (var conn = factory.CreateConnection())
            {
                using (RabbitMQ.Client.IModel channel = conn.CreateModel())
                {
                    IDictionary<string, object> queueArgs = new Dictionary<string, object>
                        {
                            {"x-ha-policy", "all"}
                        };
                    channel.QueueDeclare(queue: MQQueueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: queueArgs);
                    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                    while (!MQUtils.QueueEmpty())
                    {
                        if (!MQUtils.Pull(channel, ExcuteMessage))
                        {
                            Console.WriteLine("{0} - Queue is empty!", Thread.CurrentThread.ManagedThreadId);
                            Thread.Sleep(1000);
                        }
                    }
                }
            }

            Console.ReadKey();
        }

        private static bool ExcuteMessage(MQMessage msg)
        {
            if(msg != null)
            {
                Console.WriteLine(string.Format("[F1:F2] = [{0}:{1}]", msg.F1, msg.F2));
                return true;
            }
            return false;
        }
    }
}