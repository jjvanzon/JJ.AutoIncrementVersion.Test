TODO Overview – JJ.AutoIncrementVersion.TestSuite
=================================================

Overview of TODOs found across the automated test suite, grouped by theme.


TODOs by File
-------------

### TestHelper.cs

| Line    | TODO |
|---------|------|
| 21      | ~~`_ctx` (TestContext) only used for logging. `Debug`+`Console` might be enough — saves dependency/init/cleanup/property.~~
| 25      | Fixed `PackageVersion = "4.2.5746"` is bad. Should use latest from pre-release package feed. Tests would never test the latest otherwise.
| 34      | ~~Constructor path-finding should run once (static), not per-test.~~
| 35      | ~~`.sln` file isn't there in an NCrunch context.~~
| 36      | ~~Can we make assumptions about relative locations?~~
| 37      | ~~Maybe copy necessary test project files as embedded resources into `.TestSuite` project, save to disk relative to assembly. Helps isolation/parallel tests.~~
| 58      | ~~Three logging sinks (`_ctx.WriteLine`, `Console`, `Debug`) — maybe only one is needed.~~
| 69      | ~~Rename `CommandResult` → `CommandLineResult` to avoid ambiguity with non-CLI "commands."~~
| 75      | ~~`ProcessStartInfo` ceremony is duplicated between `RunDotnet` and `RunDotnetAtSolutionDir`.~~
| 93–94   | ~~`e.Data ?? ""` would simplify the null check pattern.~~
| 98      | ~~2-minute timeout should be a central config variable.~~ > Better keep it hard-coded to simplify test files.
| 108     | ~~Should check exit code inside `RunDotnet` already? Fail fast?~~
| 110     | ~~stderr: Throw instead to stop test?~~
| 154     | ~~Build/Rebuild error out with "README.md does not exist" because `$(SolutionDir)` isn't set when building a `.csproj` standalone. Need to fix.~~
| 171–173 | ~~`UninstallPackage` errors out: "Found more than one project".~~
| 182     | ~~Delete log says "Deleted" even when `BuildNum.xml` didn't exist.~~
| 188     | ~~Delete log says "Deleted" even when `Directory.Build.props` didn't exist.~~
| 306     | `UninstallPackage()` result is ignored in `SetInitialState`. If it fails, next steps blindly continue.
| 309     | Hardcoded `"4.3.0"` — should read current major/minor from csproj.
| 310     | ~~Log says "initial state set" but errors were ignored, so it's potentially false.~~
| 313     | ~~`Cleanup()` does nothing now since `GitRestoreAll` is disabled. Remove cleanup logic entirely?~~ > Replaced by embedded resources written and cleaned up from isolated temp dir.

### UninstallReinstallTests.cs

| Line | TODO |
|------|------|
| 43   | `UninstallPackage()` failure is ignored.
| 71   | ~~One-test-one-step principle violated. Split Reinstall. Also: tests share same dependency, can't run in parallel — enforce that.~~
| 85   | ~~`Build()` return value (error) is swallowed.~~
| 88   | `UninstallPackage()` failure is ignored.

### RunWithoutPackageTests.cs

| Line | TODO |
|------|------|
| 1    | ~~Use global usings (new `Usings.cs`).~~
| 6    | ~~`Tests` subfolder not necessary — move test classes up a directory.~~
| 7    | ~~None of the tests can run in parallel; must run in isolation.~~
| 12   | ~~Rename `_h` to `_helper` or `_testHelper`.~~
| 39   | Error for uninstall is swallowed in `SetInitialState`, so we don't even know the package state.

### FirstUseTests.cs

| Line | TODO |
|------|------|
| 61   | ~~Error text doesn't match `"Invalid NuGet version string"`. Actual error is `"'4.3.' is not a valid version string. (Parameter 'value')"`. Pattern needs updating.~~

### InstallTests.cs

| Line | TODO |
|------|------|
| 46   | Use `AssertCore` (global `using static`) from `JJ.Framework.Testing.Core` (JJs-Dev-Package-Feed).

### ManualEditTests.cs

| Line | TODO |
|------|------|
| 41   | ~~Assertion message references `build1.Error` but actual error is embedded in `build1.Output`.~~

### CommandLineAndUpgradeTests.cs

| Line | TODO |
|------|------|
| 74   | Assert `BuildNumWasFromXmljj` was present *before* removal.
| 87   | ~~Error is in `build1.Output`, not `build1.Error`.~~

### ~~All test classes (repeated 8×)~~

| TODO |
|------|
| ~~`[TestInitialize]`/`[TestCleanup]` boilerplate (`Init` and `Cleanup`) could be moved inline into the test methods themselves.~~



Grouped by Theme
----------------

### TODO

#### 13. Hardcoded versions

- [ ] `"4.3.0"` and `PackageVersion = "4.2.5746"` - should read dynamically.  
      *(TestHelper.cs lines 25, 309)*

#### 15. Minor

- [x] `e.Data ?? ""` null simplification.
      > DONE
- [ ] Assert-before-remove in upgrade test.
- [x] ~~Use `AssertCore` from `JJ.Framework.Testing.Core`.~~
      > DONE
- [ ] *(TestHelper.cs lines 93–94; CommandLineAndUpgradeTests.cs line 74; InstallTests.cs line 46)*

### Done

#### 1. ~~Broken: `$(SolutionDir)` / README.md not found~~

- [x] Builds fail when running `.csproj` standalone because `$(SolutionDir)` isn't set.
      *(TestHelper.cs line 154)*
      > DONE

#### 2. ~~Broken: `UninstallPackage` errors out~~

- [x] ~~`dotnet remove` says "Found more than one project" — likely because the working directory already contains the `.csproj` and the full path is also specified.~~  
      ~~*(TestHelper.cs lines 171–173)*~~
      > DONE

#### 3. ~~Broken: Error text mismatch~~

- [x] ~~`"Invalid NuGet version string"` doesn't match the actual SDK error `"'4.3.' is not a valid version string"`.~~  
      ~~*(FirstUseTests.cs line 61)*~~
      > DONE

#### 4. ~~Swallowed errors~~

- [x] ~~`UninstallPackage`, `Build`, and `SetInitialState` results are silently ignored in many places — tests may pass while setup actually failed.~~  
      ~~*(TestHelper.cs line 306; UninstallReinstallTests.cs lines 43, 85, 88; RunWithoutPackageTests.cs line 39)*~~
      > DONE

#### 5. Error in `.Output` not `.Error`

- [ ] Assertion messages reference `build1.Error` but actual build errors are embedded in `build1.Output`.
      *(ManualEditTests.cs line 41; CommandLineAndUpgradeTests.cs line 87)*

#### 6. ~~Test isolation / no parallel execution~~

- [x] Tests share the same project files on disk and cannot run in parallel. Needs enforcement (e.g. `[DoNotParallelize]`).  
      *(RunWithoutPackageTests.cs line 7; UninstallReinstallTests.cs line 71)*
      > Done

#### 7. ~~Move files up / global usings~~

- [x] Drop `Tests\` subfolder. 
      > DONE
- [x] Add `Usings.cs` with global usings.
      > DONE
- [x] *(RunWithoutPackageTests.cs lines 1, 6)*
      > DONE

#### 8. ~~Remove `[TestInitialize]`/`[TestCleanup]` boilerplate~~

- [x] Inline into test methods. Remove dead `Cleanup()`.
      *(All test classes; TestHelper.cs line 313)*
      > DONE

#### 9. ~~Naming~~

- [x] `_h` → `_helper` or `_testHelper`. `CommandResult` → `CommandLineResult`.
      *(RunWithoutPackageTests.cs line 12; TestHelper.cs line 69)*
      > DONE

#### 10. ~~DRY: deduplicate process execution~~

- [x] `RunDotnet` and `RunDotnetAtSolutionDir` share nearly identical `ProcessStartInfo` ceremony.
      *(TestHelper.cs line 75)*
      > DONE

#### 11. ~~Config / resilience / NCrunch~~

- [x] ~~Timeout from config.~~ > Unnecessary
- [x] ~~Static path init.~~ > Irrelevant
- [x] Embedded resources for NCrunch compatibility.
      *(TestHelper.cs lines 34–37, 98)*
      > DONE

#### 12. ~~Delete logging accuracy~~

- [x] ~~Log whether file actually existed before deletion.~~  
      ~~*(TestHelper.cs lines 182, 188)*~~
      > DONE

#### 14. ~~Logging sinks~~

- [x] Drop `TestContext` dependency, simplify to `Console`+`Debug`.
      *(TestHelper.cs lines 21, 58)*
      > DONE
