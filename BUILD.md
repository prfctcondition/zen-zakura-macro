# Building Zen Zakura Macro

## Prerequisites

- **Visual Studio 2022+** with:
  - "Desktop development with C++" workload
  - `cl.exe` (MSVC compiler) available
- **.NET 9 SDK** (`dotnet --version` should show 9.x)
- **Inno Setup 6** (optional, for building the installer)

## Quick Build (using build script)

```powershell
.\build.ps1
```

Output: `bin\Release\ZenZakuraUI.exe`

## Manual Build

### 1. Build the C++ DLL (ZenZakuraCore.dll)

Open a **Developer Command Prompt for VS 2022+** and run:

```cmd
msbuild ZenZakuraCore\ZenZakuraCore.vcxproj /p:Configuration=Release /p:Platform=x64
```

The DLL will be at `ZenZakuraCore\x64\Release\ZenZakuraCore.dll`.

### 2. Build the C# UI (ZenZakuraUI.exe)

```cmd
dotnet publish ZenZakuraUI\ZenZakuraUI.csproj -c Release -o bin\Release
```

### 3. Copy C++ DLL to output

```
ZenZakuraCore\x64\Release\ZenZakuraCore.dll  →  bin\Release\
```

## Running

After building, run `bin\Release\ZenZakuraUI.exe`.

## Building the Installer

With Inno Setup 6 installed:

```cmd
iscc installer\setup.iss
```

Output: `installer\Output\ZenZakuraMacro-Setup.exe`
