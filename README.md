# Ekona [![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://choosealicense.com/licenses/mit/) ![Build and release](https://github.com/SceneGate/Ekona/workflows/Build%20and%20release/badge.svg)

[Yarhl](https://github.com/SceneGate/yarhl) plugin for Nintendo DS common
formats.

The library supports .NET 5.0 and above on Linux, Window and MacOS.

<!-- prettier-ignore -->
| Release | Package                                                           |
| ------- | ----------------------------------------------------------------- |
| Stable  | [![Nuget](https://img.shields.io/nuget/v/SceneGate.Ekona?label=nuget.org&logo=nuget)](https://www.nuget.org/packages/SceneGate.Ekona) |
| Preview | [Azure Artifacts](https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview) |

## Supported formats

_Encryption, decryption or signature validation not supported yet._

- DS cartridge filesystem: read

## Documentation

Feel free to ask any question in the
[project Discussion site!](https://github.com/SceneGate/Ekona/discussions)

Check our on-line [API documentation](https://scenegate.github.io/Ekona/).

## Build

The project requires to build .NET 5.0 SDK, .NET Core 3.1 runtime and .NET
Framework 4.8 or latest Mono. If you open the project with VS Code and you did
install the
[VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers)
extension, you can have an already pre-configured development environment with
Docker or Podman.

To build, test and generate artifacts run:

```sh
# Only required the first time
dotnet tool restore

# Default target is Stage-Artifacts
dotnet cake
```

To just build and test quickly, run:

```sh
dotnet cake --target=BuildTest
```

## References

- [GBATek](https://problemkaputt.de/gbatek.htm)
