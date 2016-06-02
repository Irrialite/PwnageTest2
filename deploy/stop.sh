#!/bin/bash
pid=$(pidof dotnet)
if [ ! -z $pid]; then
	sudo kill -9 $pid
fi