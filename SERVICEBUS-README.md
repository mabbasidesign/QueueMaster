# QueueMaster Service Bus Guide

This document explains QueueMaster messaging behavior, including retry, dead-lettering, outbox handling, and consumer idempotency.

## Components

1. Producer: OrderService outbox publisher
2. Broker: Azure Service Bus topic order-created-topic
3. Consumers:
   - PaymentService subscription payment
   - NotificationFunction subscription notification

## Current Flow

1. Order API creates order and writes OutboxEvent in the same database transaction.
2. OutboxPublisher polls unpublished events every 30 seconds.
3. Publisher sends event to Service Bus topic.
4. On successful publish, event is marked IsPublished = true.
5. PaymentService consumer checks idempotency key before processing.
6. If not processed before, consumer creates payment and stores processed message ID.
7. If duplicate, consumer skips processing and completes message safely.

## Error Handling Today

### Producer side (OrderService)

1. If publish fails, OutboxEvent remains unpublished.
2. RetryCount is incremented and LastError is updated.
3. OutboxPublisher retries again on next polling cycle.
4. Warning is logged after RetryCount >= 5, but retries still continue.

### Consumer side (PaymentService)

1. Consumer uses AutoCompleteMessages = false.
2. If payload cannot be deserialized, message is dead-lettered explicitly.
3. If message ID already exists in ProcessedMessages, consumer skips duplicate and completes message.
4. If concurrent duplicate processing hits unique key, consumer catches duplicate-key DB exception and completes message.
5. If processing throws exception, message is not completed.
6. Service Bus retries delivery automatically.
7. After entity MaxDeliveryCount is exceeded, Service Bus moves message to DLQ.

## What Happens If Message Fails

1. Publish failed before broker:
   - Event stays in OutboxEvents table
   - Publisher retries later
   - Event is not immediately lost

2. Message delivered but processing failed:
   - Message remains unsettled
   - Service Bus redelivers based on delivery policy
   - Message eventually goes to DLQ if retries are exhausted

3. Invalid message format:
   - Message goes directly to DLQ from consumer code

## Idempotency Status

Current status: implemented on PaymentService consumer.

1. Outbox prevents losing publish intent from producer side.
2. Consumer uses MessageId as primary idempotency key.
3. If MessageId is missing, consumer falls back to OrderId-based key format: order-<OrderId>.
4. Consumer stores processed IDs in ProcessedMessages table.
5. ProcessedMessages.MessageId is a primary key (unique), so duplicates are rejected at DB level.
6. Duplicate deliveries are completed safely instead of re-processing.

## Hardening

1. Add outbox max retry policy:
   - Stop retrying after N attempts
   - Move failed events to a separate terminal state
   - Alert operations

2. Improve poison message handling:
   - Dead-letter repeated business failures explicitly with reason
   - Add runbook for DLQ replay

3. Add observability:
   - Track publish failures, consumer failures, DLQ count, and retry counts
   - Alert on DLQ growth and high retry rates

## Database Changes for Idempotency

Migration added in PaymentService:

1. 20260427162431_AddProcessedMessagesIdempotency

Schema added:

1. Table: ProcessedMessages
2. Primary key: MessageId
3. Index: IX_ProcessedMessages_OrderId

## Configuration

### OrderService

File: src/OrderService/appsettings.json

ServiceBus settings:
1. Enabled
2. FullyQualifiedNamespace
3. ConnectionString
4. TopicName
5. UseManagedIdentity

### PaymentService

File: src/PaymentService/appsettings.json

ServiceBus settings:
1. Enabled
2. FullyQualifiedNamespace
3. ConnectionString
4. TopicName
5. SubscriptionName
6. UseManagedIdentity
7. MaxDeliveryCount
8. MaxConcurrentCalls

## Operational Checklist

1. Confirm ServiceBus.Enabled = true in both services in target environment.
2. Verify topic and subscriptions exist.
3. Verify managed identity or connection string permissions.
4. Verify PaymentService logs show message completion.
5. Monitor DLQ and outbox backlog.
