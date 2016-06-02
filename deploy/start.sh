#!/bin/bash
cd /app/NetworkBase/src/NetworkBase
sudo dotnet restore
if [ -d /app/server/ ]; then
	cd /app/server/src/ServerTest2/
	sudo dotnet restore
	sudo dotnet run --configuration Release
fi

if [ -d /app/client/ ]; then
	cd /app/client/src/ConsoleApp1/
	sudo dotnet restore
	sudo dotnet run --configuration Release
fi