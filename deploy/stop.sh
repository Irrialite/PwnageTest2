#!/bin/bash
pid=$(pidof dotnet)
if [ -n "$pid" ]; then
	sudo kill -9 $pid
fi