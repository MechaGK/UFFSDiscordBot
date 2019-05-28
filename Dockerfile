FROM mcr.microsoft.com/dotnet/core/runtime:2.1

ARG BUILD_DIR=./DiscordBot/bin/Release/netcoreapp2.1/publish/

COPY $BUILD_DIR app/

ENTRYPOINT ["dotnet", "app/DiscordBot.dll"]
