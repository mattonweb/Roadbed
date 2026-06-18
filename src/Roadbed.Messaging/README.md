# Roadbed.Messaging

Standardized message envelope structure for pub/sub messaging systems (AWS SNS, AWS SQS, Azure Service Bus, etc.).

## Overview

This library provides strongly-typed message wrappers with automatic identifier generation (ULID), timestamps, publisher tracking, and JSON serialization support. Perfect for building event-driven architectures with consistent message formats.

## Installation
```bash
dotnet add package Roadbed.Messaging
```

## Key Concepts

### Message Types

- **MessagingMessageRequest\<T\>** - Request messages sent to a system
- **MessagingMessageResponse\<T\>** - Response messages with optional link to original request
- **MessagingPublisher** - Identifies the source system/service publishing messages

All messages include:
- **Unique identifier** (ULID) - Lexicographically sortable, timestamp-based
- **Publisher information** - Who sent the message
- **Timestamps** - When created (both source and envelope)
- **Type codename** - Optional categorization
- **Typed payload** - Your data as type `T`

## Quick Start

### 1. Define Your Payload
```csharp
public class OrderCreatedPayload
{
    public int OrderId { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
}
```

### 2. Create and Send Request Message
```csharp
using System.Text.Json;
using Roadbed.Messaging;
using Roadbed.Common;

// Create publisher identifier
var publisher = new MessagingPublisher(
    new CommonBusinessKey("order-service", "OrderService"),
    "order-svc-instance-01");

// Create request message
var message = new MessagingMessageRequest<OrderCreatedPayload>(
    publisher,
    "order.created")
{
    Data = new OrderCreatedPayload
    {
        OrderId = 12345,
        CustomerName = "John Doe",
        TotalAmount = 99.99m
    }
};

// Serialize and send — always pass the shared RoadbedJson.Options so
// reads/writes share one cached options instance (per-call options are
// the #1 System.Text.Json perf footgun).
string json = JsonSerializer.Serialize(message, RoadbedJson.Options);
await snsClient.PublishAsync(topicArn, json);
```

### 3. Receive and Process Message
```csharp
// Deserialize received message
var message = JsonSerializer.Deserialize<MessagingMessageRequest<OrderCreatedPayload>>(
    json,
    RoadbedJson.Options);

Console.WriteLine($"Message ID: {message.Identifier}");
Console.WriteLine($"Type: {message.MessageTypeCodename}");
Console.WriteLine($"Publisher: {message.Publisher.Name?.Value}");
Console.WriteLine($"Order: {message.Data.OrderId}");
```

### 4. Send Response Message
```csharp
var responsePublisher = new MessagingPublisher(
    new CommonBusinessKey("fulfillment-service", "FulfillmentService"));

var response = new MessagingMessageResponse<OrderFulfilledPayload>(
    responsePublisher,
    "order.fulfilled",
    Ulid.NewUlid().ToString(),
    new OrderFulfilledPayload { OrderId = 12345, TrackingNumber = "ABC123" })
{
    OriginalRequestIdentifier = message.Identifier  // Link to original request
};

string responseJson = JsonSerializer.Serialize(response, RoadbedJson.Options);
await sqsClient.SendMessageAsync(queueUrl, responseJson);
```

## Message Structure

### JSON Format
```json
{
  "message_identifier": "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
  "message_type": "order.created",
  "publisher": {
    "publisher_identifier": "order-svc-instance-01",
    "publisher_name": {
      "key": "order-service",
      "value": "OrderService"
    }
  },
  "message_create_on": "2024-01-15T14:30:00Z",
  "source_create_on": "2024-01-15T14:29:58Z",
  "data": {
    "OrderId": 12345,
    "CustomerName": "John Doe",
    "TotalAmount": 99.99
  }
}
```

### Response Message with Link
```json
{
  "message_identifier": "01HQRS8M7NKXYZ1A2B3C4D5E6F",
  "message_type": "order.fulfilled",
  "OriginalRequestIdentifier": "01HQRS6K2MFXVW8N9PQ2T3Y4Z5",
  "publisher": {
    "publisher_identifier": "fulfillment-svc-01",
    "publisher_name": {
      "key": "fulfillment-service",
      "value": "FulfillmentService"
    }
  },
  "data": {
    "OrderId": 12345,
    "TrackingNumber": "ABC123"
  }
}
```

## Complete Example: Event-Driven Order Processing

### Request: Create Order
```csharp
public class OrderService
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicArn;
    
    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        // Process order...
        var order = await ProcessOrder(request);
        
        // Create publisher
        var publisher = new MessagingPublisher(
            new CommonBusinessKey("order-service", "OrderService"),
            Environment.MachineName);
        
        // Create message
        var message = new MessagingMessageRequest<OrderCreatedPayload>(
            publisher,
            "order.created")
        {
            Data = new OrderCreatedPayload
            {
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                Items = order.Items,
                TotalAmount = order.Total
            }
        };
        
        // Publish to SNS
        string json = JsonSerializer.Serialize(message, RoadbedJson.Options);
        await _sns.PublishAsync(_topicArn, json);
    }
}
```

### Consumer: Fulfill Order
```csharp
public class FulfillmentService
{
    private readonly IAmazonSQS _sqs;
    private readonly string _responseQueueUrl;
    
    public async Task ProcessOrderMessageAsync(string messageBody)
    {
        // Deserialize request
        var request = JsonSerializer.Deserialize<MessagingMessageRequest<OrderCreatedPayload>>(
            messageBody,
            RoadbedJson.Options);
        
        if (request?.Data == null) return;
        
        // Process fulfillment
        var tracking = await FulfillOrder(request.Data.OrderId);
        
        // Create response publisher
        var publisher = new MessagingPublisher(
            new CommonBusinessKey("fulfillment-service", "FulfillmentService"));
        
        // Create response message
        var response = new MessagingMessageResponse<OrderFulfilledPayload>(
            publisher,
            "order.fulfilled",
            Ulid.NewUlid().ToString(),
            new OrderFulfilledPayload
            {
                OrderId = request.Data.OrderId,
                TrackingNumber = tracking.Number,
                EstimatedDelivery = tracking.EstimatedDelivery
            })
        {
            OriginalRequestIdentifier = request.Identifier  // Link back to request
        };
        
        // Send response
        string responseJson = JsonSerializer.Serialize(response, RoadbedJson.Options);
        await _sqs.SendMessageAsync(_responseQueueUrl, responseJson);
    }
}
```

## Message Properties

### BaseMessagingMessage\<T\>

| Property | JSON Name | Type | Description |
|----------|-----------|------|-------------|
| `Identifier` | `message_identifier` | `string` | Unique ULID identifier |
| `MessageTypeCodename` | `message_type` | `string?` | Type categorization (e.g., "order.created") |
| `Publisher` | `publisher` | `MessagingPublisher` | Message source information |
| `Data` | `data` | `T?` | Typed payload |
| `CreatedOn` | `message_create_on` | `DateTimeOffset?` | When envelope was created (UTC) |
| `SourceCreatedOn` | `source_create_on` | `DateTimeOffset?` | When source created message (UTC) |

### MessagingMessageResponse\<T\> Additional Properties

| Property | JSON Name | Type | Description |
|----------|-----------|------|-------------|
| `OriginalRequestIdentifier` | `OriginalRequestIdentifier` | `string?` | Links response to original request |

### MessagingPublisher Properties

| Property | JSON Name | Type | Description |
|----------|-----------|------|-------------|
| `Identifier` | `publisher_identifier` | `string` | Unique instance identifier (ULID) |
| `Name` | `publisher_name` | `CommonBusinessKey?` | Service name (key + display value) |

## ULID Benefits

This library uses [ULID](https://github.com/ulid/spec) instead of GUID:

- **Sortable** - Lexicographically ordered by creation time
- **Compact** - 26 characters vs 36 for GUID
- **URL-safe** - No special characters
- **Collision-resistant** - 128-bit randomness
- **Timestamp-based** - First 48 bits encode millisecond timestamp
```csharp
// ULID: 01HQRS6K2MFXVW8N9PQ2T3Y4Z5
// GUID: 123e4567-e89b-12d3-a456-426614174000

// ULIDs sort chronologically
var ulid1 = Ulid.NewUlid();  // Created now
await Task.Delay(100);
var ulid2 = Ulid.NewUlid();  // Created 100ms later
// ulid1 < ulid2 (lexicographically)
```

## Type Codename Conventions

Use dot-notation for message type categorization:
```csharp
// Entity.Action
"order.created"
"order.updated"
"order.cancelled"

// Entity.Action.Status
"payment.processed.success"
"payment.processed.failure"

// Domain.Entity.Action
"ecommerce.order.shipped"
"inventory.product.restocked"
```

## Best Practices

1. **Always set MessageTypeCodename** - Makes message routing and filtering easier
2. **Use descriptive publisher names** - Include service name and instance ID
3. **Link responses to requests** - Set `OriginalRequestIdentifier` in responses
4. **Keep payloads simple** - Use POCOs that serialize cleanly to JSON
5. **Include timestamps** - `SourceCreatedOn` helps with event ordering
6. **Validate on receive** - Check for null Data before processing

## AWS Integration Example
```csharp
// Publishing to SNS
public async Task PublishEventAsync<T>(MessagingMessageRequest<T> message)
{
    var json = JsonSerializer.Serialize(message, RoadbedJson.Options);
    
    var request = new PublishRequest
    {
        TopicArn = _topicArn,
        Message = json,
        MessageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["MessageType"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = message.MessageTypeCodename
            }
        }
    };
    
    await _snsClient.PublishAsync(request);
}

// Consuming from SQS
public async Task<MessagingMessageRequest<T>?> ReceiveMessageAsync<T>()
{
    var response = await _sqsClient.ReceiveMessageAsync(_queueUrl);
    
    if (response.Messages.Count == 0) return null;
    
    var sqsMessage = response.Messages[0];
    var message = JsonSerializer.Deserialize<MessagingMessageRequest<T>>(
        sqsMessage.Body,
        RoadbedJson.Options);
    
    // Delete message after successful processing
    await _sqsClient.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle);
    
    return message;
}
```

## Requirements

- .NET 10.0+
- System.Text.Json (built into the runtime; serialization uses the shared `RoadbedJson.Options` from Roadbed)
- Ulid (for identifier generation)
- Roadbed (for CommonBusinessKey, CommonKeyValuePair, and the shared JSON options)

## Related Packages

- **Roadbed** - Core utilities and base classes