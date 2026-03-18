JJ.AutoIncrementVersion.Test
============================

Isolated repo with manual tests for `JJ.AutoIncrementVersion` ([NuGet](https://www.nuget.org/packages/JJ.AutoIncrementVersion), [GitHub](https://github.com/jjvanzon/JJ.AutoIncrementVersion))

With a separate repo, the whole MSBuild set-up in the main repo doesn't interfere with the test.

Things might be configured so that when you compile for `Debug` you get `BuildNum` `0` and when you compile for `Release` you get an incremental `BuildNum` coming from the `BuildNum.xml`. This is by design and tests if conditional `BuildNum.xml` inclusion works. (`BuildNum.xml` updates can cause rebuild of all projects, making the build slower. This is an option to conditionally prevent that for tooling optimization.)

You can mess around with the project when you test, and then just undo the changes with git and be all clean again.

- [Manual Test Plan](#manual-test-plan)
      - [Initial State](#initial-state)
      - [Run Without](#run-without)
      - [Install](#install)
      - [First Use](#first-use)
      - [Uninstall](#uninstall)
      - [Reinstall](#reinstall)
      - [Auto-Create](#auto-create)
      - [Edit BuildNum](#edit-buildnum)
      - [Conditional Inclusion](#conditional-inclusion)
      - [Upgrade Regression](#upgrade-regression)

Manual Test Plan
----------------

### [x] Initial State

- [x] Uninstall existing `JJ.AutoIncrementVersion` package.
- [x] Go to File Explorer, not Solution Explorer.
- [x] Go to the repository folder 
      (`D:\Repositories\JJ.AutoIncrementVersion.Test`)
- [x] Delete `BuildNum.xml` and `Directory.Build.props`
- [x] Open `JJ.AutoIncrementVersion.Test.csproj`
- [x] Replace `$(BuildNum)` with `0`

### [x] Run Without

- [x] Rebuild solution.
- [x] `Output` shows
      `Successfully created package .. JJ.AutoIncrementVersion.Test.4.2.0.nupkg` 
      ending with `.0.nupkg`

### [x] Install

- [x] Install `JJ.AutoIncrementVersion` package.
- [x] Rebuild solution
- [x] `Output` shows `JJ.AutoIncrementVersion.Test.4.2.0.nupkg`
      at least ends with `.0.nupkg`
- [x] Auto-creates `BuildNum.xml` with content:  
      `<Project><PropertyGroup><BuildNum>1</BuildNum><DisableFastUpToDateCheck>True</DisableFastUpToDateCheck><BuildNumWasFromXmljj>True</BuildNumWasFromXmljj></PropertyGroup></Project>`
- [x] Auto-creates `Directory.Build.props` with content:  
      `<Project><PropertyGroup><BuildNum>0</BuildNum></PropertyGroup><Import Project="BuildNum.xml" Condition="Exists('BuildNum.xml')" /></Project>`

### [x] First Use

- [x] Prepare [Initial State](#initial-state) again
- [x] Install `JJ.AutoIncrementVersion` package.
- [x] Open `JJ.AutoIncrementVersion.Test.csproj`
- [x] Use `$(BuildNum)` in `<Version>` e.g. `<Version>4.2.$(BuildNum)</Version>`
- [x] 1st rebuild should fail:
      `Invalid NuGet version string: '4.2.'`
- [x] But auto-creates `Directory.Build.props` with content:  
      `<Project><PropertyGroup><BuildNum>0</BuildNum></PropertyGroup><Import Project="BuildNum.xml" Condition="Exists('BuildNum.xml')" /></Project>`
- [x] 2nd build succeeds.
- [x] And auto-creates `BuildNum.xml` with content:  
      `<Project><PropertyGroup><BuildNum>1</BuildNum><DisableFastUpToDateCheck>True</DisableFastUpToDateCheck><BuildNumWasFromXmljj>True</BuildNumWasFromXmljj></PropertyGroup></Project>`
- [x] `Output` shows `Successfully created package .. JJ.AutoIncrementVersion.Test.4.2.0.nupkg` 
- [x] Subsequent builds should auto-increment with output showing:  
      `Successfully created package .. JJ.AutoIncrementVersion.Test.4.2.1.nupkg`  
      `Successfully created package .. JJ.AutoIncrementVersion.Test.4.2.2.nupkg` etc.

### [x] Uninstall

- [x] Uninstall package
- [x] .xml and .Build.props should remain
- [x] Build should succeed
- [x] Ver should stay frozen

### [x] Reinstall

- [x] Reinstall package
- [x] Build should succeed, incrementing ver each time.

### [x] Recreate

- [x] Delete `Directory.Build.props`
- [x] Build should fail with error:  
      `NETSDK1018: Invalid NuGet version string: '4.2.'.`
- [x] But recreated `Directory.Build.props`
- [x] Subsequent builds succeed, incrementing ver each time.
- [x] Delete `BuildNum.xml`
- [x] Build
- [x] `BuildNum.xml` should be recreated
- [x] Versions will start at BuildNum 0 or 1 again.
- [x] Deleting both shows similar effect.

### [x] Edit

- [x] Rerstore original `BuildNum.xml`
- [x] Build
- [x] Versions should continue to increment where it left off.
- [x] Edit BuildNum.xml, setting the BuildNum value manually.
- [x] Build
- [x] Versions start counting at new BuildNum
- [x] And they increment each build.

### [x] Conditions

This tests conditional `BuildNum.xml` inclusion from the `Directory.Build.props`.

- [x] Open `Director.Build.props`.
- [x] Find the `Condition` attribute on the `Import` element.
- [x] Extend it with ` And $(Configuration)=='Release'`
- [x] Example `Directory.Build.props` content:  
     ```xml
     <Project>
     <PropertyGroup><BuildNum>0</BuildNum></PropertyGroup>
     <Import Project="BuildNum.xml" 
             Condition="Exists('BuildNum.xml') 
                        And $(Configuration)=='Release'" />
     </Project>
     ```
- [x] Test compiling for `Release` increments `BuildNum`.
- [x] Test compiling for `Debug` uses `BuildNum` `0`.

### [x] Upgrade Regression

- [x] Test what happens if `BuildNumWasFromXmljj` is removed from `BuildNum.xml` (simulating upgrade path)
- [x] Restores `BuildNumWasFromXmljj`
- [x] Continues to increment build numbers.

### [x] Build Command Line

- [x] Adding `/p:BuildNum=9999` to `dotnet build` outputs package with version ending with `9999`.
- [x] It saved `9999 + 1 = 10000` back to `BuildNum.xml`.
- [x] This is ok for now, but it might not need to save that back in the future in this case.

