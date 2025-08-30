namespace DevTools.UnitTesting;

public enum Priority
{
  Last = int.MinValue,
  BelowNormal = -100,
  Normal = 0,
  AboveNormal = 100,
  First = int.MaxValue
}