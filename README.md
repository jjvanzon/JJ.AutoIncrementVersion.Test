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
      - [Other Steps](#other-steps)

Manual Test Plan
----------------

### Initial State

- [x] Uninstall existing `JJ.AutoIncrementVersion` package.
- [x] Go to File Explorer, not Solution Explorer.
- [x] Go to the repository folder 
      (`D:\Repositories\JJ.AutoIncrementVersion.Test`)
- [x] Delete `BuildNum.xml` and `Directory.Build.props`
- [x] Open `JJ.AutoIncrementVersion.Test.csproj`
- [x] Replace `$(BuildNum)` with `0`

### Run Without

- [x] Rebuild solution.
- [x] `Output` shows `JJ.AutoIncrementVersion.Test.4.2.0.nupkg` 
      at least ends with `.0.nupkg`

### Install

- [x] Install `JJ.AutoIncrementVersion` package.
- [x] Rebuild solution
- [x] `Output` shows `JJ.AutoIncrementVersion.Test.4.2.0.nupkg`
      at least ends with `.0.nupkg`
- [x] Auto-creates `BuildNum.xml` content:  
      `<Project><PropertyGroup><BuildNum>1</BuildNum><DisableFastUpToDateCheck>True</DisableFastUpToDateCheck><BuildNumWasFromXmljj>True</BuildNumWasFromXmljj></PropertyGroup></Project>`
- [x] Auto-creates `Directory.Build.props` content:  
      `<Project><PropertyGroup><BuildNum>0</BuildNum></PropertyGroup><Import Project="BuildNum.xml" Condition="Exists('BuildNum.xml')" /></Project>`

### First Use

- [x] Prepare [Initial State](#initial-state) again
- [ ] Install `JJ.AutoIncrementVersion` package.
- [ ] Use `$(BuildNum)` in `<Version>`.
- [ ] 1st build should fail
- [ ] But should auto-create `BuildNum.xml` and `Directory.Build.props`.
- [ ] 2nd build should succeed.
- [ ] Version should auto-increment

### Other Steps

- [ ] Uninstall package
- [ ] .xml and .Build.props should remain
- [ ] Build should succeed
- [ ] Ver should stay frozen
- [ ] Reinstall package
- [ ] Build should succeed, with incremented ver.
- [ ] Delete `Directory.Build.props`
- [ ] Build
- [ ] Should so generic error
- [ ] Build again
- [ ] `Directory.Build.props` should be recreated
- [ ] Delete `BuildNum.xml`
- [ ] Build
- [ ] `BuildNum.xml` should be recreated
- [ ] Versions will start at BuildNum 0 or 1 again.
- [ ] Rerstore original `BuildNum.xml`
- [ ] Versions should continue to increment where it left off.
- [ ] Test what happens if `BuildNumWasFromXmljj` is removed from `BuildNum.xml` (simulating upgrade path)
- [ ] Test compiling for `Release` (increments `BuildNum`) or `Debug` (uses `BuildNum` `0`)
      which tests conditional `BuildNum.xml` inclusion from the `Directory.Build.props`.
