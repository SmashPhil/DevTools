using System;

namespace DevTools;

[Serializable]
public class AssertFailException : Exception
{
  public AssertFailException()
  {
  }

  public AssertFailException(string message) : base(message)
  {
  }

  public AssertFailException(string message, Exception innerException) : base(message,
    innerException)
  {
  }
}