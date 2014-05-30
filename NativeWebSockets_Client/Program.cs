using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using ClientWebSocketExtensions;

namespace NativeWebSockets_Client
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (var ws = new ClientWebSocket())
                {
                    if (ws.Open(new Uri("ws://localhost:7573")))
                    {
                        ws.Send("List 1000000").Wait();

                        var sb = new StringBuilder();
                        ws.Recv(sb).Wait();
                        Console.WriteLine(sb.ToString());
                    }
                    ws.Close();
                }
            }
            catch (AggregateException agg)
            {
                var sb = new StringBuilder();
                sb.Append(agg.Message);
                foreach (var e in agg.InnerExceptions)
                {
                    sb.Append("; ");
                    sb.Append(e.Message);
                }
                Console.Error.WriteLine("AGGREGATE EXCEPTION: {0}", sb.ToString());
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("UNKNOWN EXCEPTION: {0}", e.Message);
            }
        }
    }
}
