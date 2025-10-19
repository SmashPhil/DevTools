using System;
using System.Threading;
using UnityEngine;
using Verse;

namespace DevTools;

public class Dialog_TestSuspension : Window
{
  private readonly string message;
  private readonly CancellationTokenSource token;
  private float secondsTimeOut;

	private bool hasTimeOut;

  public Dialog_TestSuspension(string message, float secondsTimeOut, CancellationTokenSource token)
  {
    this.message = message;
    this.secondsTimeOut = secondsTimeOut;
    this.token = token;

		hasTimeOut = secondsTimeOut > 0;

		doCloseButton = true;
    draggable = true;
    absorbInputAroundWindow = false;
    forcePause = false;
    preventCameraMotion = false;
  }

  public override Vector2 InitialSize => new(250, 150);

  protected override void SetInitialSizeAndPosition()
  {
    const float FloatWindowOffset = 50;

    base.SetInitialSizeAndPosition();
    windowRect.position = new Vector2(FloatWindowOffset, FloatWindowOffset);
  }

  public override void Close(bool doCloseSound = true)
  {
    base.Close(doCloseSound);
    SignalStop();
  }

  public override void WindowUpdate()
  {
		if (!hasTimeOut)
			return;

    secondsTimeOut -= Time.deltaTime;
    if (secondsTimeOut <= 0)
      Close();
  }

  public override void DoWindowContents(Rect inRect)
  {
    using TextBlock labelBlock = new(GameFont.Small, TextAnchor.UpperCenter);

		Rect labelRect = inRect with { height = 50 };
		Widgets.Label(labelRect, $"Close this dialog when you are ready to continue.");
		labelRect.y = labelRect.yMax;
		if (hasTimeOut)
		{
			TimeSpan time = TimeSpan.FromSeconds(secondsTimeOut);
			Widgets.Label(labelRect, $"{time.Minutes:D2}:{time.Seconds:D2}");
		}
		
		

		if (!message.NullOrEmpty())
    {
      using TextBlock messageBlock = new(GameFont.Small, TextAnchor.UpperLeft);
			Widgets.Label(labelRect, message);
			labelRect.y = labelRect.yMax;
    }
	}

  private void SignalStop()
  {
    token.Cancel();
  }
}