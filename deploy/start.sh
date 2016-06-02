#!/bin/bash
cd /app/NetworkBase/src/NetworkBase
sudo dotnet restore
if [ -d /app/client/ ]; then
	cd /app/client/src/ConsoleApp1/
	sudo dotnet restore
	sudo dotnet run --configuration Release
fi