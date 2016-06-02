#!/bin/bash
mkdir -p /app/log
	
sudo rsync --delete-before --verbose --archive --exclude ".*" /app/baseTemp/ /app/NetworkBase/ > /app/log/deploy.log