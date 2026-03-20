using 'main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')
param functionLocation = readEnvironmentVariable('AZURE_FUNCTION_LOCATION', 'canadaeast')
param notificationRecipientAddresses = readEnvironmentVariable('NOTIFICATION_RECIPIENT_ADDRESSES', 'mabbasidesign@yahoo.com,mabbasidesign2016@gmail.com')
param notificationDataLocation = readEnvironmentVariable('NOTIFICATION_DATA_LOCATION', 'United States')
param notificationSenderUsername = readEnvironmentVariable('NOTIFICATION_SENDER_USERNAME', 'donotreply')
param notificationSenderDisplayName = readEnvironmentVariable('NOTIFICATION_SENDER_DISPLAY_NAME', 'QueueMaster Notifications')
