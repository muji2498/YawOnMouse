# Yaw On Mouse Mod - Nuclear Option

# Mod Origins

This mod was orginally created by Haika (_haika) on the Nuclear Option discord, i set out to convert this mod into a bepinex mod. The original mod by Haika can be found here:
https://discord.com/channels/909034158205059082/1319566766594068531/1319566766594068531

## Bepinex Version
This mod requires bepinex version [5.4.23.2](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2)

## Config Overview

The config for this mod can be found at `GameInstallDir\BepInEx\config\YawOnMouse.cfg`

`PlayerAxisControls_Patch` - This Config will enable or disable the patch, this must be toggled before game startup and cannot be toggled at runtime as it is a transpiler patch. <br>
When the mod is first applied this will default to `true`

`AxisPatchType` - This Config chooses what type of behaviour the x-axis should do. <br>
When the mod is first applied this will default to `Yaw`
<br>
There are three behaviours:
- `Roll` - This is default behaviour of the x-axis
- `NoRoll` - This Disables rolling on the x-axis
- `Yaw` - This makes the aircraft yaw on the x-axis

## Building Project

Change `<GameDir>YourGameDirectoryHere</GameDir>` inside GameDir.targets to the install location of your game. <br>
Example: `<GameDir>C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option</GameDir>`