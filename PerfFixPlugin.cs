﻿using System;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Timberborn.Common;
using System.Reflection;
using Timberborn.MapSystemUI;
using TimberApi.ConsoleSystem;
using HarmonyLib;
using TimberApi.ModSystem;
using Timberborn.Planting;

namespace Frog
{
    public class PerfFixPlugin : IModEntrypoint
    {
        public static PerfFixPlugin instance;
        public Harmony harmony;
        public static bool perfFixEnabled = true; // Left in for easy debugging.
        public static bool planReserveFixEnabled = true;
        public static bool popFixEnabled = true;
        public static bool harvestIsAllowedFixEnabled = true;
        public static bool gatherGetUnreservedFixEnabled = true;
        public static bool dwellerAssignFixEnabled = true;
        public static bool rangedEffectFixEnabled = true;
        public static bool planterBuildingFixEnabled = true;
        public static bool tickGameobjectFixEnabled = true;
        public static IConsoleWriter Logger;
        public void Entry(IMod mod, IConsoleWriter consoleWriter)
        {
            instance = this;
            Logger = consoleWriter;
            Patcher.DoPatching();

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
        //     can't possibly cause changes the list of nearby tiles, so they're ignored.  It is also updated on 
        //     a random 60 frame timer to ensure it never waits too long.
        // ========================================================================================================
        public static bool PlanterBuildingStatusUpdaterOnNavMeshUpdated(Timberborn.Planting.PlanterBuildingStatusUpdater __instance, Timberborn.Navigation.NavMeshUpdate navMeshUpdate)
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

            if ((relevantChangeFound) || (UnityEngine.Random.Range(0, 60) == 1))
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
        public static bool RangedEffectReceiverGetAffectingEffects(Timberborn.RangedEffectSystem.RangedEffectReceiver __instance, ref IReadOnlyList<Timberborn.RangedEffectSystem.RangedEffect> __result)
        {
            if (!perfFixEnabled) { return true; }
            if (!rangedEffectFixEnabled) { return true; }
            __result = __instance._enterer.IsInside ?
                GetComponentCached<Timberborn.RangedEffectSystem.RangedEffectsAffectingEnterable>(__instance._enterer.CurrentBuilding).Effects :
                __instance._rangedEffectService.GetEffectsAffectingCoordinates(Timberborn.Coordinates.CoordinateSystem.WorldToGridInt(__instance.transform.position).XY());
            return false;
        }

        public static bool DwellerHomeAssignerAssignDweller(Timberborn.DwellingSystem.AutoAssignableDwelling dwelling, IEnumerable<Timberborn.Beavers.Beaver> primaryBeavers, IEnumerable<Timberborn.Beavers.Beaver> secondaryBeavers, ref bool __result)
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

        public static bool GatherWorkplaceBehaviorGetUnreservedYielders(Timberborn.Gathering.GatherWorkplaceBehavior __instance, ref IEnumerable<Timberborn.Yielding.Yielder> __result)
        {
            if (!perfFixEnabled) { return true; }
            if (!gatherGetUnreservedFixEnabled) { return true; }
            __result = __instance._yielderService.UnreservedYielders.Where((yielder => __instance._gathererFlag.CanGather(GetComponentCached<Timberborn.Gathering.Gatherable>(yielder))));
            return false;
        }

        public static bool HarvestStarterIsAllowed(Timberborn.Yielding.YieldRemovingBuilding yieldRemovingBuilding, Timberborn.Yielding.Yielder yielder, ref bool __result)
        {
            if (!perfFixEnabled) { return true; }
            if (!harvestIsAllowedFixEnabled) { return true; }
            __result = yieldRemovingBuilding.IsAllowed(GetComponentCached<Timberborn.Yielding.YielderSpecification>(yielder));
            return false;
        }

        public static bool PopulationDataCalculatorCollectWorkforceData(IReadOnlyList<Component> workers, ref Timberborn.Population.WorkforceData __result)
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
        public static bool PopulationDataCalculatorCollectBedData(int numberOfAdults, int numberOfChildren, IEnumerable<Timberborn.DwellingSystem.Dwelling> dwellings, ref Timberborn.Population.BedData __result)
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
        public static bool PopulationDataCollectEmploymentMetrics(int numberOfAdults, int numberOfBots, IEnumerable<Timberborn.WorkSystem.Workplace> workplaces, Timberborn.WorkerTypesUI.WorkerTypeHelper ____workerTypeHelper, ref (Timberborn.Population.WorkplaceData beaverWorkplaceData, Timberborn.Population.WorkplaceData botWorkplaceData) __result)
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
    public class Patcher
    {
        public static void DoPatching()
        {
            PerfFixPlugin.instance.harmony = new Harmony("com.frog.perfmod");

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.Population.PopulationDataCalculator), "CollectWorkforceData");
                var mPrefix = SymbolExtensions.GetMethodInfo((IReadOnlyList<Component> workers, Timberborn.Population.WorkforceData __result) =>
                    PerfFixPlugin.PopulationDataCalculatorCollectWorkforceData(workers, ref __result));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.Population.PopulationDataCalculator), "CollectBedData");
                var mPrefix = SymbolExtensions.GetMethodInfo((int numberOfAdults, int numberOfChildren, IEnumerable<Timberborn.DwellingSystem.Dwelling> dwellings, Timberborn.Population.BedData __result) =>
                    PerfFixPlugin.PopulationDataCalculatorCollectBedData(numberOfAdults, numberOfChildren, dwellings, ref __result));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.Population.PopulationDataCalculator), "CollectEmploymentMetrics");
                var mPrefix = SymbolExtensions.GetMethodInfo((int numberOfAdults, int numberOfBots, IEnumerable<Timberborn.WorkSystem.Workplace> workplaces, Timberborn.WorkerTypesUI.WorkerTypeHelper ____workerTypeHelper, (Timberborn.Population.WorkplaceData beaverWorkplaceData, Timberborn.Population.WorkplaceData botWorkplaceData) __result) =>
                    PerfFixPlugin.PopulationDataCollectEmploymentMetrics(numberOfAdults, numberOfBots, workplaces, ____workerTypeHelper, ref __result));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.Planting.PlanterBuildingStatusUpdater), "OnNavMeshUpdated");
                var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.Planting.PlanterBuildingStatusUpdater __instance, Timberborn.Navigation.NavMeshUpdate navMeshUpdate) =>
                    PerfFixPlugin.PlanterBuildingStatusUpdaterOnNavMeshUpdated(__instance, navMeshUpdate));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.Fields.HarvestStarter), "IsAllowed");
                var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.Yielding.YieldRemovingBuilding yieldRemovingBuilding, Timberborn.Yielding.Yielder yielder, bool __result) =>
                    PerfFixPlugin.HarvestStarterIsAllowed(yieldRemovingBuilding, yielder, ref __result));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.Gathering.GatherWorkplaceBehavior), "get_UnreservedYielders");
                var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.Gathering.GatherWorkplaceBehavior __instance, IEnumerable<Timberborn.Yielding.Yielder> __result) =>
                    PerfFixPlugin.GatherWorkplaceBehaviorGetUnreservedYielders(__instance, ref __result));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.DwellingSystem.DwellerHomeAssigner), "AssignDweller");
                var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.DwellingSystem.AutoAssignableDwelling dwelling, IEnumerable<Timberborn.Beavers.Beaver> primaryBeavers, IEnumerable<Timberborn.Beavers.Beaver> secondaryBeavers, bool __result) =>
                    PerfFixPlugin.DwellerHomeAssignerAssignDweller(dwelling, primaryBeavers, secondaryBeavers, ref __result));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.RangedEffectSystem.RangedEffectReceiver), "GetAffectingEffects");
                var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.RangedEffectSystem.RangedEffectReceiver __instance, IReadOnlyList<Timberborn.RangedEffectSystem.RangedEffect> __result) =>
                    PerfFixPlugin.RangedEffectReceiverGetAffectingEffects(__instance, ref __result));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            {
                var mOriginal = AccessTools.Method(typeof(Timberborn.TickSystem.TickableEntity), "Tick");
                var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.TickSystem.TickableEntity __instance) => PerfFixPlugin.TickableEntityTick(__instance));
                PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            }

            { // EntityRegistration stuff needs to be enabled/disabled all at once, and can't be toggled at runtime.
                {
                    var mOriginal = AccessTools.Method(typeof(Timberborn.EntitySystem.EntityRegistry), "AddEntity");
                    var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.EntitySystem.EntityRegistry __instance, Timberborn.EntitySystem.EntityComponent entityComponent) => PerfFixPlugin.EntityRegistryAddEntity(__instance, entityComponent));
                    PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
                }
                {
                    var mOriginal = AccessTools.Method(typeof(Timberborn.EntitySystem.EntityRegistry), "RemoveEntity");
                    var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.EntitySystem.EntityRegistry __instance, Timberborn.EntitySystem.EntityComponent entityComponent) => PerfFixPlugin.EntityRegistryRemoveEntity(__instance, entityComponent));
                    PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
                }
                {
                    var mOriginal = AccessTools.Method(typeof(Timberborn.EntitySystem.EntityRegistry), "get_Entities");
                    var mPrefix = SymbolExtensions.GetMethodInfo((Timberborn.EntitySystem.EntityRegistry __instance, IEnumerable<GameObject> __result) => PerfFixPlugin.EntityRegistryget_Entities(__instance, ref __result));
                    PerfFixPlugin.instance.harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
                }
            }
        }
    }
}