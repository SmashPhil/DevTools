using System;
using DevTools.Benchmarking;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
[StaticConstructorOnStartup]
public class ExplorerColumn : IDataColumn
{
	internal const float LineHeight = 30;

	private static readonly Color InactiveColor = new(0.37f, 0.37f, 0.37f, 0.8f);

	private static readonly Texture2D CheckNo;
	private static readonly Texture2D CheckYes;
	private static readonly Texture2D CheckJobCanceled;
	private static readonly Texture2D CheckJobRunning;
	private static readonly Texture2D CheckJobNotRun;

	static ExplorerColumn()
	{
		CheckNo = EmbeddedResourceLoader.LoadTexture("DevTools.Images.Icon_TestFailed.png");
		CheckYes = EmbeddedResourceLoader.LoadTexture("DevTools.Images.Icon_TestPassed.png");
		CheckJobCanceled = EmbeddedResourceLoader.LoadTexture("DevTools.Images.Icon_TestCanceled.png");
		CheckJobRunning = EmbeddedResourceLoader.LoadTexture("DevTools.Images.Icon_TestRunning.png");
		CheckJobNotRun = EmbeddedResourceLoader.LoadTexture("DevTools.Images.Icon_TestNotRan.png");
	}

	public ExplorerColumn(Type type, float width, float padding = 5)
	{
		ColumnType = type;
		Width = width;
		Padding = padding;
	}

	public Type ColumnType { get; }

	public float Width { get; }

	public float Padding { get; }

	public TextAnchor HeaderAnchor { get; set; } = TextAnchor.MiddleLeft;

	public TextAnchor Anchor { get; set; } = TextAnchor.MiddleLeft;

	public string Name
	{
		get
		{
			return ColumnType switch
			{
				Type.Tests        => "Test",
				Type.Duration     => "Duration",
				Type.Traits       => "Traits",
				Type.ErrorMessage => "Error Message",
				_                 => throw new NotImplementedException()
			};
		}
	}

	public void Draw(Rect rect, ITestCase testCase)
	{
		rect.xMin += Padding;
		rect.xMax -= Padding;
		switch (ColumnType)
		{
			case Type.Tests:
				DrawTests(rect, testCase);
			break;
			case Type.Duration:
				DrawDuration(rect, testCase);
			break;
			case Type.Traits:
				DrawTrait(rect, testCase);
			break;
			case Type.ErrorMessage:
				DrawErrorMessage(rect, testCase);
			break;
			default:
				throw new NotImplementedException(nameof(Type));
		}
	}

	private static void DrawTests(Rect rect, ITestCase testCase)
	{
		const float ResultIconSize = 20;

		Rect testChkRect = (rect with
		{
			size = new Vector2(LineHeight, LineHeight)
		}).ContractedBy((LineHeight - ResultIconSize) / 2);
		Rect labelRect = rect with
		{
			x = testChkRect.xMax,
			width = rect.width - testChkRect.width
		};
		// +5 to pad a bit between the label and result tick mark
		labelRect.xMin += 5;

		CheckboxDraw(testChkRect, testCase.Status, false);
		Widgets.Label(labelRect, testCase.Name);

		string description = testCase.MetaData.Get<string>(MetaDataName.Description);
		if (!description.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, description);
		}
	}

	private static void DrawDuration(Rect rect, ITestCase testCase)
	{
		if (testCase.Status < Status.NotRun)
			Widgets.Label(rect, TimeLabel(testCase.Duration.Mean));
	}

	private static void DrawTrait(Rect rect, ITestCase testCase)
	{
	}

	private static void DrawErrorMessage(Rect rect, ITestCase testCase)
	{
		if (testCase is { Status: Status.Skipped or Status.Failed })
		{
			Widgets.Label(rect, testCase.FailLabel);
		}
	}

	private static void CheckboxDraw(Rect rect, Status status, bool disabled)
	{
		if (disabled)
			GUI.color = InactiveColor;

		Texture2D image = status switch
		{
			Status.Failed   => CheckNo,
			Status.Canceled => CheckJobCanceled,
			Status.Skipped  => CheckJobCanceled,
			Status.Passed   => CheckYes,
			Status.Pending  => CheckJobRunning,
			Status.NotRun   => CheckJobNotRun,
			_               => throw new NotImplementedException(),
		};
		GUI.DrawTexture(rect, image);
		if (!disabled)
		{
			TooltipHandler.TipRegion(rect, StatusLabel(status));
		}
		GUI.color = Color.white;
	}

	private static string StatusLabel(Status status)
	{
		return status switch
		{
			Status.Failed   => "Failed",
			Status.Canceled => "Canceled",
			Status.Skipped  => "Skipped",
			Status.Passed   => "Passed",
			Status.Pending  => "Pending",
			Status.NotRun   => "Not Run",
			_               => throw new NotImplementedException(),
		};
	}

	private static string TimeLabel(double value)
	{
		return value < 1 ?
			$"< 1 {Benchmark.MeasurementSuffix(Benchmark.Measurement.Milliseconds)}" :
			$"{value:0} {Benchmark.MeasurementSuffix(Benchmark.Measurement.Milliseconds)}";
	}

	public enum Type
	{
		Tests,
		Duration,
		Traits,
		ErrorMessage,
	}
}