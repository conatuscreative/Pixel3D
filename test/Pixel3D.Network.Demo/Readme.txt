Requires the following system installs:
- DirectX SDL (June 2010) on Windows
- Wine with `winetricks d3dcompiler_43` on OSX
- MonoGame 3.6 (or newer?) on both Windows and OSX

Requires the following directories to be added at the same level as the Solution
- "FNA" containing FNA project from https://github.com/FNA-XNA/FNA
- "FNALibs" containing FNA libraries from http://fna.flibitijibibo.com/archive/fnalibs.tar.bz2

Note the Build Actions (see "Import"s in the .csproj)
- MonoGameContentBuild
- CompileShader

Note that shaders need to be loaded from fxb files, not through ContentManager (example is provided)

Should work on both VS2010 (and probably newer) and VS Mac 2017

