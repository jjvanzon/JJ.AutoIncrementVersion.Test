AutoInc ScratchPad
==================

```cs
    /// <summary>
    /// <para>Verbosity is passed to the build processes executed in this helper.</para>
    /// Plus:<br/>
    /// - <c>Diagnostic</c> or <c>Detailed</c> will log all build output.<br/>
    /// - <c>Normal</c> and <c>Minimal</c> will only check silently for errors internally.<br/>
    /// - <c>Quiet</c> won't work, because it'll swallow diagnostics used by the logic.
    /// </summary>
    private const string VERBOSITY = Verbosities.Minimal;
```