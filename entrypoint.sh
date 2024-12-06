#!/bin/bash

dotnet nuget add source --name Artifactory https://artifactory.aws.wiley.com/artifactory/api/nuget/nuget
dotnet watch run --project=Trackings.API --urls=http://+:5001 --no-launch-profile