#!/bin/bash
cd /app/NetworkBase/src/NetworkBase
sudo dotnet restore
if [ -d /app/server/ ]; then
	cd /app/server/src/ServerTest2/
	sudo dotnet restore
	path=$(sudo dotnet publish --configuration Release | grep -Pio "(?<=published to )(.+)")
	cd $path
	# Redirect stdout, stderr, and stdin to dev null to let aws continue deployment
	sudo dotnet ServerTest2.dll > /dev/null 2> /dev/null < /dev/null &
fi

if [ -d /app/client/ ]; then
	cd /app/client/src/ConsoleApp1/
	sudo dotnet restore
	path=$(sudo dotnet publish --configuration Release | grep -Pio "(?<=published to )(.+)")
	cd $path
	# Redirect stdout, stderr, and stdin to dev null to let aws continue deployment
	sudo dotnet ConsoleApp1.dll > /dev/null 2> /dev/null < /dev/null &
fi