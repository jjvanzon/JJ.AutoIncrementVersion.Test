JJ.AutoIncrementVersion.Test
============================

Isolated repo with manual tests for `JJ.AutoIncrementVersion` ([NuGet](https://www.nuget.org/packages/JJ.AutoIncrementVersion), [GitHub](https://github.com/jjvanzon/JJ.AutoIncrementVersion))

With a separate repo, the whole MSBuild set-up in the main repo doesn't interfere with the test.

Things might be configured so that when you compile for `Debug` you get `BuildNum` `0` and when you compile for `Release` you get an incremental `BuildNum` coming from the `BuildNum.xml`. This is by design and tests if conditional `BuildNum.xml` inclusion works. (`BuildNum.xml` updates can cause rebuild of all projects, making the build slower. This is an option to conditionally prevent that for tooling optimization.)

You can mess around with the project when you test, and then just undo the changes with git and be all clean again.

- [Manual Test Plan](#manual-test-plan)
    - [Set Initial State](#set-initial-state)
    - [Run Without Package](#run-without-package)
    - [Install](#install)
    - [First Use](#first-use)
    - [Uninstall](#uninstall)
    - [Reinstall](#reinstall)
    - [Auto-Recreate Files](#auto-recreate-files)
    - [Manual Edit](#manual-edit)
    - [Conditionals](#conditionals)
    - [Command Line Build](#command-line-build)
    - [Upgrade Regression](#upgrade-regression)
    - [CI Integration](#ci-integration)
    - [Real-Life Test](#real-life-test)

Manual Test Plan
----------------

### Set Initial State

- [ ] Uninstall existing `JJ.AutoIncrementVersion` package.
- [ ] Go to File Explorer (not Solution Explorer).
- [ ] Go to the repository folder 
      (`D:\Repositories\JJ.AutoIncrementVersion.Test`)
- [ ] Delete `BuildNum.xml` and `Directory.Build.props`
- [ ] Open `JJ.AutoIncrementVersion.Test.csproj`
- [ ] Replace `$(BuildNum)` with `0`

### Run Without Package

- [ ] Rebuild solution.
- [ ] `Output` shows
      `Successfully created package {...} JJ.AutoIncrementVersion.Test.4.2.0.nupkg` 
      ending with `.0.nupkg`

### Install

- [ ] Install `JJ.AutoIncrementVersion` package.
- [ ] Rebuild solution
- [ ] `Output` shows `JJ.AutoIncrementVersion.Test.4.2.0.nupkg`
      at least ends with `.0.nupkg`
- [ ] Auto-creates `BuildNum.xml` with content:  
      `<Project><PropertyGroup><BuildNum>1</BuildNum><DisableFastUpToDateCheck>True</DisableFastUpToDateCheck><BuildNumWasFromXmljj>True</BuildNumWasFromXmljj></PropertyGroup></Project>`
- [ ] Auto-creates `Directory.Build.props` with content:  
      `<Project><PropertyGroup><BuildNum>0</BuildNum></PropertyGroup><Import Project="BuildNum.xml" Condition="Exists('BuildNum.xml')" /></Project>`

### First Use

- [ ] Prepare [Initial State](#set-initial-state) again
- [ ] Install `JJ.AutoIncrementVersion` package.
- [ ] Open `JJ.AutoIncrementVersion.Test.csproj`
- [ ] Use `$(BuildNum)` in `<Version>` e.g. `<Version>4.2.$(BuildNum)</Version>`
- [ ] 1st rebuild should fail:
      `Invalid NuGet version string: '4.2.'`
- [ ] 2nd build succeeds.
- [ ] Auto-creates `BuildNum.xml` with content:  
      `<Project><PropertyGroup><BuildNum>1</BuildNum><DisableFastUpToDateCheck>True</DisableFastUpToDateCheck><BuildNumWasFromXmljj>True</BuildNumWasFromXmljj></PropertyGroup></Project>`
- [ ] Auto-creates `Directory.Build.props` with content:  
      `<Project><PropertyGroup><BuildNum>0</BuildNum></PropertyGroup><Import Project="BuildNum.xml" Condition="Exists('BuildNum.xml')" /></Project>`
- [ ] `Output` shows `Successfully created package .. JJ.AutoIncrementVersion.Test.4.2.0.nupkg` 
- [ ] Subsequent builds should auto-increment with output showing:  
      `Successfully created package .. JJ.AutoIncrementVersion.Test.4.2.1.nupkg`  
      `Successfully created package .. JJ.AutoIncrementVersion.Test.4.2.2.nupkg` etc.

### Uninstall

- [ ] Uninstall package
- [ ] .xml and .Build.props should remain
- [ ] Build should succeed
- [ ] Ver should stay frozen

### Reinstall

- [ ] Reinstall package
- [ ] Build should succeed, incrementing ver each time.

### Auto-Recreate Files

- [ ] Delete `Directory.Build.props`
- [ ] Build should fail with error:  
      `NETSDK1018: Invalid NuGet version string: '4.2.'.`
- [ ] But recreated `Directory.Build.props`
- [ ] Subsequent builds succeed, incrementing ver each time.
- [ ] Delete `BuildNum.xml`
- [ ] Build
- [ ] `BuildNum.xml` should be recreated
- [ ] Versions will start at `BuildNum` `0` or `1` again.
- [ ] Deleting both shows similar effect.

### Manual Edit

- [ ] Restore original `BuildNum.xml`
- [ ] Build
- [ ] Versions should continue to increment where it left off.
- [ ] Edit `BuildNum.xml`, setting the `BuildNum` value manually.
- [ ] Build
- [ ] Versions start counting at new `BuildNum`
- [ ] And they increment each build.

### Conditionals

This tests conditional `BuildNum.xml` inclusion from the `Directory.Build.props`.

- [ ] Open `Director.Build.props`.
- [ ] Find the `Condition` attribute on the `Import` element.
- [ ] Extend it with ` And $(Configuration)=='Release'`
- [ ] Example `Directory.Build.props` content:  
     ```xml
     <Project>
     <PropertyGroup><BuildNum>0</BuildNum></PropertyGroup>
     <Import Project="BuildNum.xml" Condition="Exists('BuildNum.xml') 
                      And $(Configuration)=='Release'" />
     </Project>
     ```
- [ ] Test compiling for `Release` increments `BuildNum`.
- [ ] Test compiling for `Debug` uses `BuildNum` `0`.
- [ ] Swap a few times to see if Release build will continue with original range.

### Command Line Build

- [ ] Adding `/p:BuildNum=9999` to `dotnet build` outputs package with version ending with `9999`.
- [ ] It saved `9999 + 1 = 10000` back to `BuildNum.xml`.
- [ ] ~ This is ok for now, but it might not need to save that back in the future in this case.

### Upgrade Regression

- [ ] Test what happens if `BuildNumWasFromXmljj` is removed from `BuildNum.xml` (simulating upgrade path)
- [ ] Restores `BuildNumWasFromXmljj`
- [ ] Continues to increment build numbers.

### CI Integration

- [ ] Build project(s) (preferably in parallel) in CI
- [ ] See what the version numbers do.
- [ ] ~ Currently it may flip between current and next BuildNum arbitrarily.
- [ ] ~ It's worse in parallel build.
- [ ] ~ Should fix in the future.

### Real-Life Test

- [ ] Check if it functions in real-life projects before publishing to NuGet.