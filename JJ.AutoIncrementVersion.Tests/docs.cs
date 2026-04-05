namespace JJ.AutoIncrementVersion.Tests.docs;

/// <summary>
/// <para>
///   Repeats rebulds and checks BuildNum increases.
/// </para>
/// <para>
///   <list type="number">
///     <item>Reads the existing BuildNum.xml</item>
///     <item>Rebuilds</item>
///     <item>Gets the output package file name.</item>
///   </list>
/// </para>
/// <para>
///   Checks:
///   <list type="bullet">
///     <item>If package name has BuidNum.</item>
///     <item>If BuildNum.xml increments.</item>
///   </list>
/// </para>
/// </summary>
internal struct _rebuildsincrement;
