using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DevTools.Testing.Utils;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

public class Quickstarter
{
    private Type _quickstart;
    public Quickstarter(string quickstartName)
    {
        var type = GenTypes.AllTypesWithAttribute<QuickstartAttribute>().FirstOrDefault(x =>
            string.Equals(x.GetCustomAttribute<QuickstartAttribute>().Name, quickstartName, StringComparison.InvariantCultureIgnoreCase));
        if (type == null)
            throw new ArgumentException($"Quickstart {quickstartName} not found");
        _quickstart = type;
    }

    public void Run()
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            CoroutineObject.Instance.StartCoroutine(QuickstarterCoroutine());
        });
    }

    private IEnumerator QuickstarterCoroutine()
    {
        yield return null;

        var quickstartScene = _quickstart.CreateInstance();
        var settings = new TestRunner.FixtureGameSettings(quickstartScene);
        var config = new TestConfig();
        yield return GameGen.GenerateWorldRoutine(settings.worldGen ?? config.world, settings.mapGen ?? config.map,
            settings.scenario, settings.storyteller);

        var setup = quickstartScene
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Where(x => x.HasAttribute<OneTimeSetUpAttribute>() || x.HasAttribute<SetUpAttribute>());
        foreach (var setupMethod in setup)
        {
            if (setupMethod.ReturnType == typeof(IEnumerator))
                yield return setupMethod.Invoke(quickstartScene, null);
            else
                setupMethod.Invoke(quickstartScene, null);
        }

        yield return new WaitUntil(() => Current.ProgramState != ProgramState.Playing);
        Debugger.Break();
    }
}