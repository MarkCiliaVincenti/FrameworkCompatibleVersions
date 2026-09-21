# ![FrameworkCompatibleVersions](https://raw.githubusercontent.com/MarkCiliaVincenti/FrameworkCompatibleVersions/master/logo32.png)&nbsp;FrameworkCompatibleVersions
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/MarkCiliaVincenti/FrameworkCompatibleVersions/dotnet.yml?branch=master&logo=github&style=flat)](https://actions-badge.atrox.dev/MarkCiliaVincenti/FrameworkCompatibleVersions/goto?ref=master) [![NuGet](https://img.shields.io/nuget/v/FrameworkCompatibleVersions?label=NuGet&logo=nuget&style=flat)](https://www.nuget.org/packages/FrameworkCompatibleVersions) [![NuGet](https://img.shields.io/nuget/dt/FrameworkCompatibleVersions?logo=nuget&style=flat)](https://www.nuget.org/packages/FrameworkCompatibleVersions) [![Codacy Grade](https://img.shields.io/codacy/grade/df84851324464aae99ec76cb3cf2ae15?style=flat)](https://app.codacy.com/gh/MarkCiliaVincenti/FrameworkCompatibleVersions/dashboard)

An MSBuild Project SDK that lets a consumer mark selected NuGet dependencies as framework-compatible versions without repeating target-framework conditions for every package.

The SDK is deliberately package-agnostic. It contains no list of package IDs and never adds dependencies. Consumers choose the packages by adding `FrameworkCompatibleVersion="true"` to their own `PackageVersion` or `PackageReference` items.

## Installation and usage

### Recommended: centralize with `global.json` + `Directory.Build.props`/`Directory.Build.targets`

For any repository with more than a single project, the recommended approach is to pin the SDK version once in `global.json` and import the SDK once from the root MSBuild files. This keeps individual project files clean and avoids repeating the SDK reference (and version) in every `<Project Sdk="...">` line.

1. Pin the SDK version in `global.json`:

```json
{
  "msbuild-sdks": {
    "FrameworkCompatibleVersions": "1.0.0"
  }
}
```

2. Import the SDK's `.props` near the top of `Directory.Build.props`:

```xml
<Project>
    <!-- Other early configuration can go here. -->

    <Import Project="Sdk.props" Sdk="FrameworkCompatibleVersions" />
</Project>
```

3. Import the SDK's `.targets` near the bottom of `Directory.Build.targets`:

```xml
<Project>
    <Import Project="Sdk.targets" Sdk="FrameworkCompatibleVersions" />

    <!-- Other late configuration can go here. -->
</Project>
```

With this in place, every project under that directory automatically participates. The individual project files remain plain:

```xml
<Project Sdk="Microsoft.NET.Sdk">
```

Because the SDK is package-agnostic, nothing is applied until a project (or a shared `Directory.Packages.props`/`Directory.Build.props`) opts specific packages in with `FrameworkCompatibleVersion="true"`. Projects that should not participate can set `<FrameworkCompatibleVersionsEnabled>false</FrameworkCompatibleVersionsEnabled>`.

### Alternative: per-project SDK reference

A single project (or a repository that prefers not to use root MSBuild files) can reference the SDK directly alongside the normal .NET SDK, pinning the version inline:

```xml
<Project Sdk="Microsoft.NET.Sdk;FrameworkCompatibleVersions/1.0.0">
```

Or, with the version centralized in `global.json` as shown above, the version can be omitted:

```xml
<Project Sdk="Microsoft.NET.Sdk;FrameworkCompatibleVersions">
```

### Central Package Management

With Central Package Management, opt packages in where their versions are declared:

```xml
<Project>
    <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    </PropertyGroup>

    <ItemGroup>
        <PackageVersion Include="Microsoft.Extensions.Http" FrameworkCompatibleVersion="true" />
        <PackageVersion Include="System.Text.Json" FrameworkCompatibleVersion="MatchingMajor" />
        <PackageVersion Include="Pinned.Package" Version="1.2.3" />
    </ItemGroup>
</Project>
```

The consuming projects continue to use ordinary versionless package references:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="System.Text.Json" />
    <PackageReference Include="Pinned.Package" />
</ItemGroup>
```

When Central Package Management is enabled, FrameworkCompatibleVersions automatically enables `CentralPackageFloatingVersionsEnabled` because the selected versions are floating.

### Direct PackageReference usage

Without Central Package Management, apply the metadata directly to the package reference:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" FrameworkCompatibleVersion="true" />
    <PackageReference Include="System.Text.Json" FrameworkCompatibleVersion="true" />
    <PackageReference Include="Pinned.Package" Version="1.2.3" />
</ItemGroup>
```

Unmarked package items are left unchanged.

## Version policy

FrameworkCompatibleVersions supports two policies, selected per package through the value of the `FrameworkCompatibleVersion` metadata:

| Metadata value | Policy |
| --- | --- |
| `true` | Highest tested against (the default). |
| `HighestTestedAgainst` | Identical to `true`. |
| `MatchingMajor` | A package major that matches the target framework's own runtime major. |

### Highest tested against (`true` / `HighestTestedAgainst`)

| Target framework | NuGet version expression |
| --- | --- |
| Earlier than .NET 6 | `[6.*,7.0.0)` |
| .NET 6 and .NET 7 | `[8.*,9.0.0)` |
| .NET 8 and .NET 9 | `[10.*,11.0.0)` |
| .NET 10 and later | `[*,13.0.0)` |

### Matching major (`MatchingMajor`)

Each target framework resolves to a package major that matches its own runtime major:

| Target framework | NuGet version expression |
| --- | --- |
| .NET 5 | `[5.*,6.0.0)` |
| .NET 6 | `[6.*,7.0.0)` |
| .NET 7 | `[7.*,8.0.0)` |
| .NET 8 | `[8.*,9.0.0)` |
| .NET 9 | `[9.*,10.0.0)` |
| .NET 10 | `[10.*,11.0.0)` |
| .NET 11, 12, 13 | `[11.*,12.0.0)`, `[12.*,13.0.0)`, `[13.*,14.0.0)` |
| .NET Core App 3.x | `[3.*,4.0.0)` |

Target frameworks earlier than .NET 5 are trickier: `netstandard`, .NET Framework, and legacy `TargetFrameworkVersion`-only projects have no runtime major, and package majors before the .NET 5 unification are not aligned across ecosystems (for example, the `Microsoft.Extensions.*` packages have no 4.x). For those, `MatchingMajor` falls back to the lowest broadly-available major that still supports `netstandard2.0` (`[2.*,3.0.0)`), while .NET Core apps resolve to their own major (`netcoreapp2.x` -> `[2.*,3.0.0)`, `netcoreapp3.x` -> `[3.*,4.0.0)`).

NuGet resolves each floating expression to one concrete stable package version during restore.

## Multi-targeting

The SDK evaluates the policy separately for each inner build. A project targeting, for example:

```xml
<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>
```

can declare a package once with `FrameworkCompatibleVersion="true"`; each target framework receives its corresponding version expression.

## Directory.Build.props and Directory.Packages.props

The metadata can live wherever the consumer normally declares the item, including the project file, `Directory.Build.props`, `Directory.Packages.props`, nested variants of those files, or explicitly imported `.props` files. FrameworkCompatibleVersions works against the evaluated `PackageVersion` and `PackageReference` items rather than maintaining its own package list.

## Legacy .NET Framework projects

Legacy non-SDK-style .NET Framework projects can use FrameworkCompatibleVersions when they use `PackageReference`, a sufficiently modern MSBuild/NuGet toolchain, and an MSBuild SDK reference. Projects that expose `TargetFrameworkVersion` receive the pre-.NET-6 policy.

`packages.config` is intentionally not supported.
