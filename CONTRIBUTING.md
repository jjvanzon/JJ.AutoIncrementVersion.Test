TODO Overview – JJ.AutoIncrementVersion.TestSuite
=================================================

Overview of TODOs found across the automated test suite.

TODO by File
------------

### [ ] CommandLineAndUpgradeTests.cs

- [ ] Assert `BuildNumWasFromXmljj` was present *before* removal. (Line 74)

TODO Grouped by Theme
---------------------

### [ ] 15. Minor

- [ ] Assert-before-remove in upgrade test.
- [ ] (TestHelper.cs lines 93–94; CommandLineAndUpgradeTests.cs line 74; InstallTests.cs line 46)

Done by File
------------

### [x] TestHelper.cs

- [x] `_ctx` (TestContext) only used for logging. `Debug`+`Console` might be enough — saves dependency/init/cleanup/property. (Line 21) 
- [x] Constructor path-finding should run once (static), not per-test. (Line 34) 
- [x] `.sln` file isn't there in an NCrunch context. (Line 35) 
- [x] Can we make assumptions about relative locations? (Line 36) 
- [x] Maybe copy necessary test project files as embedded resources into `.TestSuite` project, save to disk relative to assembly. Helps isolation/parallel tests. (Line 37) 
- [x] Three logging sinks (`_ctx.WriteLine`, `Console`, `Debug`) — maybe only one is needed. (Line 58) 
- [x] Rename `CommandResult` → `CommandLineResult` to avoid ambiguity with non-CLI "commands." (Line 69) 
- [x] `ProcessStartInfo` ceremony is duplicated between `RunDotnet` and `RunDotnetAtSolutionDir`. (Line 75) 
- [x] `e.Data ?? ""` would simplify the null check pattern. (Line 93–94) 
- [x] 2-minute timeout should be a central config variable. > Better keep it hard-coded to simplify test files. (Line 98) 
- [x] Should check exit code inside `RunDotnet` already? Fail fast? (Line 108) 
- [x] stderr: Throw instead to stop test? (Line 110) 
- [x] Build/Rebuild error out with "README.md does not exist" because `$(SolutionDir)` isn't set when building a `.csproj` standalone. Need to fix. (Line 154) 
- [x] `UninstallPackage` errors out: "Found more than one project". (Line 171–173) 
- [x] Delete log says "Deleted" even when `BuildNum.xml` didn't exist. (Line 182) 
- [x] Delete log says "Deleted" even when `Directory.Build.props` didn't exist. (Line 188) 
- [x] Log says "initial state set" but errors were ignored, so it's potentially false. (Line 310) 
- [x] `Cleanup()` does nothing now since `GitRestoreAll` is disabled. Remove cleanup logic entirely? > Replaced by embedded resources written and cleaned up from isolated temp dir. (Line 313) 
- [x] Fixed `PackageVersion = "4.2.5746"` is bad. Should use latest from pre-release package feed. Tests would never test the latest otherwise. (Line 25) 
- [x] `UninstallPackage()` result is ignored in `SetInitialState`. If it fails, next steps blindly continue. (Line 306) 
- [x] Hardcoded `"4.3.0"` — should read current major/minor from csproj. (Line 309) 


### [x] UninstallReinstallTests.cs

- [x] One-test-one-step principle violated. Split Reinstall. Also: tests share same dependency, can't run in parallel — enforce that. (Line 71)
- [x] `Build()` return value (error) is swallowed. (Line 85)

- [x] `UninstallPackage()` failure is ignored. (Line 43)
- [x] `UninstallPackage()` failure is ignored. (Line 88)

### [x] RunWithoutPackageTests.cs

- [x] Use global usings (new `Usings.cs`). (Line 1) 
- [x] `Tests` subfolder not necessary — move test classes up a directory. (Line 6) 
- [x] None of the tests can run in parallel; must run in isolation. (Line 7) 
- [x] Rename `_h` to `_helper` or `_testHelper`. (Line 12)
- [x] Error for uninstall is swallowed in `SetInitialState`, so we don't even know the package state. (Line 39)

### [x] FirstUseTests.cs

- [x] Error text doesn't match `"Invalid NuGet version string"`. Actual error is `"'4.3.' is not a valid version string. (Parameter 'value')"`. Pattern needs updating. (Line 61)

### [x] InstallTests.cs

- [x] Use `AssertCore` (global `using static`) from `JJ.Framework.Testing.Core` (JJs-Dev-Package-Feed). (Line 46)

### [x] ManualEditTests.cs

- [x] Assertion message references `build1.Error` but actual error is embedded in `build1.Output`. (Line 41)

### [x] CommandLineAndUpgradeTests.cs

- [x] Error is in `build1.Output`, not `build1.Error`. (Line 87)

### [x] All test classes (repeated 8×)

- [x] `[TestInitialize]`/`[TestCleanup]` boilerplate (`Init` and `Cleanup`) could be moved inline into the test methods themselves.

Done Grouped by Theme
---------------------

### [x] 1. Broken: `$(SolutionDir)` / README.md not found

- [x] Builds fail when running `.csproj` standalone because `$(SolutionDir)` isn't set.
      (TestHelper.cs line 154)

### [x] 2. Broken: `UninstallPackage` errors out

- [x] `dotnet remove` says "Found more than one project" — likely because the working directory already contains the `.csproj` and the full path is also specified.  
      (TestHelper.cs lines 171–173)

### [x] 3. Broken: Error text mismatch

- [x] `"Invalid NuGet version string"` doesn't match the actual SDK error `"'4.3.' is not a valid version string"`.  
      (FirstUseTests.cs line 61)

### [x] 4. Swallowed errors

- [x] `UninstallPackage`, `Build`, and `SetInitialState` results are silently ignored in many places — tests may pass while setup actually failed.  
      (TestHelper.cs line 306; UninstallReinstallTests.cs lines 43, 85, 88; RunWithoutPackageTests.cs line 39)

### [x] 5. Error in `.Output` not `.Error`

- [x] Assertion messages reference `build1.Error` but actual build errors are embedded in `build1.Output`.
      (ManualEditTests.cs line 41; CommandLineAndUpgradeTests.cs line 87)

### [x] 6. Test isolation / no parallel execution

- [x] Tests share the same project files on disk and cannot run in parallel. Needs enforcement (e.g. `[DoNotParallelize]`).  
      (RunWithoutPackageTests.cs line 7; UninstallReinstallTests.cs line 71)

### [x] 7. Move files up / global usings

- [x] Drop `Tests\` subfolder. 
- [x] Add `Usings.cs` with global usings.
- [x] (RunWithoutPackageTests.cs lines 1, 6)

### [x] 8. Remove `[TestInitialize]`/`[TestCleanup]` boilerplate

- [x] Inline into test methods. Remove dead `Cleanup()`.
      (All test classes; TestHelper.cs line 313)

### [x] 9. Naming

- [x] `_h` → `_helper` or `_testHelper`. `CommandResult` → `CommandLineResult`.
      (RunWithoutPackageTests.cs line 12; TestHelper.cs line 69)

### [x] 10. DRY: deduplicate process execution

- [x] `RunDotnet` and `RunDotnetAtSolutionDir` share nearly identical `ProcessStartInfo` ceremony.
      (TestHelper.cs line 75)

### [x] 11. Config / resilience / NCrunch

- [x] ~~Timeout from config.~~ > Unnecessary
- [x] ~~Static path init.~~ > Irrelevant
- [x] Embedded resources for NCrunch compatibility.
      (TestHelper.cs lines 34–37, 98)

### [x] 12. Delete logging accuracy

- [x] Log whether file actually existed before deletion.  
      (TestHelper.cs lines 182, 188)

### [x] 13. Hardcoded versions

- [x] `"4.3.0"` and `PackageVersion = "4.2.5746"` - should read dynamically.  
      (TestHelper.cs lines 25, 309)

### [x] 14. Logging sinks

- [x] Drop `TestContext` dependency, simplify to `Console`+`Debug`.
      (TestHelper.cs lines 21, 58)

### [x] 15. Minor

- [x] `e.Data ?? ""` null simplification.
- [x] Use `AssertCore` from `JJ.Framework.Testing.Core`.
