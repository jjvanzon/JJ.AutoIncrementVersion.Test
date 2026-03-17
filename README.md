JJ.AutoIncrementVersion.Test
============================

Isolated repo with manual tests for `JJ.AutoIncrementVersion` ([NuGet](https://www.nuget.org/packages/JJ.AutoIncrementVersion), [GitHub](https://github.com/jjvanzon/JJ.AutoIncrementVersion))

With a separate repo, the whole MSBuild set-up in the main repo doesn't interfere with the test.

Things are configured so that when you compile for `Debug` you get `BuildNum` `0` and when you compile for `Release` you get an incremental `BuildNum` coming from the `BuildNum.xml`. This is by design and tests if conditional `BuildNum.xml` inclusion works. (`BuildNum.xml` updates can cause rebuild of all projects, making the build slower. This is an option to conditionally prevent that for tooling optimization.)

Manual Test
-----------

- [ ] Install `JJ.AutoIncrementVersion` package.
- [ ] Use `$(BuildNum)` in `<Version>`.
- [ ] 1st build should fail
- [ ] But should auto-create `BuildNum.xml` and `Directory.Build.props`.
- [ ] 2nd build should succeed.
- [ ] Version should auto-increment
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
