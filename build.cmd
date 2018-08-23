::Start
@echo off
mode con cols=120 lines=30
color 03
setlocal enabledelayedexpansion
title Build
cls


::			- Main -			::

:: Ini ::
:Ini
Rem Fix an error with the current directory in Admin mode
cd %~dp0

set CurrentDir=%~dp0
set PluginName=ExeIconPicker
set KeePassDir=D:\Install\Software\KeePass
set KeePassPluginsDir=%KeePassDir%\Plugins

:: Cleaning files
cd %PluginName%
erase /F /S /Q obj bin >nul
cd ..

echo Building plgx...
%KeePassDir%\KeePass.exe --plgx-create "%CurrentDir%%PluginName%"

echo Moving to Plugins dir...
move /Y ".\%PluginName%.plgx" "%KeePassPluginsDir%" >nul

echo Done.