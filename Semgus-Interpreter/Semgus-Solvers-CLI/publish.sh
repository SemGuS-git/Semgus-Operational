#!/bin/bash

dotnet publish CommandLineInterface.csproj //p:PublishProfile=win-x64
dotnet publish CommandLineInterface.csproj //p:PublishProfile=osx-x64
dotnet publish CommandLineInterface.csproj //p:PublishProfile=linux-x64

mkdir -p publish
rm -rf publish/*

cp -r bin/Release/net6.0/win-x64/publish publish/semgus-cli-win-x64
cp -r bin/Release/net6.0/osx-x64/publish publish/semgus-cli-osx-64
cp -r bin/Release/net6.0/linux-x64/publish publish/semgus-cli-linux-x64

pushd publish
zip -r semgus-cli-win-x64.zip semgus-cli-win-x64/
zip -r semgus-cli-osx-64.zip semgus-cli-osx-64/
zip -r semgus-cli-linux-x64.zip semgus-cli-linux-x64/
popd

echo "--- Done! ---"
