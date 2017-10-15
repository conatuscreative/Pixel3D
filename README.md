Pixel3D
=======

Isometric/2.5D pixel game engine built on FNA.

It was used to create the game River City Ransom: Underground.

NOTE: You will need to source your own copy of fxc.exe and place it in /builds/tools

Pixel3D is built on top of FNA, all relevant licenses and attribution is available here:
https://github.com/FNA-XNA/FNA/tree/master/licenses

Installation
============

Requires the following system installs:
- DirectX SDK (June 2010) on Windows
- Wine with `winetricks d3dcompiler_43` on OSX
- MonoGame 3.6 (or newer?) on both Windows and OSX

Requires the following directories to be added at the same level as the Solution
- "FNA" containing FNA project from https://github.com/FNA-XNA/FNA
- "FNALibs" containing FNA libraries from http://fna.flibitijibibo.com/archive/fnalibs.tar.bz2

fxc.exe should live in 'build\tools'

Note that shaders need to be loaded from fxb files, not through ContentManager (example is provided)

Should work on both VS2010 (and probably newer) and VS Mac / VS 2017