# Winit for C#

This repository is an experimental C# port of the Rust [`winit`](https://github.com/rust-windowing/winit) windowing library.

The goal is to keep the public model and backend behavior close to upstream winit while using idiomatic C# naming and .NET platform conventions. Rust traits are mapped to C# interfaces, plain Rust enums are mapped to C# enums, and Rust-style tagged unions are represented with allocation-free record structs where practical.

## Status

This project is still in active development and is not production ready.

The current focus is the Win32 backend. The core API shape, event loop facade, window attributes, monitor handling, keyboard input, pointer input, drag-and-drop, window state operations, and a Windows example application are being ported incrementally from upstream winit.

## Project Layout

- `Winit.Core` contains platform-independent event, window, input, and application abstractions.
- `Winit.Dpi` contains DPI, position, and size types.
- `Winit.Win32` contains the Windows backend implemented with direct Win32 interop.
- `Winit` is the public facade crate-style assembly that selects the platform backend.
- `examples/Window` is a Windows example used to exercise the current API surface.

## Target Frameworks

The portable assemblies target `net10.0`.

The Win32 backend targets `net10.0-windows` because it directly uses Windows APIs such as `user32`, `gdi32`, `ole32`, and `dwmapi`.

The public `Winit` facade multi-targets:

- `net10.0` for the portable API surface without a backend.
- `net10.0-windows` for the Win32-enabled build.

Applications that create real Windows windows should target `net10.0-windows`.

## Build

```powershell
dotnet build .\Winit.slnx
```

Build only the facade:

```powershell
dotnet build .\Winit\Winit.csproj -f net10.0
dotnet build .\Winit\Winit.csproj -f net10.0-windows
```

Run the window example:

```powershell
dotnet run --project .\examples\Window\Window.csproj
```

Publish the example with NativeAOT:

```powershell
dotnet publish .\examples\Window\Window.csproj -c Release -r win-x64 -p:PublishAot=true
```

## Interop Notes

`Winit.Win32` disables runtime marshalling and uses source-generated P/Invoke plus handwritten COM and callback ABI definitions. This keeps the backend compatible with NativeAOT and avoids .NET runtime COM interop.

## Upstream Reference

The implementation is intended to follow upstream Rust winit closely where the platform behavior matters. C# API names and type shapes may differ when needed to match .NET conventions.
