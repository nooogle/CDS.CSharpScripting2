# CDS.CSharpScripting2


## Building and deploying

### Making changes

1. Make code changes.
1. Add unit tests and/or demo code as appropriate.
1. Ensure that all unit tests pass.
1. Update the versions (see below).
1. Run the Cake script (see below).
1. Deploy the new NuGet package to the Dropbox NuGet folder (see below).
1. Commit to git and push.
1. Apply tags (see below).


### Semantic versioning

This project uses [Semantic Versioning](https://semver.org/). The version number is 
in the format MAJOR.MINOR.PATCH. If an API is changed in a backwards-compatible way 
then increment the MINOR version. If the change is not backwards-compatible then
increment the MAJOR version. If the change is a bug fix then increment the PATCH
version.

Update version numbers manually (or make a tool!).


### Cake

Cake is a build automation system with a C# DSL to help you write build scripts.

#### Install Cake (one time only):

1. Open the solution and start the Developer Command Prompt for Visual Studio.
2. Run the following commands:
```shell
    dotnet new tool-manifest
    dotnet tool install Cake.Tool
    dotnet cake --version
```
3. Create a build script called build.cake in the root of the solution.


#### Build using Cake

Start a Developer Command Prompt for Visual Studio and navigate to the root of the solution.
Run the following command:

```shell
dotnet cake
```

