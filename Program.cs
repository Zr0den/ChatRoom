using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

class Program
{
    private static Dictionary<string, List<WebSocket>> channels = new();
    private static Dictionary<string, string> inviteKeys = new();
    private static Random random = new();

    static async Task Main()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/");
        listener.Start();
        Console.WriteLine("=== ChatRoom Server ===");
        Console.WriteLine("Server started at ws://localhost:5000/");
        Console.WriteLine("Waiting for connections...\n");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                _ = HandleClient(wsContext.WebSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    static async Task HandleClient(WebSocket socket)
    {
        byte[] buffer = new byte[1024];
        await SendMessage(socket, "Welcome to the chat!\nCommands: 'create' to make a channel, 'join <invite-key>' to join one.");

        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string command = Encoding.UTF8.GetString(buffer, 0, result.Count);

        string channelId = null;
        if (command.StartsWith("create"))
        {
            channelId = Guid.NewGuid().ToString();
            string inviteKey = GenerateInviteKey();
            channels[channelId] = new List<WebSocket> { socket };
            inviteKeys[inviteKey] = channelId;
            await SendMessage(socket, $"\n[System] Channel created! Invite key: {inviteKey}\n");
        }
        else if (command.StartsWith("join"))
        {
            string[] parts = command.Split(' ');
            if (parts.Length == 2 && inviteKeys.TryGetValue(parts[1], out channelId) && channels.ContainsKey(channelId))
            {
                channels[channelId].Add(socket);
                await SendMessage(socket, "\n[System] Joined channel! You can now chat.\n");
            }
            else
            {
                await SendMessage(socket, "[Error] Invalid invite key.");
                return;
            }
        }
        else
        {
            await SendMessage(socket, "[Error] Invalid command.");
            return;
        }

        while (socket.State == WebSocketState.Open && channelId != null)
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"[Channel {channelId}] {message}");
                foreach (var client in channels[channelId])
                {
                    if (client != socket && client.State == WebSocketState.Open)
                    {
                        await SendMessage(client, message);
                    }
                }
            }
        }
    }

    static async Task SendMessage(WebSocket socket, string message)
    {
        byte[] msgBytes = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(new ArraySegment<byte>(msgBytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    static string GenerateInviteKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] key = new char[6];
        for (int i = 0; i < key.Length; i++)
        {
            key[i] = chars[random.Next(chars.Length)];
        }
        return new string(key);
    }
}
