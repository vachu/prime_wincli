using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;

namespace NativeWebSockets_Client
{
    class Program
    {
        const int BUFF_SIZE = 10 * 1024;
        static ClientWebSocket m_ws = new ClientWebSocket();
        static CancellationTokenSource m_tokSrc = new CancellationTokenSource();

        static void Main(string[] args)
        {
            try
            {
                m_ws.Options.KeepAliveInterval = ClientWebSocket.DefaultKeepAliveInterval;
                m_ws.Options.SetBuffer(BUFF_SIZE, BUFF_SIZE);

                // open websocket connection
                OpenWebSocket(new Uri("ws://localhost:7573")).Wait();
                if (m_ws.State == WebSocketState.Open)
                {
                    //Loop:
                    //      Read console input
                    //      If input is "close", "exit" or "quit", then break from this loop
                    //
                    //      Send the input to websocket server
                    //      Receive response from websocket server
                    //      Display response on console
                    Send("List 100000").Wait();

                    var sb = new StringBuilder();
                    Recv(sb).Wait();
                    Console.WriteLine(sb.ToString());
                }
                // close websocket conection
                CloseWebSocket().Wait();
            }
            catch (AggregateException agg)
            {
                var sb = new StringBuilder();
                sb.Append(agg.Message);
                foreach (var e in agg.InnerExceptions)
                {
                    sb.Append("\n");
                    sb.Append(e.Message);
                    if (e.InnerException != null)
                    {
                        sb.Append("\n");
                        sb.Append(e.InnerException.Message);
                    }
                }
                Console.Error.WriteLine("AGGREGATE EXCEPTION: {0}", sb.ToString());
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("UNKNOWN EXCEPTION: {0}", e.Message);
            }
            Console.Error.WriteLine("Client WebSocket State: {0}", m_ws.State.ToString());
        }

        static async Task OpenWebSocket(Uri wsUri)
        {
            await m_ws.ConnectAsync(wsUri, m_tokSrc.Token);
        }

        static async Task Send(string data)
        {
            await m_ws.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                    WebSocketMessageType.Text,
                    true,
                    m_tokSrc.Token
                );
        }

        static async Task Recv(StringBuilder sb)
        {
            if (!IsWSUsable())
                return;

            var recvBuff = new ArraySegment<byte>(new byte[BUFF_SIZE]);
            var res = await m_ws.ReceiveAsync(recvBuff, m_tokSrc.Token);
            while (IsWSUsable() && res != null && res.MessageType != WebSocketMessageType.Close &&
                    res.MessageType == WebSocketMessageType.Text && res.Count > 0)
            {
                var recvBytes = recvBuff.Array;
                Array.Resize(ref recvBytes, res.Count);
                var recvMsg = Encoding.UTF8.GetString(recvBytes);
                if (res.EndOfMessage && String.CompareOrdinal(recvMsg, "==== EOT ====") == 0)
                {
                    break;
                }
                sb.Append(recvMsg);
                Array.Resize(ref recvBytes, BUFF_SIZE);
                res = await m_ws.ReceiveAsync(recvBuff, m_tokSrc.Token);
            }
        }

        static async Task CloseWebSocket()
        {
            if (!IsWSUsable())
                return;

            await m_ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", m_tokSrc.Token);
            m_ws.Dispose();
        }

        static bool IsWSUsable()
        {
            return m_ws.State == WebSocketState.Open;
        }
    }
}
