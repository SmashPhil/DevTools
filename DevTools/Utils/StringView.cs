using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DevTools;

[DebuggerDisplay("{ToString(),raw}")]
internal struct StringView
{
  public int start;
  public int length;
  public string source;

  public StringView(string source, int start, int length)
  {
    if (source is not null && start + length > source.Length)
      throw new InvalidOperationException("StringView length is further than its source.");

    this.source = source;
    this.start = start;
    this.length = length;
  }

  public bool IsEmpty => length == 0;

  public override readonly string ToString()
  {
    if (length == 0)
      return "";

    return length == source.Length ? source : source.Substring(start, length);
  }

  public static bool operator ==(StringView view, string str)
  {
    return view.Equals(str);
  }

  public static bool operator !=(StringView view, string str)
  {
    return !(view == str);
  }

  public override readonly bool Equals(object obj)
  {
    return obj is StringView view && Equals(view);
  }

  public override readonly int GetHashCode()
  {
    return ToString().GetHashCode();
  }

  public readonly bool Equals(StringView view)
  {
    return view.length == length && view.start == start && view.source == source;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly bool Equals(string value)
  {
    if (value is null || value.Length != length)
      return false;

    if (length == 0)
      return true;

    return ReferenceEquals(source, value) ||
           string.CompareOrdinal(source, start, value, 0, length) == 0;
  }
}