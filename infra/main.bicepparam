using 'main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')
param notificationConnectionString = readEnvironmentVariable('NOTIFICATION_CONNECTION_STRING', '')
param notificationSenderAddress = readEnvironmentVariable('NOTIFICATION_SENDER_ADDRESS', '')
param notificationRecipientAddress = readEnvironmentVariable('NOTIFICATION_RECIPIENT_ADDRESS', '')
