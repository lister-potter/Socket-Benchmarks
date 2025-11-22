using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using EchoServer.Models;
using EchoServer.Repositories;

namespace EchoServer.Services;

public interface IMessageRouter
{
    Task RouteMessageAsync(string message, WebSocket webSocket, string clientId);
}

public class MessageRouter : IMessageRouter
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IBidService _bidService;
    private readonly ILotRepository _lotRepository;

    public MessageRouter(
        ISubscriptionService subscriptionService,
        IBidService bidService,
        ILotRepository lotRepository)
    {
        _subscriptionService = subscriptionService;
        _bidService = bidService;
        _lotRepository = lotRepository;
    }

    public async Task RouteMessageAsync(string message, WebSocket webSocket, string clientId)
    {
        // Try to parse as auction message first
        // Check if it looks like an auction message by checking for "type" field
        try
        {
            using var doc = JsonDocument.Parse(message);
            if (doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                
                // If it has a "type" field that matches auction message types, try to deserialize
                if (type == "JoinLot" || type == "PlaceBid")
                {
                    // These are incoming auction messages - MUST process, never echo
                    Console.WriteLine($"Received auction message type: {type} from client {clientId}");
                    var auctionMessage = AuctionMessage.Deserialize(message);
                    if (auctionMessage != null)
                    {
                        Console.WriteLine($"Successfully deserialized {type} message, processing...");
                        await HandleAuctionMessageAsync(auctionMessage, webSocket, clientId);
                        Console.WriteLine($"Finished processing {type} message for client {clientId}");
                        return;
                    }
                    else
                    {
                        // Deserialization failed but we know it's an auction message
                        // Send error response instead of echoing
                        Console.WriteLine($"Failed to deserialize auction message with type: {type}");
                        Console.WriteLine($"Message was: {message.Substring(0, Math.Min(200, message.Length))}");
                        await SendErrorAsync(webSocket, $"Invalid auction message format");
                        return;
                    }
                }
                else if (type == "LotUpdate" || type == "Error")
                {
                    // These are server responses - ignore (shouldn't be received by server)
                    // But if we receive them, don't echo them
                    return;
                }
            }
        }
        catch (JsonException ex)
        {
            // Invalid JSON - echo it back (might be a non-auction message)
            Console.WriteLine($"Invalid JSON received: {ex.Message}");
            await EchoMessageAsync(message, webSocket);
            return;
        }
        catch (Exception ex)
        {
            // Other errors during routing - log but don't crash
            // Only echo if we're sure it's not an auction message
            Console.WriteLine($"Error routing message: {ex.Message}");
            Console.WriteLine($"Message was: {message.Substring(0, Math.Min(200, message.Length))}");
            
            // Try to check if it's an auction message before echoing
            try
            {
                using var checkDoc = JsonDocument.Parse(message);
                if (checkDoc.RootElement.TryGetProperty("type", out var typeCheck))
                {
                    var msgType = typeCheck.GetString();
                    if (msgType == "PlaceBid" || msgType == "JoinLot")
                    {
                        // Don't echo auction messages - send error instead
                        await SendErrorAsync(webSocket, "Error processing auction message");
                        return;
                    }
                }
            }
            catch
            {
                // Can't parse - echo it back
                await EchoMessageAsync(message, webSocket);
                return;
            }
            
            await EchoMessageAsync(message, webSocket);
            return;
        }

        // Default to echo behavior for non-auction messages only
        await EchoMessageAsync(message, webSocket);
    }

    private async Task HandleAuctionMessageAsync(AuctionMessage message, WebSocket webSocket, string clientId)
    {
        switch (message)
        {
            case JoinLotMessage joinLot:
                await HandleJoinLotAsync(joinLot, webSocket, clientId);
                break;

            case PlaceBidMessage placeBid:
                await HandlePlaceBidAsync(placeBid, webSocket, clientId);
                break;

            default:
                await SendErrorAsync(webSocket, "Unknown message type");
                break;
        }
    }

    private async Task HandleJoinLotAsync(JoinLotMessage message, WebSocket webSocket, string clientId)
    {
        var lot = await _lotRepository.GetLotAsync(message.LotId);
        if (lot == null)
        {
            await SendErrorAsync(webSocket, "Lot not found");
            return;
        }

        _subscriptionService.Subscribe(message.LotId, clientId, webSocket);

        // Send current lot state
        var update = new LotUpdateMessage
        {
            LotId = lot.LotId,
            CurrentBid = lot.CurrentBid,
            CurrentBidder = lot.CurrentBidder,
            Status = lot.Status.ToString()
        };
        await SendMessageAsync(webSocket, update);
    }

    private async Task HandlePlaceBidAsync(PlaceBidMessage message, WebSocket webSocket, string clientId)
    {
        Console.WriteLine($"Processing PlaceBid: lot={message.LotId}, bidder={message.BidderId}, amount={message.Amount}");
        try
        {
            var result = await _bidService.PlaceBidAsync(message.LotId, message.BidderId, message.Amount);
            
            if (result.Success)
            {
                Console.WriteLine($"Bid succeeded, sending LotUpdate for lot {message.LotId}");
                var update = new LotUpdateMessage
                {
                    LotId = result.UpdatedLot!.LotId,
                    CurrentBid = result.UpdatedLot.CurrentBid,
                    CurrentBidder = result.UpdatedLot.CurrentBidder,
                    Status = result.UpdatedLot.Status.ToString()
                };
                await SendMessageAsync(webSocket, update);
                Console.WriteLine($"Sent LotUpdate to client {clientId}");
            }
            else
            {
                Console.WriteLine($"Bid failed: {result.ErrorMessage}, sending Error to client {clientId}");
                await SendErrorAsync(webSocket, result.ErrorMessage ?? "Bid failed");
                Console.WriteLine($"Sent Error to client {clientId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in HandlePlaceBidAsync: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await SendErrorAsync(webSocket, $"Error processing bid: {ex.Message}");
        }
    }

    private async Task EchoMessageAsync(string message, WebSocket webSocket)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }

    private async Task SendMessageAsync(WebSocket webSocket, LotUpdateMessage message)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var context = AuctionMessageJsonContext.Default;
                var json = JsonSerializer.Serialize(message, context.LotUpdateMessage);
                Console.WriteLine($"Serialized LotUpdate: {json}");
                var bytes = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                Console.WriteLine($"Successfully sent LotUpdate message ({bytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SendMessageAsync: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            Console.WriteLine($"Cannot send LotUpdate - WebSocket state is {webSocket.State}");
        }
    }

    private async Task SendErrorAsync(WebSocket webSocket, string errorMessage)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var error = new ErrorMessage { Message = errorMessage };
                var context = AuctionMessageJsonContext.Default;
                var json = JsonSerializer.Serialize(error, context.ErrorMessage);
                Console.WriteLine($"Serialized Error: {json}");
                var bytes = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                Console.WriteLine($"Successfully sent Error message ({bytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SendErrorAsync: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            Console.WriteLine($"Cannot send Error - WebSocket state is {webSocket.State}");
        }
    }
}

