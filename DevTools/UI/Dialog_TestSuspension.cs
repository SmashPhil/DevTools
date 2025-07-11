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

  public Dialog_TestSuspension(string message, float secondsTimeOut, CancellationTokenSource token)
  {
    this.message = message;
    this.secondsTimeOut = secondsTimeOut;
    this.token = token;
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
    secondsTimeOut -= Time.deltaTime;
    if (secondsTimeOut <= 0)
      Close();
  }

  public override void DoWindowContents(Rect inRect)
  {
    using TextBlock labelBlock = new(GameFont.Medium, TextAnchor.UpperCenter);
    Rect labelRect = inRect with { height = 50 };
    TimeSpan time = TimeSpan.FromSeconds(secondsTimeOut);
    Widgets.Label(labelRect, $"{time.Minutes:D2}:{time.Seconds:D2}");

    if (!message.NullOrEmpty())
    {
      using TextBlock messageBlock = new(GameFont.Small, TextAnchor.UpperLeft);
      labelRect.y = labelRect.yMax;
      Widgets.Label(labelRect, message);
    }
  }

  private void SignalStop()
  {
    token.Cancel();
  }
}