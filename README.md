JJ.AutoIncrementVersion.Dummy
============================

Isolated repo with tests for `JJ.AutoIncrementVersion` ([NuGet](https://www.nuget.org/packages/JJ.AutoIncrementVersion), [GitHub](https://github.com/jjvanzon/JJ.AutoIncrementVersion))

With a separate repo, the whole MSBuild set-up in the main repo doesn't interfere with the test.

Things might be configured so that when you compile for `Debug` you get `BuildNum` `0` and when you compile for `Release` you get an incremental `BuildNum` coming from the `BuildNum.xml`.

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

- [x] Open `JJ.AutoIncrementVersion.Dummy.sln`
- [x] Uninstall existing `JJ.AutoIncrementVersion` package.
- [x] Go to File Explorer (not Solution Explorer).
- [x] Go to the repository folder 
      (`D:\Repositories\JJ.AutoIncrementVersion.Dummy`)
- [x] Delete `BuildNum.xml` 
- [x] Delete `Directory.Build.props`
- [x] Edit `JJ.AutoIncrementVersion.Dummy.csproj`
- [x] Replace `$(BuildNum)` with `0`

### Run Without Package

- [x] Rebuild
- [x] NOTE: `Rebuild`, don't just `Build`, or "up-to-date" checks may skip the `BuildNum` increments.
- [x] `Output` shows
      `Successfully created package {...} JJ.AutoIncrementVersion.Dummy.4.3.0.nupkg` 
      ending with `.0.nupkg`

### Install

- [x] Install `JJ.AutoIncrementVersion` package
- [x] Rebuild
- [x] `Output` shows `JJ.AutoIncrementVersion.Dummy.4.3.0.nupkg`
      at least ends with `.0.nupkg`
- [x] Auto-creates `BuildNum.xml`
- [x] Auto-creates `Directory.Build.props`

### First Use

- [x] Prepare [Initial State](#set-initial-state) again
- [x] Install `JJ.AutoIncrementVersion` package.
- [x] Edit `JJ.AutoIncrementVersion.Dummy.csproj`
- [x] Use `$(BuildNum)` in `<Version>` e.g. `<Version>4.7.$(BuildNum)</Version>`
- [x] 1st project rebuild should fail: `Invalid NuGet version string`
- [x] 2nd project rebuild succeeds.
- [x] Auto-creates `BuildNum.xml`
- [x] Auto-creates `Directory.Build.props`
- [x] `Output` shows `Successfully created package .. JJ.AutoIncrementVersion.Dummy.4.3.0.nupkg` 
- [x] Subsequent project rebuilds should auto-increment with output showing:  
      `Successfully created package .. JJ.AutoIncrementVersion.Dummy.4.3.1.nupkg`  
      `Successfully created package .. JJ.AutoIncrementVersion.Dummy.4.3.2.nupkg` etc.

### Uninstall

- [x] Uninstall package
- [x] .xml and .Build.props should remain
- [x] Rebuild should succeed
- [x] Ver should stay frozen

### Reinstall

- [x] Reinstall package
- [x] Rebuild should succeed, incrementing ver each time.

### Auto-Recreate Files

- [x] Delete `Directory.Build.props`
- [x] Rebuild solution should fail with error: `NETSDK1018: Invalid NuGet version string`
- [x] But recreated `Directory.Build.props`
- [x] Subsequent builds succeed, incrementing ver each time.
- [x] Delete `BuildNum.xml`
- [x] Rebuild
- [x] `BuildNum.xml` should be recreated
- [x] Versions will start at `BuildNum` `0` or `1` again.
- [x] Deleting both shows similar effect.

### Manual Edit

- [x] Restore original `BuildNum.xml`
- [x] Rebuild
- [x] Versions should continue to increment where it left off.
- [x] Edit `BuildNum.xml`, setting the `BuildNum` value manually.
- [x] Rebuild
- [x] Versions start counting at new `BuildNum`
- [x] And they increment each rebuild.

### Conditionals

This tests conditional `BuildNum.xml` inclusion from the `Directory.Build.props`.

- [x] Open `Director.Build.props`.
- [x] Find the `Condition` attribute on the `Import` element.
- [x] Extend it with:
      ```
      And $(Configuration)=='Release'
      ```
- [x] Example `Directory.Build.props` content:  
     ```xml
     <Project>
     <PropertyGroup><BuildNum>0</BuildNum></PropertyGroup>
     <Import Project="BuildNum.xml" Condition="Exists('BuildNum.xml') 
                      And $(Configuration)=='Release'" />
     </Project>
     ```
- [x] Test compiling for `Release` increments `BuildNum`.
- [x] Test compiling for `Debug` uses `BuildNum` `0`.
- [x] Swap a few times to see if Release build will continue with original range.

### Command Line Build

- [x] Adding `/p:BuildNum=9999` to the command line outputs package with version ending with `9999`.
- [x] Example command line:
     ```shell
     msbuild /p:Configuration=Release /v:minimal /p:BuildNum=9999
     ```
- [x] It saved `9999 + 1 = 10000` back to `BuildNum.xml`.
- [x] ~ This is ok for now, but it might not need to save that back in the future in this case.

### Upgrade Regression

- [x] Test what happens if the `<BuildNumWasFromXmljj>` element is removed from `BuildNum.xml`
      (simulating upgrade path)
- [x] Restores `BuildNumWasFromXmljj`
- [x] Continues to increment build numbers.

### CI Integration

- [ ] Build project(s) (preferably in parallel) in CI
- [ ] See what the version numbers do.
- [ ] If they flip between current and next BuildNum arbitrarily:
- [ ] Use `BuildNum` task from Visual Studio Marketplace
- [ ] Add `/p:BuildNum=$(BuildNum)` to build args.
- [ ] This freezes the BuildNum for the whole pipeline run to the initial value.

### Real-Life Test

- [ ] Check if it functions in real-life projects before publishing to NuGet.