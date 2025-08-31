namespace DevTools.Testing;

// Enum values are ordered from worst to best in the context that lower
// values will have priority on tabulated test results. Order matters!!
public enum Status
{
  Failed,
  Canceled,
  Skipped,
  Passed,
  Pending,
  NotRun,
}