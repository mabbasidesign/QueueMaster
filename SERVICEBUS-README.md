# QueueMaster Service Bus Guide

This document explains QueueMaster messaging behavior, including retry, dead-lettering, outbox handling, and idempotency gaps.

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
5. PaymentService consumer reads from subscription and creates payment record.

## Error Handling Today

### Producer side (OrderService)

1. If publish fails, OutboxEvent remains unpublished.
2. RetryCount is incremented and LastError is updated.
3. OutboxPublisher retries again on next polling cycle.
4. Warning is logged after RetryCount >= 5, but retries still continue.

### Consumer side (PaymentService)

1. Consumer uses AutoCompleteMessages = false.
2. If payload cannot be deserialized, message is dead-lettered explicitly.
3. If processing throws exception, message is not completed.
4. Service Bus retries delivery automatically.
5. After entity MaxDeliveryCount is exceeded, Service Bus moves message to DLQ.

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

Current status: partial, not complete.

1. Outbox prevents losing publish intent from producer side.
2. Payment consumer does not currently enforce idempotency key.
3. Duplicate delivery can create duplicate payment rows.

## Hardening

1. Add idempotency key in consumer:
   - Use MessageId or business key like OrderId
   - Store processed message IDs
   - Reject duplicates safely

2. Add outbox max retry policy:
   - Stop retrying after N attempts
   - Move failed events to a separate terminal state
   - Alert operations

3. Improve poison message handling:
   - Dead-letter repeated business failures explicitly with reason
   - Add runbook for DLQ replay

4. Add observability:
   - Track publish failures, consumer failures, DLQ count, and retry counts
   - Alert on DLQ growth and high retry rates

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
