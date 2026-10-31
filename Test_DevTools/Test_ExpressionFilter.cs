using System;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using MethodType = DevTools.Testing.MethodType;

namespace DevTools.Tests;

[TestFixture(TestType.MainMenu)]
[TestDescription("Filters tests using dotnet test style comparisons and logical expressions.")]
internal class Test_ExpressionFilter
{
  [TestCase("Name=MockTest", ExpectedResult = true)]
  [TestCase("Name==MockTest", ExpectedResult = true)]
  [TestCase("nAmE=MockTest", ExpectedResult = true)]
  [TestCase("  Name = MockTest  ", ExpectedResult = true)]
  [TestCase("Name=Mock", ExpectedResult = false)]
  [TestCase("Name=FakeTest", ExpectedResult = false)]
  private bool Equality(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Name!=MockTest", ExpectedResult = false)]
  [TestCase("Name!=FakeTest", ExpectedResult = true)]
  private bool Inequality(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Name~Test", ExpectedResult = true)]
  [TestCase("Name=~Test", ExpectedResult = true)]
  [TestCase("Name~test", ExpectedResult = true)]
  [TestCase("Name~Fake", ExpectedResult = false)]
  private bool Contains(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Name!~Test", ExpectedResult = false)]
  [TestCase("Name!~World", ExpectedResult = true)]
  [TestCase("Name!~test", ExpectedResult = false)]
  private bool Excludes(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("MockTest", ExpectedResult = true)]
  [TestCase("FakeTest", ExpectedResult = false)]
  [TestCase("FullyQualifiedName~FilterSample.MockTest", ExpectedResult = true)]
  [TestCase("FullyQualifiedName=MockTest", ExpectedResult = false)]
  private bool FullyQualifiedName(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Name=MockTest&Category=MethodCategory", ExpectedResult = true)]
  [TestCase("Name=MockTest&&Category=MethodCategory", ExpectedResult = true)]
  [TestCase("Name = MockTest & Category = MethodCategory", ExpectedResult = true)]
  [TestCase("Name=MockTest&Category=Missing", ExpectedResult = false)]
  private bool And(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Name=FakeTest|Category=MethodCategory", ExpectedResult = true)]
  [TestCase("Name=FakeTest||Category=MethodCategory", ExpectedResult = true)]
  [TestCase("Name=FakeTest|Category=Missing", ExpectedResult = false)]
  private bool Or(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Name=MockTest|Category=Missing&Type=Playing", ExpectedResult = true)]
  [TestCase("Name=FakeTest|Category=MethodCategory&Type=Playing", ExpectedResult = false)]
  [TestCase("(Name=MockTest|Category=Missing)&Type=Playing", ExpectedResult = false)]
  [TestCase("Name=MockTest&(Category=Missing|Type=MainMenu)", ExpectedResult = true)]
  [TestCase("((Name=FakeTest|Category=MethodCategory)&Type=MainMenu)", ExpectedResult = true)]
  private bool PrecedenceAndGrouping(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Category=FixtureCategory", ExpectedResult = true)]
  [TestCase("Category=MethodCategory", ExpectedResult = true)]
  [TestCase("Category=SharedCategory", ExpectedResult = true)]
  [TestCase("Category=Missing", ExpectedResult = false)]
  [TestCase("Category~Category", ExpectedResult = true)]
  [TestCase("Category!=MethodCategory", ExpectedResult = false)]
  [TestCase("Category!=Missing", ExpectedResult = true)]
  [TestCase("Category!~Category", ExpectedResult = false)]
  [TestCase("Category!~Missing", ExpectedResult = true)]
  private bool Categories(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Class=FilterSample", ExpectedResult = true)]
  [TestCase("Class~FilterSample", ExpectedResult = true)]
  [TestCase("Class=Test_ExpressionFilter", ExpectedResult = false)]
  [TestCase("Method=MockTest", ExpectedResult = true)]
  [TestCase("Method=FilterSample.MockTest", ExpectedResult = true)]
  [TestCase("Method~FilterSample.Mock", ExpectedResult = true)]
  [TestCase("Method=FakeTest", ExpectedResult = false)]
  [TestCase("Module=Test_DevTools", ExpectedResult = true)]
  [TestCase("Module=OtherModule", ExpectedResult = false)]
  [TestCase("Namespace=DevTools.Tests", ExpectedResult = true)]
  [TestCase("Namespace~Tools", ExpectedResult = true)]
  [TestCase("Type=MainMenu", ExpectedResult = true)]
  [TestCase("Type=Playing", ExpectedResult = false)]
  private bool BuiltInProperties(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [TestCase("Status=Failed", ExpectedResult = true)]
  [TestCase("Status=Passed", ExpectedResult = true)]
  [TestCase("Status=NotRun", ExpectedResult = false)]
  [TestCase("Status!=Failed", ExpectedResult = false)]
  [TestCase("Status!=NotRun", ExpectedResult = true)]
  [TestCase("Description=\"Fixture detail\"", ExpectedResult = true)]
  [TestCase("Description=\"Method & detail\"", ExpectedResult = true)]
  [TestCase("Description~detail", ExpectedResult = true)]
  [TestCase("Description=Missing", ExpectedResult = false)]
  [TestCase("Unknown=Anything", ExpectedResult = false)]
  [TestCase("Unknown!=Anything", ExpectedResult = true)]
  private bool StatusAndMetadata(string expression)
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new(expression);
    return filter.Filter(fixture, function);
  }

  [Test]
  private void QualifiedNames()
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    Expect.IsTrue(new ExpressionFilter($"Class={fixture.Type.FullName}").Filter(fixture, function));
    Expect.IsTrue(new ExpressionFilter($"Class=\"{fixture.Type.AssemblyQualifiedName}\"").Filter(fixture, function));
    Expect.IsTrue(new ExpressionFilter($"Method={fixture.Type.FullName}.{function.MethodInfo.Name}").Filter(fixture, function));
    Expect.IsTrue(new ExpressionFilter($"FullyQualifiedName={function.FullName}").Filter(fixture, function));
    Expect.IsFalse(new ExpressionFilter($"FullyQualifiedName={fixture.FullName}").Filter(fixture, function));
  }

  [Test]
  private void RejectInvalidInput()
  {
    Expect.Throws<ArgumentException>(() => _ = new ExpressionFilter(null));
    Expect.Throws<ArgumentException>(() => _ = new ExpressionFilter(""));
    Expect.Throws<ArgumentException>(() => _ = new ExpressionFilter("  \t  "));
    Expect.Throws<InvalidOperationException>(() => _ = new ExpressionFilter("Name="));
    Expect.Throws<InvalidOperationException>(() => _ = new ExpressionFilter("Name=MockTest&"));
    Expect.Throws<InvalidOperationException>(() => _ = new ExpressionFilter("(Name=MockTest"));
    Expect.Throws<InvalidOperationException>(() => _ = new ExpressionFilter("Name=MockTest)"));
    Expect.Throws<ArgumentException>(() => _ = new ExpressionFilter("(Name=MockTest)=true"));
  }

  [Test]
  private void RejectNullCases()
  {
    (TestFixture fixture, TestFunction function) = CreateCases();
    ExpressionFilter filter = new("Name=MockTest");
    Expect.Throws<ArgumentNullException>(() => filter.Filter(null, function));
    Expect.Throws<ArgumentNullException>(() => filter.Filter(fixture, null));
  }

  private static (TestFixture Fixture, TestFunction Function) CreateCases()
  {
    Type type = typeof(FilterSample);
    MethodInfo method = AccessTools.Method(type, nameof(FilterSample.MockTest));
    TestFixture fixture = new(new AssemblyModule(type.Assembly), type, TestType.MainMenu)
    {
      Status = Status.Failed
    };
    fixture.MetaData.Load(type);
    TestFunction function = new(fixture, method, MethodType.Test)
    {
      Status = Status.Passed
    };
    function.MetaData.Load(method);
    return (fixture, function);
  }

  [TestCategory("FixtureCategory", "SharedCategory")]
  [TestDescription("Fixture detail")]
  private sealed class FilterSample
  {
    [TestCategory("MethodCategory", "SharedCategory")]
    [TestDescription("Method & detail")]
    public void MockTest()
    {
    }
  }
}
