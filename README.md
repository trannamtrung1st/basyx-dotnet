﻿# Welcome to basyx-dotnet!

This is the main repository to start working with the BaSyx .NET SDK.

The entire .NET SDK is structured in 4 separate Git submodules:
- [basyx-dotnet-sdk](https://github.com/eclipse-basyx/basyx-dotnet-sdk): Contains all the core libraries to build everything from scratch
- [basyx-dotnet-components](https://github.com/eclipse-basyx/basyx-dotnet-components): Built on top of the core libraries providing more high-level components as well as various client and server libraries
- [basyx-dotnet-applications](https://github.com/eclipse-basyx/basyx-dotnet-applications): Off-the-shelf components ready to be used or deployed in any sceneraio
- [basyx-dotnet-examples](https://github.com/eclipse-basyx/basyx-dotnet-examples): Example solution to how everything workds

# Setup BaSyx

## NuGet Packages
All tagged/released packages are available as NuGet packages on nuget.org and can be installed via NuGet Package Manager within Visual Studio.

## Build NuGet Packages on your own
In order to build your own NuGet packages and use them in your project with the newest commits on the main-branch, just execute **Setup_BaSyx.bat** and **Build_BaSyx.bat**. 
Make your Visual Studio is closed the first time you run these scripts. The first script will add a new folder **basyx-dotnet-nuget-packages** and add this folder as NuGet package source to the system. The second script will build the NuGet packages and put them into the above mentioned folder. (To build the NuGet packages in Visual Studio don't forget to change the Solution Configuration to *Release*)
