using System;
using System.Collections;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;
using Verse.Profile;

namespace DevTools.Testing.Utils;

public static class GameGen
{
    private static TransitionState state = TransitionState.Idle;
    public static IEnumerator GenerateWorldRoutine(WorldGenerationSettings worldGenSettings,
        MapGenerationSettings mapGenSettings, [CanBeNull] Scenario scenario, [CanBeNull] Storyteller storyteller)
    {
        using GenStepWarningDisabler disabler = new();
        GenerateWorld(worldGenSettings, mapGenSettings, scenario, storyteller);
        yield return WaitTillLoaded(ProgramState.Playing);

        // Skip 1 extra frame to allow for game to execute its single tick on load
        yield return null;

        var currentMap = Find.CurrentMap ?? Find.Maps.FirstOrFallback();
        if (currentMap == null)
        {
            Log.Warning("Failed to generate map after creating world.");
            yield return GenerateMapRoutine(mapGenSettings);
        }
        else
        {
            if (!mapGenSettings.postGenerationActions.NullOrEmpty())
            {
                foreach (Action action in mapGenSettings.postGenerationActions)
                {
                    action();
                }
            }
            Verse.Current.Game.CurrentMap = currentMap;
        }
    }

    public static IEnumerator GenerateMapRoutine(MapGenerationSettings mapGenSettings)
    {
        var currentMap = Current.Game.CurrentMap;
        if (currentMap != null)
        {
            Verse.Current.Game.DeinitAndRemoveMap(currentMap, notifyPlayer: false);
        }
        using GenStepWarningDisabler disabler = new();
        currentMap = GenerateMap(mapGenSettings);
        yield return WaitTillLoaded(ProgramState.Playing);

        // Skip 1 extra frame to allow for game to render single frame, otherwise gpu buffer keeps stacking frames.
        yield return null;

        Verse.Current.Game.CurrentMap = currentMap;

        if (!mapGenSettings.postGenerationActions.NullOrEmpty())
        {
            foreach (Action action in mapGenSettings.postGenerationActions)
            {
                action();
            }
        }
    }

    private static void GenerateWorld(WorldGenerationSettings worldGenSettings,
        MapGenerationSettings mapGenSettings, Scenario scenario = null, Storyteller storyteller = null)
    {
        LongEventHandler.QueueLongEvent(delegate
        {
            Verse.Current.Game?.Dispose();
            InitGame(worldGenSettings, mapGenSettings, scenario, storyteller);
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.GameInitData.PrepForMapGen();
                Find.Scenario.PreMapGenerate();
            }, "Play", "GeneratingMap", doAsynchronously: true, exceptionHandler: FailTestWhileTransitioning);
        }, "GeneratingMap", doAsynchronously: true, exceptionHandler: FailTestWhileTransitioning);
    }

    private static Map GenerateMap(MapGenerationSettings mapGenSettings)
    {
        Map map = null;
        try
        {
            World world = Find.World;
            Assert.IsNotNull(world);
            PlanetTile tile = GetFreeTile();
            Assert.IsTrue(tile.Valid);
            world.grid[tile].PrimaryBiome = mapGenSettings.biome ?? BiomeDefOf.TemperateForest;
            IntVec3 size = new(mapGenSettings.size, 1, mapGenSettings.size);
            Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            settlement.Tile = tile;
            settlement.SetFaction(mapGenSettings.Faction);
            Find.WorldObjects.Add(settlement);
            map = MapGenerator.GenerateMap(size, settlement,
                mapGenSettings.mapGeneratorDef ?? MapGenerationSettings.Default.mapGeneratorDef,
                mapGenSettings.extraGenStepDefs);
            CameraJumper.TryJump(map.Center, map, mode: CameraJumper.MovementMode.Cut);
        }
        catch (Exception ex)
        {
            FailTestWhileTransitioning(ex);
        }
        return map;

        static int GetFreeTile()
        {
            World world = Find.World;
            WorldGrid grid = world.grid;
            int nextBest = 0;
            for (int i = 0; i < grid.TilesCount; i++)
            {
                if (!world.worldObjects.AnyMapParentAt(i))
                {
                    return i;
                }
                if (!world.worldObjects.AnySettlementAt(i))
                {
                    nextBest = i;
                }
            }
            MapParent parent = world.worldObjects.MapParentAt(nextBest);
            if (parent != null)
            {
                Verse.Current.Game.DeinitAndRemoveMap(parent.Map, notifyPlayer: false);
                parent.Destroy();
            }
            return nextBest;
        }
    }

    private static void InitGame(WorldGenerationSettings worldGenSettings,
        MapGenerationSettings mapGenSettings, Scenario scenario = null, Storyteller storyteller = null)
    {
        MemoryUtility.ClearAllMapsAndWorld();
        Game game = new();
        GameInitData gameInitData = new()
        {
            startingSeason = worldGenSettings.startingSeason,
            startingPawnsRequired = worldGenSettings.startingPawnsRequired,
            startingXenotypesRequired = worldGenSettings.startingXenotypesRequired,
            startingMutantsRequired = worldGenSettings.startingMutantsRequired,
            allowedDevelopmentalStages = worldGenSettings.allowedDevelopmentalStages,
            startingSkillsRequired = worldGenSettings.startingSkillsRequired
        };

        if (mapGenSettings != null)
        {
            gameInitData.mapSize = mapGenSettings.size;
            gameInitData.mapGeneratorDef = mapGenSettings.mapGeneratorDef;
        }

        Verse.Current.ProgramState = ProgramState.Entry;
        Game.ClearCaches();
        Verse.Current.Game = game;
        Verse.Current.Game.InitData = gameInitData;
        Verse.Current.Game.Scenario = scenario ?? ScenarioDefOf.Crashlanded.scenario;
        Find.Scenario.PreConfigure();
        Verse.Current.Game.storyteller = storyteller ?? new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
        Verse.Current.Game.World = WorldGenerator.GenerateWorld(worldGenSettings.percent,
            GenText.RandomSeedString(),
            worldGenSettings.rainfall, worldGenSettings.temperature, worldGenSettings.population,
            worldGenSettings.landmarkDensity);
        Find.GameInitData.ChooseRandomStartingTile();
        if (mapGenSettings?.biome != null)
        {
            Find.WorldGrid[Find.GameInitData.startingTile].PrimaryBiome = mapGenSettings.biome;
        }

        Find.Scenario.PostIdeoChosen();
    }

    private static IEnumerator WaitTillLoaded(ProgramState programState)
    {
        state = TransitionState.Transitioning;
        try
        {
            while (Verse.Current.ProgramState != programState || LongEventHandler.AnyEventNowOrWaiting)
            {
                if (state == TransitionState.Failed)
                    yield break;

                yield return null;
            }
        }
        finally
        {
            state = TransitionState.Idle;
        }
    }

    private static void FailTestWhileTransitioning(Exception ex)
    {
        Log.Error("ErrorWhileGeneratingMap".Translate());
        Scribe.ForceStop();
        state = TransitionState.Failed;
    }

    private enum TransitionState
    {
        Idle,
        Transitioning,
        Failed
    }
}