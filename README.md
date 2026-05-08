# CDS.CSharpScripting2

![CI](https://github.com/nooogle/CDS.CSharpScripting2/actions/workflows/ci.yml/badge.svg)


## 🔧 Versioning & Build Process

This project uses Nerdbank.GitVersioning to automate version stamping based 
on Git history and Git tags.

### 🛠️ Building & Packaging
To build the project and generate a NuGet package:

* Use Visual Studio to build the release configuration, or
* Run `dotnet pack -c Release`

The version of the resulting package is derived from the Git state:

| Git State	| Generated Version Example |
|-----------|---------------------------|
| Tag v2.0.2	| 2.0.2 |
| Commit after tag v2.0.2	| 2.0.2+gabcdef |
| Commit after minor bump to 2.1	| 2.1.1+gabcdef |
| Tag v3.0.0	| 3.0.0 |

The format follows [SemVer 2.0.0](https://semver.org/), with Git commit metadata included 
in development builds.

### 🔁 Change Types & Versioning Strategy

| Change Type	|  Action Required | 
|-------------|------------------|
| 🐛 Bug fix / patch	| Just commit — Nerdbank will auto-bump the patch number |
| ✨ Minor feature	| Optionally update version.json to new minor version |
| 💥 Breaking change	| Manually update version.json to bump major version |


### 📦 Releasing a New Version

(Optional) Edit version.json to bump major/minor:

```json
{
  "version": "3.0"
}
```

Commit your changes:

```shell
git commit -am "Update: breaking changes for 3.0"
```

Tag the release:

```shell
git tag v3.0.0
git push --tags
```

Build your release package:
```shell
dotnet pack -c Release
```

This will generate:

`CDS.CSharpScriptUtils.3.0.0.nupkg`

(Optional) Push to NuGet:

```shell
dotnet nuget push bin/Release/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### 🧪 Development Builds

Between releases, every commit produces a uniquely versioned dev build:

* Example: `2.0.3+gabcdef`
* These are not prerelease versions — they’re full SemVer builds with metadata

This ensures you can test builds and track behavior without releasing official versions until you're ready.


## Attributuions

Uicons by <a href="https://www.flaticon.com/uicons">Flaticon</a>
