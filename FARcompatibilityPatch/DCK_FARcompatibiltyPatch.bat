@ECHO OFF
ECHO(
ECHO ------------------------------------------------------
ECHO       DCK Aircraft Armor FAR Compatibility Patch 
ECHO ------------------------------------------------------
ECHO(
ECHO(  DCK_FARcompatibilityPatch adds exceptions to FAR so
ECHO(   that DCK Aircraft Armor is invisible to FAR Aero 
ECHO(        calculations and then deletes itself
ECHO(
ECHO     This file should be inside your FAR directory 
ECHO(
PAUSE

@echo off & setlocal EnableDelayedExpansion
set row= 
for /F "delims=" %%j in (FARPartModuleTransformExceptions.cfg) do (
  if  defined row echo.!row!>>FARPartModuleTransformExceptionstemp.cfg
  set row=%%j
)
endlocal

ECHO OFF
ECHO(      FARPartModuleException>>FARPartModuleTransformExceptionstemp.cfg
ECHO(      {>>FARPartModuleTransformExceptionstemp.cfg
ECHO(            PartModuleName = DCKAAtextureswitch2>>FARPartModuleTransformExceptionstemp.cfg
ECHO(            TransformException = objectNames>>FARPartModuleTransformExceptionstemp.cfg
ECHO(            TransformException = ArmorRootTransform>>FARPartModuleTransformExceptionstemp.cfg
ECHO(      }>>FARPartModuleTransformExceptionstemp.cfg
ECHO(}>>FARPartModuleTransformExceptionstemp.cfg


DEL FARPartModuleTransformExceptions.cfg
REN FARPartModuleTransformExceptionstemp.cfg FARPartModuleTransformExceptions.cfg


ECHO(
ECHO ----------------------------------------------------
ECHO       FAR Compatibility Successfully Installed
ECHO ----------------------------------------------------
ECHO(
ECHO              Now Go Blow Something Up!
ECHO(

PAUSE

DEL DCK_FARcompatibiltyPatch.bat
