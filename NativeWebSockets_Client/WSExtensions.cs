using System;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClientWebSocketExtensions
{
    public static class CmdLineTextExtensions
    {
        const int WAIT_TIME = 15 * 1000; // 15 secs
        const int BUFF_SIZE = 10 * 1000; // 10K buffer

        public static bool Open(this ClientWebSocket ws, Uri wsUri)
        {
            using (var tokSrc = new CancellationTokenSource())
            {
                ws.Options.SetBuffer(BUFF_SIZE, BUFF_SIZE);
                ws.Options.KeepAliveInterval = ClientWebSocket.DefaultKeepAliveInterval;
                return (
                        ws.ConnectAsync(wsUri, tokSrc.Token).Wait(WAIT_TIME) &&
                        ws.State == WebSocketState.Open
                    );
            }
        }

        public static void Close(this ClientWebSocket ws)
        {
            if (!ws.IsOpen())
                return;

            using (var tokSrc = new CancellationTokenSource())
            {
                ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Client Closure", tokSrc.Token).Wait();
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client Closure", tokSrc.Token).Wait();
            }
        }

        public static async Task Send(this ClientWebSocket ws, string data)
        {
            using (var tokSrc = new CancellationTokenSource())
            {
                await ws.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                            WebSocketMessageType.Text,
                            true,
                            tokSrc.Token
                        );
            }
        }

        public static async Task Recv(this ClientWebSocket ws, StringBuilder sb)
        {
            if (!ws.IsOpen())
                return;

            using (var tokSrc = new CancellationTokenSource())
            {
                var recvBuff = new ArraySegment<byte>(new byte[BUFF_SIZE]);
                var res = await ws.ReceiveAsync(recvBuff, tokSrc.Token);
                while (ws.IsOpen() && res != null &&
                        res.MessageType != WebSocketMessageType.Close &&
                        res.MessageType == WebSocketMessageType.Text && res.Count > 0)
                {
                    var recvBytes = recvBuff.Array;
                    Array.Resize(ref recvBytes, res.Count);
                    var recvMsg = Encoding.UTF8.GetString(recvBytes);
                    sb.Append(recvMsg);
                    if (res.EndOfMessage)
                    {
                        break;
                    }
                    Array.Resize(ref recvBytes, BUFF_SIZE);
                    res = await ws.ReceiveAsync(recvBuff, tokSrc.Token);
                }
            }
        }

        public static IEnumerable<string> RecvNextChunk(this ClientWebSocket ws)
        {
            if (!ws.IsOpen())
                yield break;

            using (var tokSrc = new CancellationTokenSource())
            {
                var recvBuff = new ArraySegment<byte>(new byte[BUFF_SIZE]);
                var res = ws.ReceiveAsync(recvBuff, tokSrc.Token).Result;
                while (ws.IsOpen() && res != null &&
                        res.MessageType != WebSocketMessageType.Close &&
                        res.MessageType == WebSocketMessageType.Text && res.Count > 0)
                {
                    var recvBytes = recvBuff.Array;
                    Array.Resize(ref recvBytes, res.Count);
                    var recvMsg = Encoding.UTF8.GetString(recvBytes);
                    yield return recvMsg;

                    if (res.EndOfMessage)
                    {
                        yield break;
                    }
                    Array.Resize(ref recvBytes, BUFF_SIZE);
                    res = ws.ReceiveAsync(recvBuff, tokSrc.Token).Result;
                }
            }
        }

        public static bool IsOpen(this ClientWebSocket ws)
        {
            return ws.State == WebSocketState.Open;
        }
    }
}
