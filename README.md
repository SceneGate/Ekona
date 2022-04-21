# Ekona [![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://choosealicense.com/licenses/mit/) ![Build and release](https://github.com/SceneGate/Ekona/workflows/Build%20and%20release/badge.svg)

[Yarhl](https://github.com/SceneGate/yarhl) plugin for DS and DSi formats.

The library supports .NET 6.0 and above on Linux, Window and MacOS.

<!-- prettier-ignore -->
| Release | Package                                                           |
| ------- | ----------------------------------------------------------------- |
| Stable  | [![Nuget](https://img.shields.io/nuget/v/SceneGate.Ekona?label=nuget.org&logo=nuget)](https://www.nuget.org/packages/SceneGate.Ekona) |
| Preview | [Azure Artifacts](https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview) |

## Supported formats

- DS cartridge:
  - Filesystem: read and write
  - Header: read and write, including extended header
  - Banner and icon: read and write.
  - ARM9 secure area encryption and decryption.
  - HMAC validation and re-generation when keys are provided.
  - Signature validation when keys are provided.
- DSi cartridge:
  - Filesystem: read and write `arm9i` and `arm7i` programs.
  - Header: read and write
  - Animated banner icons
  - Modcrypt encryption and decryption
  - HMAC validation and re-generation (including digest) when keys are provided.
  - Signature validation when keys are provided.

## Documentation

Feel free to ask any question in the
[project Discussion site!](https://github.com/SceneGate/Ekona/discussions)

Check our on-line [API documentation](https://scenegate.github.io/Ekona/).

## Build

The project requires to build .NET 6.0 SDK and .NET Framework 4.8 or latest
Mono. If you open the project with VS Code and you did install the
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

To run the performance test with memory and CPU traces:

```sh
dotnet run --project src/Ekona.PerformanceTests/ -c Release -- -f "*<TestName>*" -m -p EP --maxWidth 60
```

## References

- [GBATek](https://problemkaputt.de/gbatek.htm)
