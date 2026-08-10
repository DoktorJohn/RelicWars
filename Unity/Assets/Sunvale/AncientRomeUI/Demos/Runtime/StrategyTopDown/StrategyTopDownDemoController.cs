using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.UI;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.StrategyTopDown
{
    public enum DemoProvinceBuildingType
    {
            noneExistingNull,
            brickwork,
            smithy,
            carpenterbowMaker,
            sawmill,
            flourBakerMill,
            farm,
            cattleFarm,
            horseBreeder,
            admin,
            waterWorks,
            temple,
            market
    }

    public enum DemoTradeRouteType
    {
            Land,
            Naval
    }

    [Serializable]
    public class DemoGovernorData
    {
            public string characterName;
            public string mainTitle = "Governor";
            public string extraTitle;

            public Sprite portraitSprite;

            public int administration;   // around 7-15
            public int influence;        // around 8-18
            public int command;          // around 3-10
            public int treasury;         // around 1-6
    }

    public class DemoTradeRouteData
    {
            public string destinationProvince;
            public DemoTradeRouteType routeType;
            public int income;
    }


    public class DemoGarrisonUnitData
    {
            public Sprite sprite;
            public int health;       // 0-100
            public int unitCount;    // 100-2800-ish

            public void HealOneTurn()
            {
                    health = Mathf.Clamp(health + Random.Range(4, 13), 0, 100);
            }
    }


    public class DemoConstructionQueueItem
    {
            public DemoProvinceBuildingType buildingType;

            [Range(1, 4)]
            public int targetLevel;

            public int turnsRemaining;

            
            public bool constructAsNewBuilding = false;

            public int sourceBuildingIndex = -1;
    }


    public class DemoProvinceBuildingData
    {
            public DemoProvinceBuildingType buildingType;

            [Range(1, 4)]
            public int level;

            public Sprite sprite;
    }


    public class DemoProvinceData
    {
            public string provinceName;

            public DemoGovernorData governor;

            public int publicOrder;
            public int infrastructure;

            public int maxTradeRoutes = 6;
            public int maxBuildingSlots = 6;

            public List<DemoTradeRouteData> tradeRoutes = new List<DemoTradeRouteData>();
            public List<DemoGarrisonUnitData> garrison = new List<DemoGarrisonUnitData>();
            public List<DemoProvinceBuildingData> buildings = new List<DemoProvinceBuildingData>();
            public List<DemoConstructionQueueItem> buildQueue = new List<DemoConstructionQueueItem>();

            public void PassOneTurn(Func<DemoProvinceBuildingType, int, Sprite> buildingSpriteResolver)
            {
                    for (int i = 0; i < garrison.Count; i++)
                    {
                            garrison[i].HealOneTurn();
                    }

                    if (buildQueue == null || buildQueue.Count == 0)
                            return;

                    DemoConstructionQueueItem activeConstruction = buildQueue[0];
                    activeConstruction.turnsRemaining--;

                    if (activeConstruction.turnsRemaining <= 0)
                    {
                            FinishBuilding(activeConstruction, buildingSpriteResolver);
                            buildQueue.RemoveAt(0);
                    }
            }

            private void FinishBuilding(
                    DemoConstructionQueueItem item,
                    Func<DemoProvinceBuildingType, int, Sprite> buildingSpriteResolver)
            {
                    if (item.buildingType == DemoProvinceBuildingType.noneExistingNull)
                            return;

                    int targetLevel = Mathf.Clamp(item.targetLevel, 1, 4);

                    if (item.constructAsNewBuilding)
                    {
                            if (buildings.Count >= maxBuildingSlots)
                                    return;

                            DemoProvinceBuildingData newBuilding = new DemoProvinceBuildingData();
                            newBuilding.buildingType = item.buildingType;
                            newBuilding.level = targetLevel;
                            newBuilding.sprite = buildingSpriteResolver(newBuilding.buildingType, newBuilding.level);

                            buildings.Add(newBuilding);
                            return;
                    }

                    DemoProvinceBuildingData existing = FindUpgradeTarget(item);

                    if (existing != null)
                    {
                            existing.level = targetLevel;
                            existing.sprite = buildingSpriteResolver(existing.buildingType, existing.level);
                            return;
                    }

                    if (buildings.Count >= maxBuildingSlots)
                            return;

                    DemoProvinceBuildingData fallbackNewBuilding = new DemoProvinceBuildingData();
                    fallbackNewBuilding.buildingType = item.buildingType;
                    fallbackNewBuilding.level = targetLevel;
                    fallbackNewBuilding.sprite = buildingSpriteResolver(fallbackNewBuilding.buildingType, fallbackNewBuilding.level);

                    buildings.Add(fallbackNewBuilding);
            }

            private DemoProvinceBuildingData FindUpgradeTarget(DemoConstructionQueueItem item)
            {
                    if (item.sourceBuildingIndex >= 0 && item.sourceBuildingIndex < buildings.Count)
                    {
                            DemoProvinceBuildingData indexedBuilding = buildings[item.sourceBuildingIndex];

                            if (indexedBuilding != null && indexedBuilding.buildingType == item.buildingType)
                                    return indexedBuilding;
                    }

                    return FindBuilding(item.buildingType);
            }

            public DemoProvinceBuildingData FindBuilding(DemoProvinceBuildingType type)
            {
                    for (int i = 0; i < buildings.Count; i++)
                    {
                            if (buildings[i].buildingType == type)
                                    return buildings[i];
                    }

                    return null;
            }
    }

    public class StrategyTopDownDemoController : MonoBehaviour
    {
            public List<SimpleButton> provinceButtons;

            public RPGSkillButton turnButton;
            public DemoProvincePanelController provincePanel;
            [Header("BuildingSprites")]
            public List<Sprite> brickworkSprites;
            public List<Sprite> smithykSprites;
            public List<Sprite> carpenterbowMakerSprites;
            public List<Sprite> sawmillSprites;
            public List<Sprite> flourBakerMill;
            public List<Sprite> farmSprites;
            public List<Sprite> cattleFarmSprites;
            public List<Sprite> horseBreederSprites;
            public List<Sprite> adminSprites;
            public List<Sprite> waterWorksrSprites;
            public List<Sprite> templeSprites;
            public List<Sprite> marketSprites;

            public DemoSpriteCollection spriteCollection;

            [Header("Generated Demo Data")]
            public List<DemoProvinceData> provinces = new List<DemoProvinceData>();

            private bool wasInitialized;
            
            private readonly string[] provinceNames =
            {
                    "Alexandria",
                    "Jerusalem",
                    "Antioch",
                    "Memphis"
            };

            private readonly string[] tradeTargets =
            {
                    "Rome",
                    "Athens",
                    "Sparta",
                    "Thebes",
                    "Cyrene",
                    "Sidon",
                    "Tyre",
                    "Damascus",
                    "Rhodes",
                    "Pergamon",
                    "Carthage",
                    "Ephesus"
            };

            private readonly Dictionary<string, string[]> characterDictionary =
                    new Dictionary<string, string[]>
            {
                    {
                            "praenomen",
                            new string[]
                            {
                                    "Aulus", "Gaius", "Lucius", "Marcus", "Titus",
                                    "Decimus", "Quintus", "Sextus"
                            }
                    },
                    {
                            "nomen",
                            new string[]
                            {
                                    "Fabius", "Julius", "Cassius", "Valerius", "Cornelius",
                                    "Aemilius", "Claudius", "Flavius"
                            }
                    },
                    {
                            "cognomen",
                            new string[]
                            {
                                    "Cato", "Drusus", "Varro", "Nerva", "Scipio",
                                    "Marcellus", "Sabinus", "Pulcher"
                            }
                    },
                    {
                            "extraTitle",
                            new string[]
                            {
                                    "Zealot", "Magistrate", "Prefect", "Quaestor",
                                    "Procurator", "Strategos", "Tribune", "Legate"
                            }
                    }
            };
            
            

            private void Start()
            {
                    InitializeWithRandomData();
            }

            public void InitializeWithRandomData()
            {
                    InnerInitialization();
                    MakeRandomProvinceData();
                    ProvinceButtonClicked(provinceButtons[0]);
            }

            private void InnerInitialization()
            {
                    if (wasInitialized)
                    {
                            return;
                    }
                    wasInitialized = true;
                    
                    for (var i = 0; i < provinceButtons.Count; i++)
                    {
                            var btn = provinceButtons[i];
                            btn.OnButtonActivatedClicked += ProvinceButtonClicked;
                    }

                    turnButton.OnPointerUpEvent += TurnButtonClicked;
            }

            private void TurnButtonClicked(RPGSkillButton thebutton, PointerEventData eventData)
            {
                   PassOneTurn();
            }

            private void ProvinceButtonClicked(SimpleButton theButton)
            {
                    int index = provinceButtons.IndexOf(theButton);

                    provincePanel.gameObject.SetActive(true);
                    provincePanel.InitializeForProvince(provinces[index]);
            }

            public void PassOneTurn()
            {
                    for (int i = 0; i < provinces.Count; i++)
                    {
                            provinces[i].PassOneTurn(GetBuildingSpriteForLevel);
                    }

                    if (provincePanel != null && provincePanel.gameObject.activeInHierarchy && provincePanel.currentProvince != null)
                    {
                            provincePanel.RefreshCurrentProvince();
                    }
            }

            private void MakeRandomProvinceData()
            {
                    provinces.Clear();

                    for (int i = 0; i < provinceNames.Length; i++)
                    {
                            DemoProvinceData province = new DemoProvinceData();

                            province.provinceName = provinceNames[i];
                            province.governor = MakeRandomGovernor();

                            province.publicOrder = Random.Range(50, 91);
                            province.infrastructure = Random.Range(15, 61);

                            province.maxTradeRoutes = 6;
                            province.maxBuildingSlots = 6;

                            province.tradeRoutes = MakeRandomTradeRoutes(province.provinceName);
                            province.garrison = MakeRandomUnits();
                            province.buildings = MakeRandomBuildings(province.maxBuildingSlots);
                            province.buildQueue = MakeRandomBuildQueue(province);

                            provinces.Add(province);
                    }
            }

            private DemoGovernorData MakeRandomGovernor()
            {
                    DemoGovernorData governor = new DemoGovernorData();

                    string first = Pick(characterDictionary["praenomen"]);
                    string middle = Pick(characterDictionary["nomen"]);
                    string last = Pick(characterDictionary["cognomen"]);

                    governor.characterName = first + " " + middle + " " + last;
                    governor.mainTitle = "Governor";
                    governor.extraTitle = Pick(characterDictionary["extraTitle"]);

                    governor.portraitSprite = spriteCollection != null ? PickSprite(spriteCollection.strategyTopDownPortraits) : null;

                    governor.administration = Random.Range(7, 16);
                    governor.influence = Random.Range(8, 19);
                    governor.command = Random.Range(3, 11);
                    governor.treasury = Random.Range(1, 7);

                    return governor;
            }

            private List<DemoTradeRouteData> MakeRandomTradeRoutes(string ownerProvinceName)
            {
                    List<DemoTradeRouteData> routes = new List<DemoTradeRouteData>();

                    int routeCount = Random.Range(3, 7);
                    int safety = 0;

                    while (routes.Count < routeCount && safety < 100)
                    {
                            safety++;

                            string target = Pick(tradeTargets);

                            if (target == ownerProvinceName)
                                    continue;

                            if (HasTradeRoute(routes, target))
                                    continue;

                            DemoTradeRouteData route = new DemoTradeRouteData();
                            route.destinationProvince = target;
                            route.routeType = Random.value > 0.45f
                                    ? DemoTradeRouteType.Naval
                                    : DemoTradeRouteType.Land;

                            route.income = MakeRouteIncome(target);

                            routes.Add(route);
                    }

                    return routes;
            }

            private int MakeRouteIncome(string target)
            {
                    if (target == "Rome")
                            return Random.Range(550, 901);

                    if (target == "Athens" || target == "Sparta" || target == "Rhodes")
                            return Random.Range(180, 421);

                    return Random.Range(70, 261);
            }

            private List<DemoGarrisonUnitData> MakeRandomUnits()
            {
                    List<DemoGarrisonUnitData> units = new List<DemoGarrisonUnitData>();

                    int wantedCount = Random.Range(4, 7);

                    List<Sprite> spritePool = new List<Sprite>();

                    if (spriteCollection != null && spriteCollection.strategyTopDownUnits != null)
                    {
                            for (int i = 0; i < spriteCollection.strategyTopDownUnits.Count; i++)
                                    spritePool.Add(spriteCollection.strategyTopDownUnits[i]);
                    }

                    for (int i = 0; i < wantedCount; i++)
                    {
                            DemoGarrisonUnitData unit = new DemoGarrisonUnitData();

                            if (spritePool.Count > 0)
                            {
                                    int spriteIndex = Random.Range(0, spritePool.Count);
                                    unit.sprite = spritePool[spriteIndex];
                                    spritePool.RemoveAt(spriteIndex);
                            }

                            unit.health = Random.Range(55, 101);

                            int countRoll = Random.Range(0, 100);

                            if (countRoll < 15)
                                    unit.unitCount = Random.Range(1, 5) * 100;
                            else if (countRoll < 55)
                                    unit.unitCount = Random.Range(5, 15) * 100;
                            else if (countRoll < 85)
                                    unit.unitCount = Random.Range(14, 22) * 100;
                            else
                                    unit.unitCount = Random.Range(21, 29) * 100;

                            units.Add(unit);
                    }

                    return units;
            }

            private List<DemoProvinceBuildingData> MakeRandomBuildings(int maxBuildingSlots)
            {
                    List<DemoProvinceBuildingData> buildings = new List<DemoProvinceBuildingData>();

                    int buildingCount = Random.Range(2, 4);
                    int safety = 0;

                    while (buildings.Count < buildingCount && safety < 100)
                    {
                            safety++;

                            DemoProvinceBuildingType type = PickRandomBuildingType();

                            if (HasBuilding(buildings, type))
                                    continue;

                            int level = PickRandomBuildingLevel();

                            DemoProvinceBuildingData building = new DemoProvinceBuildingData();
                            building.buildingType = type;
                            building.level = level;
                            building.sprite = GetBuildingSpriteForLevel(type, level);

                            buildings.Add(building);
                    }

                    return buildings;
            }

            private List<DemoConstructionQueueItem> MakeRandomBuildQueue(DemoProvinceData province)
            {
                    List<DemoConstructionQueueItem> queue = new List<DemoConstructionQueueItem>();

                    int queueCount = Random.Range(0, 2);
                    int safety = 0;

                    while (queue.Count < queueCount && safety < 100)
                    {
                            safety++;

                            DemoProvinceBuildingType type = PickRandomBuildingType();

                            if (HasQueueItem(queue, type))
                                    continue;

                            DemoProvinceBuildingData existing = province.FindBuilding(type);

                            bool canUpgrade = existing != null && existing.level < 4;
                            bool canBuildNew = existing == null && province.buildings.Count < province.maxBuildingSlots;

                            if (!canUpgrade && !canBuildNew)
                                    continue;

                            int targetLevel = existing == null ? 1 : existing.level + 1;

                            DemoConstructionQueueItem item = new DemoConstructionQueueItem();
                            item.buildingType = type;
                            item.targetLevel = Mathf.Clamp(targetLevel, 1, 4);
                            item.turnsRemaining = TurnsForBuildingLevel(item.targetLevel);
                            item.constructAsNewBuilding = existing == null;
                            item.sourceBuildingIndex = existing == null ? -1 : province.buildings.IndexOf(existing);
                            
                            queue.Add(item);
                    }

                    return queue;
            }

            private int TurnsForBuildingLevel(int level)
            {
                    // Level 1 = 2 turns, level 2 = 3 turns, level 3 = 4 turns, level 4 = 5 turns.
                    return Mathf.Clamp(level + 1, 2, 5);
            }

            public Sprite GetBuildingSpriteForLevel(DemoProvinceBuildingType type, int level)
            {
                    List<Sprite> sprites = GetBuildingSpriteList(type);

                    if (sprites == null || sprites.Count == 0)
                            return null;

                    int spriteIndex = Mathf.Clamp(level - 1, 0, sprites.Count - 1);
                    return sprites[spriteIndex];
            }

            private List<Sprite> GetBuildingSpriteList(DemoProvinceBuildingType type)
            {
                    switch (type)
                    {
                            case DemoProvinceBuildingType.brickwork:
                                    return brickworkSprites;

                            case DemoProvinceBuildingType.smithy:
                                    return smithykSprites;

                            case DemoProvinceBuildingType.carpenterbowMaker:
                                    return carpenterbowMakerSprites;

                            case DemoProvinceBuildingType.sawmill:
                                    return sawmillSprites;

                            case DemoProvinceBuildingType.flourBakerMill:
                                    return flourBakerMill;

                            case DemoProvinceBuildingType.farm:
                                    return farmSprites;

                            case DemoProvinceBuildingType.cattleFarm:
                                    return cattleFarmSprites;

                            case DemoProvinceBuildingType.horseBreeder:
                                    return horseBrederSafe();

                            case DemoProvinceBuildingType.admin:
                                    return adminSprites;

                            case DemoProvinceBuildingType.waterWorks:
                                    return waterWorksrSprites;

                            case DemoProvinceBuildingType.temple:
                                    return templeSprites;

                            case DemoProvinceBuildingType.market:
                                    return marketSprites;

                            default:
                                    return null;
                    }
            }

            private List<Sprite> horseBrederSafe()
            {
                    return horseBreederSprites;
            }

            private DemoProvinceBuildingType PickRandomBuildingType()
            {
                    Array values = Enum.GetValues(typeof(DemoProvinceBuildingType));

                    // Start at 1 so we skip noneExistingNull.
                    int index = Random.Range(1, values.Length);

                    return (DemoProvinceBuildingType)values.GetValue(index);
            }

            private int PickRandomBuildingLevel()
            {
                    int roll = Random.Range(0, 100);

                    if (roll < 45)
                            return 1;

                    if (roll < 75)
                            return 2;

                    if (roll < 93)
                            return 3;

                    return 4;
            }

            private bool HasTradeRoute(List<DemoTradeRouteData> routes, string destination)
            {
                    for (int i = 0; i < routes.Count; i++)
                    {
                            if (routes[i].destinationProvince == destination)
                                    return true;
                    }

                    return false;
            }

            private bool HasBuilding(List<DemoProvinceBuildingData> buildings, DemoProvinceBuildingType type)
            {
                    for (int i = 0; i < buildings.Count; i++)
                    {
                            if (buildings[i].buildingType == type)
                                    return true;
                    }

                    return false;
            }

            private bool HasQueueItem(List<DemoConstructionQueueItem> queue, DemoProvinceBuildingType type)
            {
                    for (int i = 0; i < queue.Count; i++)
                    {
                            if (queue[i].buildingType == type)
                                    return true;
                    }

                    return false;
            }

            private Sprite PickSprite(List<Sprite> sprites)
            {
                    if (sprites == null || sprites.Count == 0)
                            return null;

                    return sprites[Random.Range(0, sprites.Count)];
            }

            private string Pick(string[] values)
            {
                    if (values == null || values.Length == 0)
                            return string.Empty;

                    return values[Random.Range(0, values.Length)];
            }
    }
}
