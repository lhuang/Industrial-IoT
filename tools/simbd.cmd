@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

if not "%1" == "--build" goto :args
shift
if not "%1" == "" goto :build
:args
if not "%1" == "" goto :deploy
echo Must specify name of simulation.
goto :done

:build
set __args=
set __args=%__args% -Subscription IOT_GERMANY
set __args=%__args% -Registry industrialiotdev 
set __args=%__args% -Build
set __args=%__args% -Fast 
pushd %build_root%\tools\scripts
powershell ./acr-matrix.ps1 %__args%
popd
if !ERRORLEVEL! == 0 goto :deploy
echo Build failed.
goto :done

:deploy
set __args=
set __args=%__args% -acrSubscriptionName IOT_GERMANY
set __args=%__args% -acrRegistryName industrialiotdev
set __args=%__args% -subscriptionName IOT-OPC-WALLS
set __args=%__args% -aadApplicationName iiot
set __args=%__args% -resourceGroupLocation westeurope
set __args=%__args% -simulationProfile testing
set __args=%__args% -numberOfLinuxGateways 2
set __args=%__args% -numberOfWindowsGateways 2
set __args=%__args% -numberOfSimulationsPerEdge 2
pushd %build_root%\deploy\scripts
powershell ./deploy.ps1 -type simulation %__args% -resourceGroupName %1 -applicationName %1
popd
if !ERRORLEVEL! == 0 goto :done
echo Deploy failed.
goto :done

:done
set __args=
goto :eof
