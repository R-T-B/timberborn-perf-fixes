using System;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Timberborn.Common;

namespace Frog
{
  [BepInPlugin("com.frog.perfmod", "Performance Fixes", "1.0.5.0")]
  public class PerfFixPlugin : BaseUnityPlugin
  {
    public static PerfFixPlugin instance;
    private Harmony harmony;
    public static bool perfFixEnabled = true; // Left in for easy debugging.
    public static bool planReserveFixEnabled = true;
    public static bool popFixEnabled = true;
    public static bool harvestIsAllowedFixEnabled = true;
    public static bool gatherGetUnreservedFixEnabled = true;
    public static bool dwellerAssignFixEnabled = true;
    public static bool rangedEffectFixEnabled = true;
    public static bool planterBuildingFixEnabled = true;
    public static bool tickGameobjectFixEnabled = true;
    private void Awake()
    {
      instance = this;
      harmony = new Harmony("com.frog.perfmod");

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.Population.PopulationDataCalculator), "CollectWorkforceData");
        var mPrefix = SymbolExtensions.GetMethodInfo((IReadOnlyList<Component> workers, Timberborn.Population.WorkforceData __result) =>
            PopulationDataCalculatorCollectWorkforceData(workers, ref __result));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.Population.PopulationDataCalculator), "CollectBedData");
        var mPrefix = SymbolExtensions.GetMethodInfo((int numberOfAdults, int numberOfChildren, IEnumerable<Timberborn.DwellingSystem.Dwelling> dwellings, Timberborn.Population.BedData __result) =>
            PopulationDataCalculatorCollectBedData(numberOfAdults, numberOfChildren, dwellings, ref __result));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.Population.PopulationDataCalculator), "CollectEmploymentMetrics");
        var mPrefix = SymbolExtensions.GetMethodInfo((int numberOfAdults, int numberOfBots, IEnumerable<Timberborn.WorkSystem.Workplace> workplaces, Timberborn.WorkerTypesUI.WorkerTypeHelper ____workerTypeHelper, (Timberborn.Population.WorkplaceData beaverWorkplaceData, Timberborn.Population.WorkplaceData golemWorkplaceData) __result) =>
            PopulationDataCollectEmploymentMetrics(numberOfAdults, numberOfBots, workplaces, ____workerTypeHelper, ref __result));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.Planting.PlantBehavior), "ReserveCoordinates");
        var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.Planting.PlantBehavior __instance, GameObject agent, bool prioritized) =>
            PlantBehaviorReserveCoordinates(__instance, agent, prioritized));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.Planting.PlanterBuildingStatusUpdater), "OnNavMeshUpdated");
        var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.Planting.PlanterBuildingStatusUpdater __instance, Timberborn.Navigation.NavMeshUpdate navMeshUpdate) =>
            PlanterBuildingStatusUpdaterOnNavMeshUpdated(__instance, navMeshUpdate));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.Fields.HarvestStarter), "IsAllowed");
        var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.Yielding.YieldRemovingBuilding yieldRemovingBuilding, Timberborn.Yielding.Yielder yielder, bool __result) =>
            HarvestStarterIsAllowed(yieldRemovingBuilding, yielder, ref __result));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.Gathering.GatherWorkplaceBehavior), "get_UnreservedYielders");
        var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.Gathering.GatherWorkplaceBehavior __instance, IEnumerable<Timberborn.Yielding.Yielder> __result) =>
            GatherWorkplaceBehaviorGetUnreservedYielders(__instance, ref __result));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.DwellingSystem.DwellerHomeAssigner), "AssignDweller");
        var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.DwellingSystem.AutoAssignableDwelling dwelling, IEnumerable<Timberborn.Beavers.Beaver> primaryBeavers, IEnumerable<Timberborn.Beavers.Beaver> secondaryBeavers, bool __result) =>
            DwellerHomeAssignerAssignDweller(dwelling, primaryBeavers, secondaryBeavers, ref __result));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.RangedEffectSystem.RangedEffectReceiver), "GetAffectingEffects");
        var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.RangedEffectSystem.RangedEffectReceiver __instance, IReadOnlyList<Timberborn.RangedEffectSystem.RangedEffect> __result) =>
            RangedEffectReceiverGetAffectingEffects(__instance, ref __result));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      {
        var mOriginal = AccessTools.Method(typeof(Timberborn.TickSystem.TickableEntity), "Tick");
        var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.TickSystem.TickableEntity __instance) => TickableEntityTick(__instance));
        harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
      }

      { // EntityRegistration stuff needs to be enabled/disabled all at once, and can't be toggled at runtime.
        {
          var mOriginal = AccessTools.Method(typeof(Timberborn.EntitySystem.EntityRegistry), "AddEntity");
          var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.EntitySystem.EntityRegistry __instance, Timberborn.EntitySystem.EntityComponent entityComponent) => EntityRegistryAddEntity(__instance, entityComponent));
          harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
        }
        {
          var mOriginal = AccessTools.Method(typeof(Timberborn.EntitySystem.EntityRegistry), "RemoveEntity");
          var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.EntitySystem.EntityRegistry __instance, Timberborn.EntitySystem.EntityComponent entityComponent) => EntityRegistryRemoveEntity(__instance, entityComponent));
          harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
        }
        {
          var mOriginal = AccessTools.Method(typeof(Timberborn.EntitySystem.EntityRegistry), "get_Entities");
          var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.EntitySystem.EntityRegistry __instance, IEnumerable<GameObject> __result) => EntityRegistryget_Entities(__instance, ref __result));
          harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
        }
      }
    }

    // public void OnGUI() {
    //   int y = 200;
    //   int step = 50;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "constructionFixEnabled: " + (constructionFixEnabled ? "enabled" : "disabled"))) { constructionFixEnabled = !constructionFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "planReserveFixEnabled: " + (planReserveFixEnabled ? "enabled" : "disabled"))) { planReserveFixEnabled = !planReserveFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "popFixEnabled: " + (popFixEnabled ? "enabled" : "disabled"))) { popFixEnabled = !popFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "harvestIsAllowedFixEnabled: " + (harvestIsAllowedFixEnabled ? "enabled" : "disabled"))) { harvestIsAllowedFixEnabled = !harvestIsAllowedFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "gatherGetUnreservedFixEnabled: " + (gatherGetUnreservedFixEnabled ? "enabled" : "disabled"))) { gatherGetUnreservedFixEnabled = !gatherGetUnreservedFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "dwellerAssignFixEnabled: " + (dwellerAssignFixEnabled ? "enabled" : "disabled"))) { dwellerAssignFixEnabled = !dwellerAssignFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "rangedEffectFixEnabled: " + (rangedEffectFixEnabled ? "enabled" : "disabled"))) { rangedEffectFixEnabled = !rangedEffectFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "planterBuildingFixEnabled: " + (planterBuildingFixEnabled ? "enabled" : "disabled"))) { planterBuildingFixEnabled = !planterBuildingFixEnabled; }
    //   y+=step;
    //   if (GUI.Button(new Rect(100, y, 400, 50), "tickGameobjectFixEnabled: " + (tickGameobjectFixEnabled ? "enabled" : "disabled"))) { tickGameobjectFixEnabled = !tickGameobjectFixEnabled; }
    // }

    // ========================================================================================================
    // EntityRegistry Removal Optimization
    // ----
    //
    // Notes on the original implementation: 
    //     Entities are stored in a List<> in the order they are added, so they can later be looped over in
    //     instantiation order. When entities are removed it just calls List<>.Remove(), which scales linearly
    //     with the total number of entities.
    //
    // Changes:
    //     Entities are now stored in a LinkedList<> (along with a Dict mapping from entity -> LinkedListNode).
    //     They can still be looped over in instantiation order, but they can also be added/removed in O(1).
    // ========================================================================================================
    private static Dictionary<Timberborn.EntitySystem.EntityRegistry, Dictionary<Guid, LinkedListNode<GameObject>>> entityRegistryToEntityNodes =
        new Dictionary<Timberborn.EntitySystem.EntityRegistry, Dictionary<Guid, LinkedListNode<GameObject>>>();
    private static Dictionary<Guid, LinkedListNode<GameObject>> GetEntityNodesFromRegistry(Timberborn.EntitySystem.EntityRegistry registry)
    {
      if (!entityRegistryToEntityNodes.TryGetValue(registry, out var nodes))
      {
        nodes = entityRegistryToEntityNodes[registry] = new Dictionary<Guid, LinkedListNode<GameObject>>();
      }
      return nodes;
    }
    private static Dictionary<Timberborn.EntitySystem.EntityRegistry, LinkedList<GameObject>> entityRegistryToOrderedEntities = new Dictionary<Timberborn.EntitySystem.EntityRegistry, LinkedList<GameObject>>();
    private static LinkedList<GameObject> GetOrderedEntitiesFromRegistry(Timberborn.EntitySystem.EntityRegistry registry)
    {
      if (!entityRegistryToOrderedEntities.TryGetValue(registry, out var orderedEntities))
      {
        orderedEntities = entityRegistryToOrderedEntities[registry] = new LinkedList<GameObject>();
      }
      return orderedEntities;
    }
    public static bool EntityRegistryAddEntity(Timberborn.EntitySystem.EntityRegistry __instance, Timberborn.EntitySystem.EntityComponent entityComponent)
    {
      var nodes = GetEntityNodesFromRegistry(__instance);
      var entitiesInOrder = GetOrderedEntitiesFromRegistry(__instance);

      GameObject gameObject = entityComponent.gameObject;
      __instance._entities.Add(entityComponent.EntityId, gameObject);
      nodes[entityComponent.EntityId] = entitiesInOrder.AddLast(gameObject);
      return false;
    }
    public static bool EntityRegistryRemoveEntity(Timberborn.EntitySystem.EntityRegistry __instance, Timberborn.EntitySystem.EntityComponent entityComponent)
    {
      var nodes = GetEntityNodesFromRegistry(__instance);
      var entitiesInOrder = GetOrderedEntitiesFromRegistry(__instance);

      __instance._entities.Remove(entityComponent.EntityId);
      entitiesInOrder.Remove(nodes[entityComponent.EntityId]);
      nodes.Remove(entityComponent.EntityId);
      return false;
    }
    public static bool EntityRegistryget_Entities(Timberborn.EntitySystem.EntityRegistry __instance, ref IEnumerable<GameObject> __result)
    {
      __result = GetOrderedEntitiesFromRegistry(__instance);
      return false;
    }

    // ========================================================================================================
    // PlanterBuildingStatusUpdater.OnNavMeshUpdated optimization
    // ----
    //
    // Notes on the original implementation: 
    //     The original implementation causes all planter buildings to update their status any time the NavMesh
    //     changes at all, which is very expensive as it involves getting a list of all tiles within range of
    //     the building.
    //
    // Changes:
    //     The status is now only updated if the NavMesh change was close enough to the planter to possibly
    //     cause changes. Any changes further away than the `_navigationDistance.ResourceBuildings` limit
    //     can't possibly cause changes the list of nearby tiles, so they're ignored.
    // ========================================================================================================
    private static bool PlanterBuildingStatusUpdaterOnNavMeshUpdated(Timberborn.Planting.PlanterBuildingStatusUpdater __instance, Timberborn.Navigation.NavMeshUpdate navMeshUpdate)
    {
      if (!perfFixEnabled) { return true; }
      if (!planterBuildingFixEnabled) { return true; }
      var startPos = __instance.transform.position;
      var distLimit = ((Timberborn.Navigation.NavigationRangeService)__instance._navigationRangeService)._navigationDistance.ResourceBuildings;
      bool relevantChangeFound = false;
      foreach (var changeLoc in navMeshUpdate.TerrainCoordinates)
      {
        if (Vector3.Distance(changeLoc, startPos) < distLimit)
        {
          relevantChangeFound = true;
          break;
        }
      }

      if (relevantChangeFound)
      {
        __instance._shouldUpdateStatus = true;
        __instance._shouldUpdateRange = true;
      }
      return false;
    }

    // ========================================================================================================
    // GetComponent<> optimizations
    // ----
    //
    // Notes on the original implementation: 
    //     GetComponent<> is insanely slow to call, and it's called in a lot of places it shouldn't be. If the
    //     result is never going to change then it is a huge waste of time to call GetComponent. This alone is
    //     the biggest source of lag in all of Timberborn, and if fixed by the developers then this mod will be
    //     mostly useless. :)
    //
    // Changes:
    //     A new helper function is implemented that just caches the value from GetComponent. A bunch of these
    //     laggy functions were identified, copy/pasted, and had GetComponent replaced with GetComponentCached.
    // ========================================================================================================
    private static Dictionary<Type, Dictionary<Component, object>> cachedComponents = new Dictionary<Type, Dictionary<Component, object>>();
    public static T GetComponentCached<T>(Component c, bool refresh = false)
    {
      if (!cachedComponents.TryGetValue(typeof(T), out var objToCachedOfType))
      {
        objToCachedOfType = cachedComponents[typeof(T)] = new Dictionary<Component, object>();
      }
      if (refresh || !objToCachedOfType.TryGetValue(c, out var result))
      {
        result = objToCachedOfType[c] = c.GetComponent<T>();
      }
      return (T)result;
    }
    private static bool RangedEffectReceiverGetAffectingEffects(Timberborn.RangedEffectSystem.RangedEffectReceiver __instance, ref IReadOnlyList<Timberborn.RangedEffectSystem.RangedEffect> __result)
    {
      if (!perfFixEnabled) { return true; }
      if (!rangedEffectFixEnabled) { return true; }
      __result = __instance._enterer.IsInside ?
          GetComponentCached<Timberborn.RangedEffectSystem.RangedEffectsAffectingEnterable>(__instance._enterer.CurrentBuilding).Effects :
          __instance._rangedEffectService.GetEffectsAffectingCoordinates(Timberborn.Coordinates.CoordinateSystem.WorldToGridInt(__instance.transform.position).XY());
      return false;
    }

    private static bool DwellerHomeAssignerAssignDweller(Timberborn.DwellingSystem.AutoAssignableDwelling dwelling, IEnumerable<Timberborn.Beavers.Beaver> primaryBeavers, IEnumerable<Timberborn.Beavers.Beaver> secondaryBeavers, ref bool __result)
    {
      if (!perfFixEnabled) { return true; }
      if (!dwellerAssignFixEnabled) { return true; }

      foreach (var beaver in primaryBeavers.Concat(secondaryBeavers))
      {
        var dweller = GetComponentCached<Timberborn.DwellingSystem.Dweller>(beaver);
        if (dweller.IsLookingForBetterHome() && dwelling.CanAssignDweller(dweller))
        {
          dwelling.AssignDweller(dweller);
          __result = true;
          return false;
        }
      }
      __result = false;
      return false;
    }

    private static bool GatherWorkplaceBehaviorGetUnreservedYielders(Timberborn.Gathering.GatherWorkplaceBehavior __instance, ref IEnumerable<Timberborn.Yielding.Yielder> __result)
    {
      if (!perfFixEnabled) { return true; }
      if (!gatherGetUnreservedFixEnabled) { return true; }
      __result = __instance._yielderService.UnreservedYielders.Where((yielder => __instance._gathererFlag.CanGather(GetComponentCached<Timberborn.Gathering.Gatherable>(yielder))));
      return false;
    }

    private static bool HarvestStarterIsAllowed(Timberborn.Yielding.YieldRemovingBuilding yieldRemovingBuilding, Timberborn.Yielding.Yielder yielder, ref bool __result)
    {
      if (!perfFixEnabled) { return true; }
      if (!harvestIsAllowedFixEnabled) { return true; }
      __result = yieldRemovingBuilding.IsAllowed(GetComponentCached<Timberborn.Yielding.YielderSpecification>(yielder));
      return false;
    }

    private static bool PopulationDataCalculatorCollectWorkforceData(IReadOnlyList<Component> workers, ref Timberborn.Population.WorkforceData __result)
    {
      if (!perfFixEnabled) { return true; }
      if (!popFixEnabled) { return true; }
      int numberOfEmployable = 0;
      int numberOfUnemployable = 0;
      for (int index = 0; index < workers.Count; ++index)
      {
        if (GetComponentCached<Timberborn.WorkSystem.WorkRefuser>(workers[index]).RefusesWork)
        {
          ++numberOfUnemployable;
        }
        else
        {
          ++numberOfEmployable;
        }
      }
      __result = new Timberborn.Population.WorkforceData(numberOfEmployable, numberOfUnemployable);
      return false;
    }
    private static bool PopulationDataCalculatorCollectBedData(int numberOfAdults, int numberOfChildren, IEnumerable<Timberborn.DwellingSystem.Dwelling> dwellings, ref Timberborn.Population.BedData __result)
    {
      if (!perfFixEnabled) { return true; }
      if (!popFixEnabled) { return true; }
      int numberOfFullBeds = 0;
      int numberOfFreeBeds = 0;
      foreach (Timberborn.DwellingSystem.Dwelling dwelling in dwellings)
      {
        var component = GetComponentCached<Timberborn.Buildings.BlockableBuilding>(dwelling);
        if (component != null && component.IsUnblocked)
        {
          int numberOfDwellers = dwelling.NumberOfDwellers;
          numberOfFullBeds += numberOfDwellers;
          numberOfFreeBeds += dwelling.MaxBeavers - numberOfDwellers;
        }
      }
      int numberOfHomeless = numberOfAdults + numberOfChildren - numberOfFullBeds;
      __result = new Timberborn.Population.BedData(numberOfFullBeds, numberOfFreeBeds, numberOfHomeless);
      return false;
    }
    private static bool PopulationDataCollectEmploymentMetrics(int numberOfAdults, int numberOfBots, IEnumerable<Timberborn.WorkSystem.Workplace> workplaces, Timberborn.WorkerTypesUI.WorkerTypeHelper ____workerTypeHelper, ref (Timberborn.Population.WorkplaceData beaverWorkplaceData, Timberborn.Population.WorkplaceData golemWorkplaceData) __result)
    {
      if (!perfFixEnabled) { return true; }
      if (!popFixEnabled) { return true; }
      int numberOfFullWorkslots1 = 0;
      int numberOfFreeWorkslots1 = 0;
      int numberOfFullWorkslots2 = 0;
      int numberOfFreeWorkslots2 = 0;
      foreach (Timberborn.WorkSystem.Workplace workplace in workplaces)
      {
        var component = GetComponentCached<Timberborn.Buildings.BlockableBuilding>(workplace);
        if (component != null && component.IsUnblocked)
        {
          int ofAssignedWorkers = workplace.NumberOfAssignedWorkers;
          int num = Mathf.Max(workplace.DesiredWorkers - ofAssignedWorkers, 0);
          var workerType = GetComponentCached<Timberborn.WorkSystem.WorkplaceWorkerType>(workplace);
          if (____workerTypeHelper.IsBeaverWorkerType(workerType.WorkerType))
          {
            numberOfFullWorkslots1 += ofAssignedWorkers;
            numberOfFreeWorkslots1 += num;
          }
          else
          {
            numberOfFullWorkslots2 += ofAssignedWorkers;
            numberOfFreeWorkslots2 += num;
          }
        }
      }
      int numberOfUnemployed1 = numberOfAdults - numberOfFullWorkslots1;
      int numberOfUnemployed2 = numberOfBots - numberOfFullWorkslots2;
      __result = (new Timberborn.Population.WorkplaceData(numberOfFullWorkslots1, numberOfFreeWorkslots1, numberOfUnemployed1), new Timberborn.Population.WorkplaceData(numberOfFullWorkslots2, numberOfFreeWorkslots2, numberOfUnemployed2));
      return false;
    }

    // ========================================================================================================
    // PlantBehavior.ReserveCoordinates optimization
    // ----
    //
    // Notes on the original implementation: 
    //     Every frame that a spot isn't already reserved, PlantingCoordinatesFinder.FindClosestAllowed is
    //     called. This means when farmers are idle at a farmhouse with no empty spots to plant in, this is
    //     firing EVERY frame for each idling farmer. This is incredibly expensive because
    //     PlantingCoordinatesFinder.GetReachable loops over every plantable spot on the whole map to find
    //     the nearest spot. Even with cached paths in the NavigationService, this scales at
    //     O(NumFarmers * NumTilesOnTheMap) which is nutty slow.
    //
    // Changes:
    //     The whole approach is changed here. Now all tiles are within range of each farmhouse are retrieved
    //     and cached, and only updated every ~100 frames. During caching the tiles are sorted by distance, so
    //     retreival of the closest tiles can be found quickly.
    //     Notably, this chooses plant spots based on their distance to the farmhouse, not distance to the
    //     accessible door. This is weird, but it's how the vanilla function works and it's a lot faster to
    //     calculate, so it's been preserved.
    // ========================================================================================================
    private static bool CanPlantAtIgnoreTerrain(Timberborn.Planting.PlantingCoordinatesFinder finder, Vector3Int coordinates, string plantingSpotPrefabName, Timberborn.Planting.Plantable prioritizedPlantable)
    {
      return finder._soilMoistureService.SoilIsMoist(coordinates.XY()) && finder._waterNaturalResourceService.ConditionsAreMet(plantingSpotPrefabName, coordinates) && (prioritizedPlantable ? (plantingSpotPrefabName == prioritizedPlantable.PrefabName ? 1 : 0) : (finder._planterBuilding.CanPlant(plantingSpotPrefabName) ? 1 : 0)) != 0;
    }
    private static Dictionary<Timberborn.Planting.PlantBehavior, List<Vector3Int>> behaviorToSortedSpots = new Dictionary<Timberborn.Planting.PlantBehavior, List<Vector3Int>>();
    private static Vector3Int? GetClosestSpotForPlantBehavior(Timberborn.Planting.PlantBehavior behavior, Timberborn.Planting.Plantable prioritizedPlantable)
    {
      var plantCoordsFinder = behavior._worker.Workplace.GetComponent<Timberborn.Planting.PlantingCoordinatesFinder>();

      // Update the sorted list of nearby plantable spots.
      if (!behaviorToSortedSpots.TryGetValue(behavior, out var sortedSpots)
        || UnityEngine.Random.Range(0f, 1f) < .01f) // Invalidate the cache every ~100 frames
      {
        sortedSpots = behaviorToSortedSpots[behavior] = new List<Vector3Int>();
        foreach (var spot in behavior._plantingService._plantingSpots.Keys.Concat(behavior._plantingService._reservedCoordinates))
        {
          if (plantCoordsFinder._accessible.IsReachableByTerrain(Timberborn.Coordinates.CoordinateSystem.GridToWorldCentered(spot)))
          {
            sortedSpots.Add(spot);
          }
        }
        var blockCenter = plantCoordsFinder._blockObjectCenter.WorldCenterGrounded;
        sortedSpots.Sort((a, b) =>
        {
          var distA = Vector3.Distance(blockCenter, Timberborn.Coordinates.CoordinateSystem.GridToWorldCentered(a));
          var distB = Vector3.Distance(blockCenter, Timberborn.Coordinates.CoordinateSystem.GridToWorldCentered(b));
          return distA > distB ? 1 : distA < distB ? -1 : 0;
        });
      }

      // Find the nearest plantable spot that meets all required criteria.
      Vector3Int? result = null;
      foreach (var spot in sortedSpots)
      {
        if (plantCoordsFinder._plantingService._reservedCoordinates.Contains(spot)) { continue; }
        string resourceAt = plantCoordsFinder._plantingService.GetResourceAt(spot.XY());
        if (resourceAt == null) { continue; }
        if (!CanPlantAtIgnoreTerrain(plantCoordsFinder, spot, resourceAt, prioritizedPlantable)) { continue; }
        if (!plantCoordsFinder._spawnValidator.IsUnobstructed(spot, resourceAt)) { continue; }
        result = spot;
        break;
      }
      return result;
    }
    private static bool PlantBehaviorReserveCoordinates(Timberborn.Planting.PlantBehavior __instance, GameObject agent, bool prioritized)
    {
      if (!perfFixEnabled) { return true; }
      if (!planReserveFixEnabled) { return true; }
      if (__instance._planter.PlantingCoordinates.HasValue) { return false; }

      Vector3 position = agent.transform.position;
      Vector3Int? result = null;
      if (prioritized)
      {
        var prioritizer = __instance.GetComponent<Timberborn.Planting.PlantablePrioritizer>();
        if (prioritizer)
        {
          var plantCoordsFinder = __instance._worker.Workplace.GetComponent<Timberborn.Planting.PlantingCoordinatesFinder>();
          result = plantCoordsFinder.GetClosestOrDefault(plantCoordsFinder.GetNeighboring(agent.transform.position, prioritizer.PrioritizedPlantable));
          if (!result.HasValue)
          {
            result = GetClosestSpotForPlantBehavior(__instance, prioritizer.PrioritizedPlantable);
          }
        }
      }
      else
      {
        result = GetClosestSpotForPlantBehavior(__instance, null);
      }
      if (!result.HasValue)
      {
        return false;
      }
      __instance._planter.Reserve(result.Value);
      return false;
    }

    // ========================================================================================================
    // TickableEntity.Tick optimizations
    // ----
    //
    // Notes on the original implementation: 
    //     Similar to the GetComponent issues above, calling the getter for Component.gameObject is expensive.
    //
    // Changes:
    //     Components are never moved between GameObjects, so the value is cached in a Dictionary. Everything
    //     else is copy/pasted from the original implementation.
    // ========================================================================================================
    private static Dictionary<Timberborn.EntitySystem.EntityComponent, GameObject> entityComponentToGameObject = new Dictionary<Timberborn.EntitySystem.EntityComponent, GameObject>();
    public static bool TickableEntityTick(Timberborn.TickSystem.TickableEntity __instance)
    {
      if (!perfFixEnabled) { return true; }
      if (!tickGameobjectFixEnabled) { return true; }
      try
      {
        if (!entityComponentToGameObject.TryGetValue(__instance._entityComponent, out var gameObject))
        {
          gameObject = entityComponentToGameObject[__instance._entityComponent] = __instance._entityComponent.gameObject;
        }
        if (!gameObject.activeInHierarchy)
        {
          return false;
        }
        __instance.TickTickableComponents();
      }
      catch (Exception ex)
      {
        string str = string.Format("Exception thrown while ticking entity {0}", (object)__instance.EntityId);
        throw new Exception(!(bool)(UnityEngine.Object)__instance._entityComponent ? str + " '" + __instance._originalName + "' (destroyed)" : str + " '" + __instance._entityComponent.name + "'", ex);
      }
      return false;
    }
  }
}
