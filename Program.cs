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
    private static Dictionary<WebSocket, string> userNames = new(); // Mapping WebSocket to username
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

        // Receive username first
        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string initialMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

        // Handle username assignment
        if (initialMessage.StartsWith("USERNAME:"))
        {
            string username = initialMessage.Substring(9); // Extract username
            userNames[socket] = username;
            Console.WriteLine($"[New Connection] Username assigned: {username}");
        }
        else
        {
            await SendMessage(socket, "[Error] Username required. Please send 'USERNAME:<your_username>'.");
            return;
        }

        // Process command to create or join a channel
        string channelId = null;
        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string command = Encoding.UTF8.GetString(buffer, 0, result.Count);

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

        // Handle messages while in the channel
        while (socket.State == WebSocketState.Open && channelId != null)
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                string username = userNames[socket];  // Get the username for the current socket
                string messageWithUsername = $"{username}: {message}";

                Console.WriteLine($"[Channel {channelId}] {messageWithUsername}");
                foreach (var client in channels[channelId])
                {
                    if (client != socket && client.State == WebSocketState.Open)
                    {
                        await SendMessage(client, messageWithUsername);
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
