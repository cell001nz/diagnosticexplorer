SignalR emulator
dotnet tool install  -g Microsoft.Azure.SignalR.Emulator
https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-howto-emulator

Azurite storage emulator
npm install -g azurite
https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage



ng serve --port 4200
swa start http://localhost:4200 --api-location api


ng serve
azurite --location ./.azurite_data
asrs-emulator start -c api\asrs.emulator.settings.json --port 7072
swa start http://localhost:4200 --api-location api

