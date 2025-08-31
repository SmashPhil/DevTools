using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public class SmokeTestConfig : BaseTestConfig
{
	private bool stopOnFailure;

	public override bool StopOnFailure => stopOnFailure;

	public override int RetryAttempts => 0;
}