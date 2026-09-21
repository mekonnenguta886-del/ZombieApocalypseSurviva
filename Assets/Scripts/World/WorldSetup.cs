using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Missions;
using ZombieApocalypse.Systems;
using ZombieApocalypse.UI;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Auto-constructs Phase 6 survival-exploration world hierarchy in TestArena:
    /// World -> SafeHouse, AbandonedHouse, MedicalBuilding, SupplyWarehouse, ZombieZone, ExtractionZone.
    /// Builds interactable doors, locked key doors, loot containers, location triggers, safe zone,
    /// mission manager, UI HUD, objective checklist, and objective markers.
    /// 
    /// ATTACH TO: [WorldSetup] GameObject in TestArena scene.
    /// </summary>
    public class WorldSetup : MonoBehaviour
    {
        [Header("Item & Data References (Auto-loaded if empty)")]
        [SerializeField] private ItemData medkitItem;
        [SerializeField] private ItemData bandageItem;
        [SerializeField] private ItemData pistolAmmoItem;
        [SerializeField] private ItemData rifleAmmoItem;
        [SerializeField] private ItemData shotgunAmmoItem;
        [SerializeField] private ItemData foodItem;
        [SerializeField] private ItemData waterItem;
        [SerializeField] private KeyData medicalKeyItem;

        private void Awake()
        {
            LoadDefaultItemData();
            ConstructWorldEnvironment();
            ConstructMissionAndUI();
            EnsurePhase8Systems();
        }

        private void EnsurePhase8Systems()
        {
            if (DifficultyManager.Instance == null && FindObjectOfType<DifficultyManager>() == null)
            {
                GameObject diffObj = new GameObject("[DifficultyManager]");
                diffObj.AddComponent<DifficultyManager>();
            }

            if (ZombieApocalypse.WorldEvents.WorldEventManager.Instance == null && FindObjectOfType<ZombieApocalypse.WorldEvents.WorldEventManager>() == null)
            {
                GameObject wemObj = new GameObject("[WorldEventManager]");
                wemObj.AddComponent<ZombieApocalypse.WorldEvents.WorldEventManager>();
            }

            if (ZombieApocalypse.Progression.PlayerProgressionSystem.Instance == null && FindObjectOfType<ZombieApocalypse.Progression.PlayerProgressionSystem>() == null)
            {
                GameObject progObj = new GameObject("[PlayerProgressionSystem]");
                progObj.AddComponent<ZombieApocalypse.Progression.PlayerProgressionSystem>();
            }
        }

        private void LoadDefaultItemData()
        {
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item == null) continue;
                string id = item.itemId != null ? item.itemId.ToLower() : "";
                string name = item.itemName != null ? item.itemName.ToLower() : "";

                if (id.Contains("medkit") || name.Contains("medkit")) medkitItem = item;
                else if (id.Contains("bandage") || name.Contains("bandage")) bandageItem = item;
                else if (id.Contains("pistol") || name.Contains("pistol")) pistolAmmoItem = item;
                else if (id.Contains("rifle") || name.Contains("rifle")) rifleAmmoItem = item;
                else if (id.Contains("shotgun") || name.Contains("shotgun")) shotgunAmmoItem = item;
                else if (id.Contains("food") || name.Contains("food")) foodItem = item;
                else if (id.Contains("water") || name.Contains("water")) waterItem = item;
            }

            if (medicalKeyItem == null)
            {
                medicalKeyItem = ScriptableObject.CreateInstance<KeyData>();
                medicalKeyItem.itemId = "key_medical";
                medicalKeyItem.itemName = "Medical Key";
                medicalKeyItem.description = "Key to unlock the Medical Building doors.";
                medicalKeyItem.category = ItemCategory.Miscellaneous;
                medicalKeyItem.isStackable = false;
                medicalKeyItem.maxStackSize = 1;
                medicalKeyItem.isConsumable = false;
                medicalKeyItem.keyId = "key_medical";
            }
        }

        private void ConstructWorldEnvironment()
        {
            GameObject worldRoot = GameObject.Find("World");
            if (worldRoot == null)
            {
                worldRoot = new GameObject("World");
            }

            // 1. Safe House (Position: 0, 0, 0)
            CreateLocationBuilding(worldRoot.transform, "SafeHouse", new Vector3(0f, 0f, 0f), new Vector3(12f, 4f, 12f), "SAFE HOUSE", "loc_safehouse", new Color(0.2f, 0.7f, 0.3f), out GameObject safeHouseObj);
            if (safeHouseObj != null && safeHouseObj.GetComponent<SafeZoneTrigger>() == null)
            {
                safeHouseObj.AddComponent<SafeZoneTrigger>();
            }
            CreateDoor(safeHouseObj.transform, "Safe House Door", new Vector3(0f, 1.5f, 6f), false, null);
            CreateWorkbench(safeHouseObj.transform, "Weapon Workbench", new Vector3(3f, 0.8f, 3f));

            // 2. Abandoned House (Position: 25, 0, 0)
            CreateLocationBuilding(worldRoot.transform, "AbandonedHouse", new Vector3(25f, 0f, 0f), new Vector3(10f, 4f, 10f), "ABANDONED HOUSE", "loc_abandoned_house", new Color(0.7f, 0.5f, 0.2f), out GameObject houseObj);
            CreateDoor(houseObj.transform, "Abandoned House Door", new Vector3(25f, 1.5f, 5f), false, null);
            CreateLootBox(houseObj.transform, "Kitchen Cabinet", new Vector3(23f, 0.8f, 2f), foodItem, waterItem);
            CreateKeyPickup(houseObj.transform, "Desk Key", new Vector3(27f, 0.8f, -2f), medicalKeyItem);

            // 3. Medical Building (Position: 50, 0, 0)
            CreateLocationBuilding(worldRoot.transform, "MedicalBuilding", new Vector3(50f, 0f, 0f), new Vector3(14f, 5f, 14f), "MEDICAL BUILDING", "loc_med_building", new Color(0.2f, 0.5f, 0.9f), out GameObject medObj);
            CreateDoor(medObj.transform, "Medical Door", new Vector3(50f, 1.5f, 7f), true, medicalKeyItem);
            CreateLootBox(medObj.transform, "Medical Supply Chest", new Vector3(53f, 0.8f, -3f), medkitItem, bandageItem);

            // 4. Supply Warehouse (Position: 0, 0, 35)
            CreateLocationBuilding(worldRoot.transform, "SupplyWarehouse", new Vector3(0f, 0f, 35f), new Vector3(16f, 6f, 14f), "SUPPLY WAREHOUSE", "loc_warehouse", new Color(0.6f, 0.6f, 0.3f), out GameObject wareObj);
            CreateDoor(wareObj.transform, "Warehouse Door", new Vector3(0f, 1.5f, 28f), false, null);
            CreateLootBox(wareObj.transform, "Military Ammo Crate", new Vector3(-3f, 0.8f, 38f), pistolAmmoItem, rifleAmmoItem, shotgunAmmoItem);

            // 5. Zombie Zone (Position: 40, 0, 35)
            CreateLocationZone(worldRoot.transform, "ZombieZone", new Vector3(40f, 0f, 35f), new Vector3(20f, 2f, 20f), "ZOMBIE INFESTED ZONE", "loc_zombie_zone", new Color(0.9f, 0.2f, 0.2f), out GameObject zombieZoneObj);
            RepositionZombieSpawner(zombieZoneObj.transform.position);

            if (zombieZoneObj != null && zombieZoneObj.GetComponent<ZombieApocalypse.WorldEvents.WorldEventTrigger>() == null)
            {
                zombieZoneObj.AddComponent<ZombieApocalypse.WorldEvents.WorldEventTrigger>();
            }

            // 6. Extraction Zone (Position: -30, 0, 35)
            CreateLocationZone(worldRoot.transform, "ExtractionZone", new Vector3(-30f, 0f, 35f), new Vector3(12f, 2f, 12f), "EXTRACTION POINT", "loc_extraction", new Color(0.3f, 0.9f, 0.8f), out GameObject extractObj);
        }

        private void CreateLocationBuilding(Transform parent, string name, Vector3 center, Vector3 size, string label, string locationId, Color color, out GameObject buildingRoot)
        {
            buildingRoot = new GameObject(name);
            buildingRoot.transform.SetParent(parent);
            buildingRoot.transform.position = center;

            // Location Trigger Collider
            BoxCollider col = buildingRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size + Vector3.one * 2f;
            col.center = Vector3.up * (size.y * 0.5f);

            LocationTrigger locTrigger = buildingRoot.AddComponent<LocationTrigger>();
            var idField = typeof(LocationTrigger).GetField("locationId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nameField = typeof(LocationTrigger).GetField("locationName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null) idField.SetValue(locTrigger, locationId);
            if (nameField != null) nameField.SetValue(locTrigger, label);

            // Visual Marker / Floor Frame
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = $"{name}_Floor";
            floor.transform.SetParent(buildingRoot.transform);
            floor.transform.position = center + Vector3.up * 0.05f;
            floor.transform.localScale = new Vector3(size.x, 0.1f, size.z);
            SetMaterialColor(floor, color * 0.4f);

            // Roof visual
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = $"{name}_Roof";
            roof.transform.SetParent(buildingRoot.transform);
            roof.transform.position = center + Vector3.up * size.y;
            roof.transform.localScale = new Vector3(size.x, 0.3f, size.z);
            SetMaterialColor(roof, color * 0.6f);

            // Walls
            CreateWall(buildingRoot.transform, center + new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.3f, size.y, size.z), color);
            CreateWall(buildingRoot.transform, center + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.3f, size.y, size.z), color);
            CreateWall(buildingRoot.transform, center + new Vector3(0f, size.y * 0.5f, -size.z * 0.5f), new Vector3(size.x, size.y, 0.3f), color);

            // 3D Label Text
            CreateWorldLabel(buildingRoot.transform, center + Vector3.up * (size.y + 1.5f), label, color);
        }

        private void CreateLocationZone(Transform parent, string name, Vector3 center, Vector3 size, string label, string locationId, Color color, out GameObject zoneRoot)
        {
            zoneRoot = new GameObject(name);
            zoneRoot.transform.SetParent(parent);
            zoneRoot.transform.position = center;

            BoxCollider col = zoneRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            col.center = Vector3.up * (size.y * 0.5f);

            LocationTrigger locTrigger = zoneRoot.AddComponent<LocationTrigger>();
            var idField = typeof(LocationTrigger).GetField("locationId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nameField = typeof(LocationTrigger).GetField("locationName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null) idField.SetValue(locTrigger, locationId);
            if (nameField != null) nameField.SetValue(locTrigger, label);

            GameObject zonePad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zonePad.name = $"{name}_Pad";
            zonePad.transform.SetParent(zoneRoot.transform);
            zonePad.transform.position = center + Vector3.up * 0.02f;
            zonePad.transform.localScale = new Vector3(size.x, 0.05f, size.z);
            SetMaterialColor(zonePad, color * 0.5f);

            CreateWorldLabel(zoneRoot.transform, center + Vector3.up * 3f, label, color);
        }

        private void CreateWall(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            SetMaterialColor(wall, color * 0.7f);
        }

        private void CreateDoor(Transform parent, string doorName, Vector3 pos, bool isLocked, ItemData requiredKey)
        {
            GameObject doorRoot = new GameObject(doorName);
            doorRoot.transform.SetParent(parent);
            doorRoot.transform.position = pos;

            GameObject doorMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorMesh.name = "DoorFrame";
            doorMesh.transform.SetParent(doorRoot.transform);
            doorMesh.transform.position = pos + new Vector3(1f, 0f, 0f);
            doorMesh.transform.localScale = new Vector3(2f, 3f, 0.2f);
            SetMaterialColor(doorMesh, isLocked ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.6f, 0.4f, 0.2f));

            DoorController doorCtrl = doorRoot.AddComponent<DoorController>();
            var nameF = typeof(DoorController).GetField("doorName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lockF = typeof(DoorController).GetField("isLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var keyF = typeof(DoorController).GetField("requiredKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hingeF = typeof(DoorController).GetField("doorHinge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (nameF != null) nameF.SetValue(doorCtrl, doorName);
            if (lockF != null) lockF.SetValue(doorCtrl, isLocked);
            if (keyF != null) keyF.SetValue(doorCtrl, requiredKey);
            if (hingeF != null) hingeF.SetValue(doorCtrl, doorRoot.transform);
        }

        private void CreateLootBox(Transform parent, string boxName, Vector3 pos, params ItemData[] items)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = boxName;
            crate.transform.SetParent(parent);
            crate.transform.position = pos;
            crate.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            SetMaterialColor(crate, new Color(0.8f, 0.6f, 0.2f));

            LootContainer container = crate.AddComponent<LootContainer>();
            var nameF = typeof(LootContainer).GetField("containerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tableF = typeof(LootContainer).GetField("lootTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (nameF != null) nameF.SetValue(container, boxName);

            if (tableF != null && items != null)
            {
                List<LootEntry> entries = new List<LootEntry>();
                foreach (var item in items)
                {
                    if (item == null) continue;
                    LootEntry entry = new LootEntry
                    {
                        itemData = item,
                        minQuantity = 1,
                        maxQuantity = 2,
                        dropChance = 1.0f
                    };
                    entries.Add(entry);
                }
                tableF.SetValue(container, entries);
            }
        }

        private void CreateKeyPickup(Transform parent, string keyName, Vector3 pos, KeyData keyData)
        {
            GameObject keyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            keyObj.name = keyName;
            keyObj.transform.SetParent(parent);
            keyObj.transform.position = pos;
            keyObj.transform.localScale = Vector3.one * 0.4f;
            SetMaterialColor(keyObj, new Color(1.0f, 0.85f, 0.1f));

            ItemPickup pickup = keyObj.AddComponent<ItemPickup>();
            pickup.Setup(keyData, 1);
        }

        private void CreateWorkbench(Transform parent, string benchName, Vector3 pos)
        {
            GameObject benchObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benchObj.name = benchName;
            benchObj.transform.SetParent(parent);
            benchObj.transform.position = pos;
            benchObj.transform.localScale = new Vector3(2.0f, 1.0f, 1.0f);
            SetMaterialColor(benchObj, new Color(0.3f, 0.4f, 0.6f));

            Workbench workbench = benchObj.AddComponent<Workbench>();
            var nameF = typeof(Workbench).GetField("workbenchName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nameF != null) nameF.SetValue(workbench, benchName);
        }

        private void CreateWorldLabel(Transform parent, Vector3 pos, string text, Color color)
        {
            GameObject labelObj = new GameObject("WorldLabel");
            labelObj.transform.SetParent(parent);
            labelObj.transform.position = pos;

            TextMeshPro tm = labelObj.AddComponent<TextMeshPro>();
            tm.text = text;
            tm.fontSize = 7;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = color;
        }

        private void SetMaterialColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = color;
            }
        }

        private void RepositionZombieSpawner(Vector3 zombieZonePos)
        {
            ZombieSpawner spawner = FindObjectOfType<ZombieSpawner>();
            if (spawner != null)
            {
                spawner.transform.position = zombieZonePos;
            }
        }

        private void ConstructMissionAndUI()
        {
            // 0. Save Manager Setup
            GameObject saveMgrObj = GameObject.Find("[SaveManager]");
            if (saveMgrObj == null)
            {
                saveMgrObj = new GameObject("[SaveManager]");
                saveMgrObj.AddComponent<ZombieApocalypse.Save.SaveManager>();
            }

            // 1. Mission Manager Setup
            GameObject missionMgrObj = GameObject.Find("[MissionManager]");
            if (missionMgrObj == null)
            {
                missionMgrObj = new GameObject("[MissionManager]");
            }

            MissionManager mgr = missionMgrObj.GetComponent<MissionManager>();
            if (mgr == null) mgr = missionMgrObj.AddComponent<MissionManager>();

            // Create Sample Mission Data: "Medical Supply Run"
            MissionData medicalMission = ScriptableObject.CreateInstance<MissionData>();
            medicalMission.missionId = "mission_med_run";
            medicalMission.missionTitle = "Medical Supply Run";
            medicalMission.missionDescription = "Travel to the Medical Building, collect 1 Medkit, and return safely to the Safe House.";

            // Objective 1: Reach Medical Building
            Objective obj1 = new Objective
            {
                objectiveId = "obj_reach_med",
                description = "Reach Medical Building",
                type = ObjectiveType.ReachLocation,
                targetLocationId = "loc_med_building"
            };

            // Objective 2: Collect 1 Medkit
            Objective obj2 = new Objective
            {
                objectiveId = "obj_find_medkit",
                description = "Collect 1 Medkit",
                type = ObjectiveType.CollectItem,
                targetItem = medkitItem,
                requiredAmount = 1
            };

            // Objective 3: Return to Safe House
            Objective obj3 = new Objective
            {
                objectiveId = "obj_return_safehouse",
                description = "Return to Safe House",
                type = ObjectiveType.ReachLocation,
                targetLocationId = "loc_safehouse"
            };

            medicalMission.objectives = new List<Objective> { obj1, obj2, obj3 };

            // Mission Rewards
            if (bandageItem != null)
            {
                medicalMission.rewardItems = new List<InventorySlot> { new InventorySlot(bandageItem, 1) };
            }
            medicalMission.rewardAmmoType = AmmoType.Pistol;
            medicalMission.rewardAmmoAmount = 30;

            var availField = typeof(MissionManager).GetField("availableMissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (availField != null)
            {
                availField.SetValue(mgr, new List<MissionData> { medicalMission });
            }

            mgr.StartMission(0);

            // 2. UI Setup (HUD Canvas, MissionUI, ObjectiveMarker, WorkbenchUI)
            GameObject canvasObj = GameObject.Find("GameplayCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GameplayCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                HUDController hud = canvasObj.AddComponent<HUDController>();
            }

            if (canvasObj.GetComponent<WorkbenchUIController>() == null && FindObjectOfType<WorkbenchUIController>() == null)
            {
                canvasObj.AddComponent<WorkbenchUIController>();
            }

            if (canvasObj.GetComponent<ProgressionUIController>() == null && FindObjectOfType<ProgressionUIController>() == null)
            {
                canvasObj.AddComponent<ProgressionUIController>();
            }

            GameObject missionPanel = GameObject.Find("MissionPanel");
            if (missionPanel == null)
            {
                missionPanel = new GameObject("MissionPanel");
                missionPanel.transform.SetParent(canvasObj.transform, false);

                RectTransform rect = missionPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.02f, 0.70f);
                rect.anchorMax = new Vector2(0.30f, 0.96f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                GameObject titleObj = new GameObject("MissionTitle");
                titleObj.transform.SetParent(missionPanel.transform, false);
                TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.fontSize = 18;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = new Color(1f, 0.85f, 0.2f);
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 0.75f);
                titleRect.anchorMax = new Vector2(1f, 1f);

                GameObject descObj = new GameObject("MissionDesc");
                descObj.transform.SetParent(missionPanel.transform, false);
                TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
                descText.fontSize = 13;
                descText.color = Color.white;
                RectTransform descRect = descObj.GetComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0f, 0.45f);
                descRect.anchorMax = new Vector2(1f, 0.75f);

                GameObject objListObj = new GameObject("ObjectivesList");
                objListObj.transform.SetParent(missionPanel.transform, false);
                TextMeshProUGUI listText = objListObj.AddComponent<TextMeshProUGUI>();
                listText.fontSize = 14;
                listText.color = Color.white;
                RectTransform listRect = objListObj.GetComponent<RectTransform>();
                listRect.anchorMin = new Vector2(0f, 0f);
                listRect.anchorMax = new Vector2(1f, 0.45f);

                MissionUI missionUI = missionPanel.AddComponent<MissionUI>();
                var titleF = typeof(MissionUI).GetField("missionTitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var descF = typeof(MissionUI).GetField("missionDescriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var listF = typeof(MissionUI).GetField("objectivesListText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (titleF != null) titleF.SetValue(missionUI, titleText);
                if (descF != null) descF.SetValue(missionUI, descText);
                if (listF != null) listF.SetValue(missionUI, listText);
            }

            // Objective Marker
            GameObject markerObj = GameObject.Find("ObjectiveMarker");
            if (markerObj == null)
            {
                markerObj = new GameObject("ObjectiveMarker");
                ObjectiveMarker marker = markerObj.AddComponent<ObjectiveMarker>();

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "MarkerVisual";
                visual.transform.SetParent(markerObj.transform, false);
                visual.transform.localScale = Vector3.one * 0.5f;
                SetMaterialColor(visual, new Color(1f, 0.8f, 0.1f));

                var visF = typeof(ObjectiveMarker).GetField("markerVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (visF != null) visF.SetValue(marker, visual);
            }
        }
    }
}
