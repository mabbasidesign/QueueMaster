param(
  [Parameter(Mandatory = $true)]
  [string]$ResourceGroup,

  [string]$FunctionAppName,

  [string]$ProjectPath = "src/NotificationFunction/NotificationFunction.csproj"
)

$ErrorActionPreference = "Stop"

if (-not $FunctionAppName) {
  $FunctionAppName = az resource list --resource-group $ResourceGroup --resource-type Microsoft.Web/sites --query "[?tags.'azd-service-name'=='notification-function'].name | [0]" -o tsv
}

if (-not $FunctionAppName) {
  throw "Unable to resolve Function App name in resource group '$ResourceGroup'."
}

Write-Host "Deploying project '$ProjectPath' to Function App '$FunctionAppName' in '$ResourceGroup'..."

dotnet publish $ProjectPath -c Release -o ./out/notification-function

if (Test-Path ./out/notification-function.zip) {
  Remove-Item ./out/notification-function.zip -Force
}

Compress-Archive -Path ./out/notification-function/* -DestinationPath ./out/notification-function.zip -Force

az functionapp deployment source config-zip --name $FunctionAppName --resource-group $ResourceGroup --src ./out/notification-function.zip

Write-Host "Deployment submitted successfully."
