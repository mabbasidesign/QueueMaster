using 'main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')
param functionLocation = readEnvironmentVariable('AZURE_FUNCTION_LOCATION', 'canadaeast')
param notificationRecipientAddresses = readEnvironmentVariable('NOTIFICATION_RECIPIENT_ADDRESSES', 'mabbasidesign@yahoo.com,mabbasidesign2016@gmail.com')
param apimPublisherEmail = readEnvironmentVariable('APIM_PUBLISHER_EMAIL', 'mabbasidesign@yahoo.com')
param apimPublisherName = readEnvironmentVariable('APIM_PUBLISHER_NAME', 'QueueMaster')
param apimSkuName = readEnvironmentVariable('APIM_SKU_NAME', 'Developer')
param orderServiceBackendUrl = readEnvironmentVariable('ORDER_SERVICE_BACKEND_URL', 'https://orderservice-dev.azurewebsites.net')
param paymentServiceBackendUrl = readEnvironmentVariable('PAYMENT_SERVICE_BACKEND_URL', 'https://paymentservice-dev.azurewebsites.net')
param notificationDataLocation = readEnvironmentVariable('NOTIFICATION_DATA_LOCATION', 'United States')
param notificationSenderUsername = readEnvironmentVariable('NOTIFICATION_SENDER_USERNAME', 'donotreply')
param notificationSenderDisplayName = readEnvironmentVariable('NOTIFICATION_SENDER_DISPLAY_NAME', 'QueueMaster Notifications')
