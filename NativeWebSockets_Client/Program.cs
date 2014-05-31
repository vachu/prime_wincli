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
                        CmdRspLoop(ws);
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

        static void CmdRspLoop(ClientWebSocket ws)
        {
            if (!ws.IsOpen())
            {
                Console.Error.WriteLine(
                    "WARNING: WebSocket not unusable; state = {0}",
                    ws.State.ToString()
                );
                return;
            }

            for (var cmdStr = "?";
                 !String.IsNullOrWhiteSpace(cmdStr);
                 cmdStr = Console.ReadLine() )
            {
                cmdStr = cmdStr.Trim().ToUpper();
                switch (cmdStr)
                {
                    case "QUIT":
                    case "EXIT":
                    case "CLOSE":
                        return;
                }

                ws.Send(cmdStr).Wait();
                //---- Getting the full response ----
                var sb = new StringBuilder();
                ws.Recv(sb).Wait();
                Console.WriteLine(sb.ToString());
                
                // ---- Get chunked response through iterator ----
                //foreach (var res in ws.RecvNextChunk())
                //{
                //    Console.Write(res);
                //}
                //Console.WriteLine();

                Console.Write("Enter Command: ");
            }
            Console.Error.WriteLine("WARNING: empty command input");
        }
    }
}
