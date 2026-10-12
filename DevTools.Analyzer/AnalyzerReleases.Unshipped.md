; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DT0001 | Usage | Error | Test and TestCase attributes must not be combined
DT0002 | Usage | Warning | Save loading bypasses generation settings
DT0003 | Usage | Warning | Test fixture contains no tests
DT0004 | Usage | Error | Test fixtures require a supported public constructor
DT0005 | Usage | Error | SetUp and TearDown methods must return void
DT0006 | Usage | Error | Test parameters require a source attribute
DT0007 | Usage | Error | ParametersSource must identify a valid source
DT0008 | Usage | Error | TestCase argument array must not be null
DT0009 | Usage | Error | Test methods must return void or IEnumerator
