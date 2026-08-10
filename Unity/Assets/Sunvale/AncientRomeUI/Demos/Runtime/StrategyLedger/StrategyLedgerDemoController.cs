using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class StrategyLedgerDemoController : MonoBehaviour
    {
        [NonSerialized] public List<RomeCityData> myData;


        public Sprite villigeIcon;
        public Sprite smallTownIcon;
        public Sprite mediumTownIcon;
        public Sprite largeTownIcon;

        public Sprite wrathMoodIcon;
        public Sprite unhappyMoodIcon;
        public Sprite averageMoodIcon;
        public Sprite goodMoodIcon;
        public Sprite ecstaticMoodIcon;


        public DemoSpriteCollection spriteCollection;

        [NonSerialized] public RomeGovernmentData governmentData;
        [NonSerialized] public RomeEmpireStatsData empireStatsData;
        [NonSerialized] public RomeMilitaryStatsData militaryStatsData;
        
        
        public List<FramedSpriteTabButton> myTopCategoryTabs;

        public DemoEmpireOverviewCategoryView demoEmpireOverviewCategotyDisplayer;
        public DemoDemographicsTableController demographicsCategoryDisplayer;

        private GameObject currentContentGO;
        private int currentContentIndex;

        private void Awake()
        {
            //will be removed and initialization will be called externally.
            Initialize();
        }

        private void Initialize()
        {
            InnerInitialization();
            MakeFakeCitiesDataSet();
            MakeFakeEmpireStatsData();
            MakeFakeGovernmentData();
            MakeFakeMilitaryStatsData();
            SelectContentToShow(1);
        }

        private void SelectContentToShow(int tabIndex)
        {
            if (tabIndex == currentContentIndex)
            {
                return;
            }

            if (currentContentIndex != -1)
            {
                var tabToDeselect = myTopCategoryTabs[currentContentIndex];
                tabToDeselect.SetAsDeselected(true);
                currentContentGO.SetActive(false);
            }

            currentContentIndex = tabIndex;
            var tab = myTopCategoryTabs[tabIndex];
            tab.SetAsSelectedAsPrime(true);
            switch (tabIndex)
            {
                case 0:
                    demoEmpireOverviewCategotyDisplayer.Initialize();
                    currentContentGO = demoEmpireOverviewCategotyDisplayer.gameObject;
                    currentContentGO.SetActive(true);
                    break;
                case 1:
                    demographicsCategoryDisplayer.Initialize();
                    currentContentGO = demographicsCategoryDisplayer.gameObject;
                    currentContentGO.SetActive(true);
                    break;
                case 2:
                    demoEmpireOverviewCategotyDisplayer.Initialize();
                    currentContentGO = demoEmpireOverviewCategotyDisplayer.gameObject;
                    currentContentGO.SetActive(true);
                    break;
                case 3:
                    demographicsCategoryDisplayer.Initialize();
                    currentContentGO = demographicsCategoryDisplayer.gameObject;
                    currentContentGO.SetActive(true);
                    break;
            }
        }

        private void InnerInitialization()
        {
            currentContentIndex = -1;
            for (var i = 0; i < myTopCategoryTabs.Count; i++)
            {
                var tab = myTopCategoryTabs[i];
                tab.OnButtonActivatedClicked += TabClicked;
            }
        }

        private void TabClicked(FramedSpriteTabButton theTab)
        {
            SelectContentToShow(myTopCategoryTabs.IndexOf(theTab));
        }
        
        public void MakeFakeEmpireStatsData()
        {
            if (myData == null || myData.Count == 0)
                MakeFakeCitiesDataSet();

            empireStatsData = new RomeEmpireStatsData();
            empireStatsData.MakeTheData(myData);
        }
        
        public void MakeFakeMilitaryStatsData()
        {
            if (myData == null || myData.Count == 0)
                MakeFakeCitiesDataSet();

            if (empireStatsData == null)
                MakeFakeEmpireStatsData();

            militaryStatsData = new RomeMilitaryStatsData();
            militaryStatsData.GenerateMilitaryData(this, empireStatsData);
        }

        public void MakeFakeGovernmentData()
        {
            governmentData = new RomeGovernmentData();
            governmentData.GenerateRandomGovernmentData(this);
        }

        public void MakeFakeCitiesDataSet()
        {
            myData = new List<RomeCityData>();

            string[] cityNames =
            {
                "Roma", "Capua", "Ostia", "Neapolis", "Mediolanum",
                "Ravenna", "Pompeii", "Aquileia", "Syracusae", "Carthago",
                "Londinium", "Eboracum", "Lutetia", "Massilia", "Arelate",
                "Tarraco", "Emerita", "Corduba", "Gades", "Toletum",
                "Alexandria", "Antiochia", "Ephesus", "Pergamum", "Smyrna",
                "Athenae", "Corinthus", "Byzantium", "Nicomedia", "Cyrene",
                "Leptis Magna", "Sabratha", "Tingis", "Lugdunum", "Vindobona",
                "Carnuntum", "Sirmium", "Salona", "Dyrrachium", "Burdigala"
            };

            string[] provinceNames =
            {
                "Italia", "Italia", "Italia", "Italia", "Italia",
                "Italia", "Italia", "Italia", "Sicilia", "Africa",
                "Britannia", "Britannia", "Gallia", "Gallia", "Gallia",
                "Hispania", "Hispania", "Hispania", "Hispania", "Hispania",
                "Aegyptus", "Syria", "Asia", "Asia", "Asia",
                "Achaea", "Achaea", "Thracia", "Bithynia", "Cyrenaica",
                "Africa", "Africa", "Mauretania", "Gallia", "Pannonia",
                "Pannonia", "Pannonia", "Dalmatia", "Macedonia", "Aquitania"
            };

            for (int i = 0; i < 24; i++)
            {
                int population = GetRandomPopulation(i);
                int mood = UnityEngine.Random.Range(0, 101);

                int growth = Mathf.RoundToInt((mood - 50) / 15f) + UnityEngine.Random.Range(-1, 2);
                growth = Mathf.Clamp(growth, -5, 5);

                int taxPerCapita = UnityEngine.Random.Range(2, 9);

                if (mood <= 20)
                    taxPerCapita -= 2;
                else if (mood <= 40)
                    taxPerCapita -= 1;
                else if (mood >= 81)
                    taxPerCapita += 1;

                taxPerCapita = Mathf.Clamp(taxPerCapita, 1, 10);

                int taxes = population * taxPerCapita;
                taxes = RoundToNearest(taxes, 100);

                RomeCityData data = new RomeCityData
                {
                    cityName = cityNames[i],
                    provinceName = provinceNames[i],

                    population = population,
                    growth = growth,
                    taxes = taxes,
                    taxPerCapita = taxPerCapita,
                    mood = mood,

                    cityIcon = GetCityIcon(population),
                    cityMoodIcon = GetMoodIcon(mood)
                };

                float citySize = Mathf.InverseLerp(2000, 250000, population);

                float[] statusWeights =
                {
                    UnityEngine.Random.Range(0.18f, 0.30f), // dependant
                    UnityEngine.Random.Range(0.06f, 0.14f) + citySize * 0.04f, // slaves
                    UnityEngine.Random.Range(0.32f, 0.48f), // plebs
                    UnityEngine.Random.Range(0.14f, 0.24f), // freemen
                    UnityEngine.Random.Range(0.02f, 0.07f) + citySize * 0.04f, // merchants
                    UnityEngine.Random.Range(0.003f, 0.018f) + citySize * 0.015f // patricians
                };

                int[] status = SplitPopulation(population, statusWeights);

                data.dependant = status[0];
                data.slaves = status[1];
                data.plebs = status[2];
                data.freemen = status[3];
                data.merchants = status[4];
                data.patricians = status[5];

                float growthBonus = growth * 0.005f;

                float[] ageWeights =
                {
                    UnityEngine.Random.Range(0.18f, 0.26f) + growthBonus, // children
                    UnityEngine.Random.Range(0.10f, 0.17f), // youth
                    UnityEngine.Random.Range(0.42f, 0.52f), // adults
                    UnityEngine.Random.Range(0.08f, 0.14f), // seniors
                    UnityEngine.Random.Range(0.03f, 0.07f) - growthBonus // elders
                };

                int[] age = SplitPopulation(population, ageWeights);

                data.children = age[0];
                data.youth = age[1];
                data.adults = age[2];
                data.seniors = age[3];
                data.elders = age[4];

                myData.Add(data);
            }
        }

        private int GetRandomPopulation(int index)
        {
            // Make the first few entries feel more important.
            if (index == 0)
                return RoundToNearest(UnityEngine.Random.Range(150000, 250001), 1000);

            if (index == 1 || index == 2)
                return RoundToNearest(UnityEngine.Random.Range(80000, 150001), 1000);

            float roll = UnityEngine.Random.value;

            if (roll < 0.45f)
                return RoundToNearest(UnityEngine.Random.Range(2000, 7501), 100);

            if (roll < 0.75f)
                return RoundToNearest(UnityEngine.Random.Range(7500, 25001), 500);

            if (roll < 0.93f)
                return RoundToNearest(UnityEngine.Random.Range(25000, 80001), 1000);

            return RoundToNearest(UnityEngine.Random.Range(80000, 180001), 1000);
        }

        private Sprite GetCityIcon(int population)
        {
            if (population < 7500)
                return villigeIcon;

            if (population < 25000)
                return smallTownIcon;

            if (population < 80000)
                return mediumTownIcon;

            return largeTownIcon;
        }

        private Sprite GetMoodIcon(int mood)
        {
            if (mood <= 20)
                return wrathMoodIcon;

            if (mood <= 40)
                return unhappyMoodIcon;

            if (mood <= 60)
                return averageMoodIcon;

            if (mood <= 80)
                return goodMoodIcon;

            return ecstaticMoodIcon;
        }

        private static int[] SplitPopulation(int total, float[] weights)
        {
            int[] values = new int[weights.Length];

            float weightTotal = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Mathf.Max(0.001f, weights[i]);
                weightTotal += weights[i];
            }

            int used = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                values[i] = Mathf.FloorToInt(total * (weights[i] / weightTotal));
                used += values[i];
            }

            int remaining = total - used;

            while (remaining > 0)
            {
                int index = UnityEngine.Random.Range(0, values.Length);
                values[index]++;
                remaining--;
            }

            return values;
        }

        private static int RoundToNearest(int value, int nearest)
        {
            return Mathf.RoundToInt(value / (float) nearest) * nearest;
        }
    }


    [Serializable]
    public class RomeGovernmentData
    {
        public int totalSeats = 300;

        public RomeCharacterData consulA;
        public RomeCharacterData consulB;
        public RomeCharacterData senateLeader;
        public RomeCharacterData praetor;

        public List<RomeSenateFactionData> factions = new();

        private static readonly string[] RomanNames =
        {
            "Lucius Cornelius Scipio",
            "Marcus Aemilius Lepidus",
            "Gaius Julius Varro",
            "Publius Claudius Pulcher",
            "Quintus Fabius Maximus",
            "Titus Manlius Torquatus",
            "Aulus Postumius Albinus",
            "Sextus Pompeius Magnus",
            "Decimus Junius Brutus",
            "Gnaeus Domitius Ahenobarbus",
            "Servius Sulpicius Rufus",
            "Appius Claudius Caecus",
            "Manius Valerius Messalla",
            "Spurius Cassius Vecellinus",
            "Numerius Fabius Pictor",
            "Tiberius Sempronius Gracchus",
            "Marcus Porcius Cato",
            "Lucius Licinius Crassus",
            "Gaius Marius Victor",
            "Publius Cornelius Lentulus",
            "Quintus Caecilius Metellus",
            "Titus Quinctius Flamininus",
            "Aulus Gabinius Severus",
            "Gnaeus Octavius Rullus",
            "Marcus Livius Drusus"
        };

        private static readonly string[] FactionNames =
        {
            "Optimates",
            "Populares",
            "Militarists",
            "Traditionalist",
            "Zealots",
            "Unaligned"
        };

        public void GenerateRandomGovernmentData(StrategyLedgerDemoController ledger)
        {
            factions.Clear();

            List<string> namePool = new(RomanNames);

            consulA = CreateRandomCharacter(namePool, ledger);
            consulB = CreateRandomCharacter(namePool, ledger);
            senateLeader = CreateRandomCharacter(namePool, ledger);
            praetor = CreateRandomCharacter(namePool, ledger);

            // Random faction seat distribution.
            // Unaligned is included and usually kept smaller, but can still matter.
            int factionCount = FactionNames.Length;

            const string unalignedFactionName = "Unaligned";
            const string traditionalistFactionName = "Traditionalist";

            int minSeatsPerFaction = Mathf.CeilToInt(totalSeats * 0.05f);

            // Safety fallback in case totalSeats is ever too small.
            if (minSeatsPerFaction * factionCount > totalSeats)
                minSeatsPerFaction = Mathf.Max(1, totalSeats / factionCount);

            // Keep Traditionalist visually small for the demo.
            // The cap is below 9.5%, so the rounded UI influence value stays below 10%.
            int traditionalistMaxSeats = Mathf.CeilToInt(totalSeats * 0.095f) - 1;
            traditionalistMaxSeats = Mathf.Max(minSeatsPerFaction, traditionalistMaxSeats);

            int[] seatsByFaction = new int[factionCount];

            for (int i = 0; i < factionCount; i++)
                seatsByFaction[i] = minSeatsPerFaction;

            // Pick one dominant faction.
            // I exclude Unaligned and Traditionalist so they stay like minor/no-party groups.
            List<int> dominantCandidates = new();

            for (int i = 0; i < factionCount; i++)
            {
                if (FactionNames[i] != unalignedFactionName &&
                    FactionNames[i] != traditionalistFactionName)
                {
                    dominantCandidates.Add(i);
                }
            }

            if (dominantCandidates.Count == 0)
            {
                for (int i = 0; i < factionCount; i++)
                    dominantCandidates.Add(i);
            }

            int dominantIndex = dominantCandidates[UnityEngine.Random.Range(0, dominantCandidates.Count)];

            int dominantMinSeats = Mathf.CeilToInt(totalSeats * 0.25f);
            int dominantMaxSeats = Mathf.FloorToInt(totalSeats * 0.30f);

            dominantMinSeats = Mathf.Max(dominantMinSeats, minSeatsPerFaction);

            // Make sure dominant faction still leaves the floor for everyone else.
            dominantMaxSeats = Mathf.Min(
                dominantMaxSeats,
                totalSeats - minSeatsPerFaction * (factionCount - 1)
            );

            dominantMaxSeats = Mathf.Max(dominantMinSeats, dominantMaxSeats);

            int dominantSeats = UnityEngine.Random.Range(dominantMinSeats, dominantMaxSeats + 1);
            seatsByFaction[dominantIndex] = dominantSeats;

            int remainingSeats = totalSeats - dominantSeats - minSeatsPerFaction * (factionCount - 1);

            // Caps prevent another faction from accidentally becoming equal/bigger than the dominant faction.
            int[] maxSeatsByFaction = new int[factionCount];

            for (int i = 0; i < factionCount; i++)
            {
                if (i == dominantIndex)
                {
                    maxSeatsByFaction[i] = dominantSeats;
                }
                else if (FactionNames[i] == traditionalistFactionName)
                {
                    maxSeatsByFaction[i] = Mathf.Min(traditionalistMaxSeats, dominantSeats - 1);
                }
                else if (FactionNames[i] == unalignedFactionName)
                {
                    maxSeatsByFaction[i] = Mathf.Min(45, dominantSeats - 1);
                }
                else
                {
                    maxSeatsByFaction[i] = Mathf.Min(Mathf.FloorToInt(totalSeats * 0.22f), dominantSeats - 1);
                }

                maxSeatsByFaction[i] = Mathf.Max(maxSeatsByFaction[i], minSeatsPerFaction);
            }

            // Give each non-dominant faction a different weight.
            // This makes the result less even.
            float[] weights = new float[factionCount];

            for (int i = 0; i < factionCount; i++)
            {
                if (i == dominantIndex)
                {
                    weights[i] = 0f;
                }
                else if (FactionNames[i] == traditionalistFactionName)
                {
                    weights[i] = UnityEngine.Random.Range(0.05f, 0.25f);
                }
                else if (FactionNames[i] == unalignedFactionName)
                {
                    weights[i] = UnityEngine.Random.Range(0.20f, 0.65f);
                }
                else
                {
                    weights[i] = UnityEngine.Random.Range(0.35f, 2.25f);
                }
            }

            while (remainingSeats > 0)
            {
                float totalWeight = 0f;

                for (int i = 0; i < factionCount; i++)
                {
                    if (i == dominantIndex)
                        continue;

                    if (seatsByFaction[i] >= maxSeatsByFaction[i])
                        continue;

                    totalWeight += weights[i];
                }

                // Safety fallback. Should rarely/never happen with the caps above.
                if (totalWeight <= 0f)
                {
                    bool assignedFallbackSeat = false;

                    for (int i = 0; i < factionCount && remainingSeats > 0; i++)
                    {
                        if (i == dominantIndex)
                            continue;

                        if (seatsByFaction[i] >= maxSeatsByFaction[i])
                            continue;

                        seatsByFaction[i]++;
                        remainingSeats--;
                        assignedFallbackSeat = true;
                    }

                    // Last-resort safety so the total still adds up even if caps are too tight.
                    if (!assignedFallbackSeat)
                    {
                        seatsByFaction[dominantIndex]++;
                        remainingSeats--;
                    }

                    continue;
                }

                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float current = 0f;

                for (int i = 0; i < factionCount; i++)
                {
                    if (i == dominantIndex)
                        continue;

                    if (seatsByFaction[i] >= maxSeatsByFaction[i])
                        continue;

                    current += weights[i];

                    if (roll <= current)
                    {
                        seatsByFaction[i]++;
                        remainingSeats--;
                        break;
                    }
                }
            }

            for (int i = 0; i < factionCount; i++)
            {
                int seats = seatsByFaction[i];

                factions.Add(new RomeSenateFactionData
                {
                    name = FactionNames[i],
                    seats = seats,
                    influence = Mathf.RoundToInt((seats / (float) totalSeats) * 100f)
                });
            }

            ShuffleFactions();
        }

        private int GetWeightedRandomIndex(float[] weights)
        {
            float totalWeight = 0f;

            for (int i = 0; i < weights.Length; i++)
                totalWeight += Mathf.Max(0f, weights[i]);

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float current = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                current += Mathf.Max(0f, weights[i]);

                if (roll <= current)
                    return i;
            }

            return weights.Length - 1;
        }

        private RomeCharacterData CreateRandomCharacter(List<string> namePool, StrategyLedgerDemoController ledger)
        {
            int index = UnityEngine.Random.Range(0, namePool.Count);
            string selectedName = namePool[index];
            namePool.RemoveAt(index);
            
            int spriteIdx = 0;
            Sprite portraitSprite = null;

            if (ledger != null && ledger.spriteCollection != null && ledger.spriteCollection.strategyLedgerPortraits != null && ledger.spriteCollection.strategyLedgerPortraits.Count > 0)
            {
                spriteIdx = UnityEngine.Random.Range(0, ledger.spriteCollection.strategyLedgerPortraits.Count);
                portraitSprite = ledger.spriteCollection.strategyLedgerPortraits[spriteIdx];
            }

            return new RomeCharacterData
            {
                name = selectedName,
                portraitIconIndex = spriteIdx,
                portraitSprite = portraitSprite
            };
        }

        private void ShuffleFactions()
        {
            for (int i = 0; i < factions.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, factions.Count);
                (factions[i], factions[randomIndex]) = (factions[randomIndex], factions[i]);
            }
        }
    }

    [Serializable]
    public class RomeCharacterData
    {
        public string name;
        public int portraitIconIndex;
        public Sprite portraitSprite;
    }

    [Serializable]
    public class RomeSenateFactionData
    {
        public string name;
        public int seats;

        // Useful for UI bars, pie charts, tooltip summaries, etc.
        public int influence;
    }

    [Serializable]
    public class RomeMilitaryStatsData
    {
        public RomeCharacterData legatus;
        public RomeCharacterData primusLegionis;

        public int availableManpower;

        // Army
        public int legions;
        public int maxLegions;
        public int legionsPercent;

        public int infantryPercent;
        public int auxiliaryPercent;
        public int skirmishersPercent;
        public int equitesPercent;
        public int siegePercent;
        public int logisticsPercent;

        // Navy
        public int shipsPercent;
        public int transportsPercent;

        // Fortifications / supplies
        public int heavyFortifications;
        public int maxHeavyFortifications;

        public int lightFortifications;
        public int maxLightFortifications;

        public int provisionsPercent;

        private static readonly string[] MilitaryNames =
        {
            "Marcus Sulpicius",
            "Gaius Marcellus",
            "Lucius Vorenus",
            "Titus Pullo",
            "Aulus Flavius",
            "Quintus Varro",
            "Publius Scaeva",
            "Gnaeus Drusus",
            "Decimus Corvus",
            "Sextus Aelius",
            "Manius Falco",
            "Appius Lentulus",
            "Servius Gracchus",
            "Tiberius Varus",
            "Numerius Sabinus"
        };

        public void GenerateMilitaryData(StrategyLedgerDemoController ledger, RomeEmpireStatsData empireStatsData)
        {
            if (empireStatsData == null)
                return;

            List<string> namePool = new(MilitaryNames);

            legatus = CreateRandomCharacter(namePool, ledger);
            primusLegionis = CreateRandomCharacter(namePool, ledger);

            availableManpower = RoundToNearest(Mathf.RoundToInt(empireStatsData.population * 0.15f), 1000);

            float populationScale = Mathf.InverseLerp(50000f, 1000000f, empireStatsData.population);
            float taxScale = Mathf.InverseLerp(25000f, 600000f, empireStatsData.taxes);
            float strengthScale = Mathf.Lerp(populationScale, taxScale, 0.35f);

            maxLegions = Mathf.RoundToInt(Mathf.Lerp(8f, 24f, strengthScale));
            maxLegions += UnityEngine.Random.Range(-2, 4);
            maxLegions = Mathf.Clamp(maxLegions, 6, 30);

            legions = Mathf.RoundToInt(maxLegions * UnityEngine.Random.Range(0.58f, 0.86f));
            legions = Mathf.Clamp(legions, 1, maxLegions);

            legionsPercent = Mathf.RoundToInt((legions / (float) maxLegions) * 100f);

            infantryPercent = RandomPercent(48, 78, strengthScale);
            auxiliaryPercent = RandomPercent(32, 68, strengthScale);
            skirmishersPercent = RandomPercent(25, 62, strengthScale);
            equitesPercent = RandomPercent(18, 54, strengthScale);
            siegePercent = RandomPercent(12, 42, taxScale);
            logisticsPercent = RandomPercent(38, 86, taxScale);

            shipsPercent = RandomPercent(18, 64, taxScale);
            transportsPercent = RandomPercent(24, 72, taxScale);

            maxHeavyFortifications = Mathf.RoundToInt(Mathf.Lerp(10f, 28f, strengthScale));
            maxHeavyFortifications += UnityEngine.Random.Range(-2, 3);
            maxHeavyFortifications = Mathf.Clamp(maxHeavyFortifications, 6, 36);

            heavyFortifications = Mathf.RoundToInt(maxHeavyFortifications * UnityEngine.Random.Range(0.45f, 0.82f));
            heavyFortifications = Mathf.Clamp(heavyFortifications, 1, maxHeavyFortifications);

            maxLightFortifications = maxHeavyFortifications + UnityEngine.Random.Range(4, 13);
            maxLightFortifications = Mathf.Clamp(maxLightFortifications, 10, 48);

            lightFortifications = Mathf.RoundToInt(maxLightFortifications * UnityEngine.Random.Range(0.52f, 0.90f));
            lightFortifications = Mathf.Clamp(lightFortifications, 1, maxLightFortifications);

            float provisionPressure = Mathf.InverseLerp(4f, 30f, legions);
            int minProvision = Mathf.RoundToInt(Mathf.Lerp(62f, 24f, provisionPressure));
            int maxProvision = Mathf.RoundToInt(Mathf.Lerp(95f, 68f, provisionPressure));

            provisionsPercent = UnityEngine.Random.Range(minProvision, maxProvision + 1);
            provisionsPercent = Mathf.Clamp(provisionsPercent, 8, 100);
        }

        private static RomeCharacterData CreateRandomCharacter(List<string> namePool, StrategyLedgerDemoController ledger)
        {
            string selectedName;

            if (namePool.Count > 0)
            {
                int nameIndex = UnityEngine.Random.Range(0, namePool.Count);
                selectedName = namePool[nameIndex];
                namePool.RemoveAt(nameIndex);
            }
            else
            {
                selectedName = "M. Sulpicius";
            }

            Sprite portraitSprite = null;
            int portraitIconIndex = -1;

            if (ledger != null && ledger.spriteCollection != null && ledger.spriteCollection.strategyLedgerPortraits != null && ledger.spriteCollection.strategyLedgerPortraits.Count > 0)
            {
                portraitIconIndex = UnityEngine.Random.Range(0, ledger.spriteCollection.strategyLedgerPortraits.Count);
                portraitSprite = ledger.spriteCollection.strategyLedgerPortraits[portraitIconIndex];
            }

            return new RomeCharacterData
            {
                name = selectedName,
                portraitIconIndex = portraitIconIndex,
                portraitSprite = portraitSprite
            };
        }

        private static int RandomPercent(int min, int max, float qualityScale)
        {
            float random = UnityEngine.Random.Range(min, max + 1);
            float qualityBonus = Mathf.Lerp(-8f, 8f, qualityScale);

            return Mathf.Clamp(Mathf.RoundToInt(random + qualityBonus), 0, 100);
        }

        private static int RoundToNearest(int value, int nearest)
        {
            return Mathf.RoundToInt(value / (float) nearest) * nearest;
        }
    }


    [Serializable]
    public class RomeEmpireStatsData
    {
        public int population;
        public float growth;
        public float taxes;
        public float taxPerCapita;
        public float mood;

        public int dependant;
        public int slaves;
        public int plebs;
        public int freemen;
        public int merchants;
        public int patricians;

        public int children;
        public int youth;
        public int adults;
        public int seniors;
        public int elders;

        // treasury
        public float treasuryBalance;
        public float tradeIncome;
        public float mintingIncome;
        public float tariffsIncome;
        public float plunderIncome;
        public float totalIncome;

        public float armyExpenditure;
        public float navyExpenditure;
        public float administrationExpenditure;
        public float constructionExpenditure;
        public float inflationExpenditure;
        public float totalExpenditure;

        // production
        public float wheatProduction;
        public float wheatPercent;
        public float meatProduction;
        public float meatPercent;
        public float woodProduction;
        public float woodPercent;
        public float linenProduction;
        public float linenPercent;
        public float olivesProduction;
        public float olivesPercent;
        public float horsesProduction;
        public float horsesPercent;
        public float bronzeProduction;
        public float bronzePercent;
        public float produceProduction;
        public float producePercent;

        public void MakeTheData(List<RomeCityData> cityData)
        {
            ResetData();

            if (cityData == null || cityData.Count == 0)
                return;

            float weightedGrowth = 0f;
            float weightedMood = 0f;

            for (int i = 0; i < cityData.Count; i++)
            {
                RomeCityData city = cityData[i];

                population += city.population;
                taxes += city.taxes;

                weightedGrowth += city.growth * city.population;
                weightedMood += city.mood * city.population;

                dependant += city.dependant;
                slaves += city.slaves;
                plebs += city.plebs;
                freemen += city.freemen;
                merchants += city.merchants;
                patricians += city.patricians;

                children += city.children;
                youth += city.youth;
                adults += city.adults;
                seniors += city.seniors;
                elders += city.elders;
            }

            float safePopulation = Mathf.Max(1f, population);

            growth = RoundOne(weightedGrowth / safePopulation);
            mood = RoundOne(weightedMood / safePopulation);
            taxes = RoundMoney(taxes);
            taxPerCapita = RoundOne(taxes / safePopulation);

            GenerateTreasuryData();
            GenerateProductionData();
        }

        private void GenerateTreasuryData()
        {
            float baseTaxes = Mathf.Max(10000f, taxes);

            tradeIncome = RoundMoney(baseTaxes * UnityEngine.Random.Range(0.18f, 0.42f));
            mintingIncome = RoundMoney(baseTaxes * UnityEngine.Random.Range(0.04f, 0.12f));
            tariffsIncome = RoundMoney(baseTaxes * UnityEngine.Random.Range(0.06f, 0.18f));
            plunderIncome = RoundMoney(baseTaxes * UnityEngine.Random.Range(0.01f, 0.03f));

            totalIncome = RoundMoney(
                baseTaxes +
                tradeIncome +
                mintingIncome +
                tariffsIncome +
                plunderIncome
            );

            float expenditureBudget = totalIncome * UnityEngine.Random.Range(0.68f, 0.90f);

            float armyWeight = UnityEngine.Random.Range(0.32f, 0.46f);
            float navyWeight = UnityEngine.Random.Range(0.10f, 0.20f);
            float administrationWeight = UnityEngine.Random.Range(0.18f, 0.30f);
            float constructionWeight = UnityEngine.Random.Range(0.12f, 0.24f);
            float inflationWeight = UnityEngine.Random.Range(0.04f, 0.12f);

            float weightTotal =
                armyWeight +
                navyWeight +
                administrationWeight +
                constructionWeight +
                inflationWeight;

            armyExpenditure = RoundMoney(expenditureBudget * armyWeight / weightTotal);
            navyExpenditure = RoundMoney(expenditureBudget * navyWeight / weightTotal);
            administrationExpenditure = RoundMoney(expenditureBudget * administrationWeight / weightTotal);
            constructionExpenditure = RoundMoney(expenditureBudget * constructionWeight / weightTotal);
            inflationExpenditure = RoundMoney(expenditureBudget * inflationWeight / weightTotal);

            totalExpenditure = RoundMoney(
                armyExpenditure +
                navyExpenditure +
                administrationExpenditure +
                constructionExpenditure +
                inflationExpenditure
            );

            // Safety: demo treasury should not show a deficit.
            if (totalExpenditure >= totalIncome)
            {
                float scale = (totalIncome * UnityEngine.Random.Range(0.78f, 0.92f)) / totalExpenditure;

                armyExpenditure = RoundMoney(armyExpenditure * scale);
                navyExpenditure = RoundMoney(navyExpenditure * scale);
                administrationExpenditure = RoundMoney(administrationExpenditure * scale);
                constructionExpenditure = RoundMoney(constructionExpenditure * scale);
                inflationExpenditure = RoundMoney(inflationExpenditure * scale);

                totalExpenditure = RoundMoney(
                    armyExpenditure +
                    navyExpenditure +
                    administrationExpenditure +
                    constructionExpenditure +
                    inflationExpenditure
                );
            }

            float monthlySurplus = Mathf.Max(0f, totalIncome - totalExpenditure);

            treasuryBalance = RoundMoney(
                UnityEngine.Random.Range(totalIncome * 0.06f, totalIncome * 0.22f) +
                monthlySurplus * UnityEngine.Random.Range(1.5f, 5.5f)
            );
        }

        private void GenerateProductionData()
        {
            float populationScale = Mathf.InverseLerp(50000f, 1000000f, population);

            float wheatBase = Mathf.Lerp(4500f, 14500f, populationScale);
            wheatBase *= UnityEngine.Random.Range(0.90f, 1.12f);

            wheatProduction = RoundGoods(Mathf.Clamp(wheatBase, 1000f, 15000f));

            meatProduction = RoundGoods(wheatProduction * UnityEngine.Random.Range(0.28f, 0.58f));
            woodProduction = RoundGoods(wheatProduction * UnityEngine.Random.Range(0.34f, 0.72f));
            linenProduction = RoundGoods(wheatProduction * UnityEngine.Random.Range(0.16f, 0.44f));
            olivesProduction = RoundGoods(wheatProduction * UnityEngine.Random.Range(0.22f, 0.56f));
            horsesProduction = RoundGoods(wheatProduction * UnityEngine.Random.Range(0.06f, 0.22f));
            bronzeProduction = RoundGoods(wheatProduction * UnityEngine.Random.Range(0.08f, 0.28f));
            produceProduction = RoundGoods(wheatProduction * UnityEngine.Random.Range(0.48f, 0.86f));

            wheatPercent = RandomProductionPercent(true);
            meatPercent = RandomProductionPercent(false);
            woodPercent = RandomProductionPercent(false);
            linenPercent = RandomProductionPercent(false);
            olivesPercent = RandomProductionPercent(false);
            horsesPercent = RandomProductionPercent(false);
            bronzePercent = RandomProductionPercent(false);
            producePercent = RandomProductionPercent(false);
        }

        private void ResetData()
        {
            population = 0;
            growth = 0f;
            taxes = 0f;
            taxPerCapita = 0f;
            mood = 0f;

            dependant = 0;
            slaves = 0;
            plebs = 0;
            freemen = 0;
            merchants = 0;
            patricians = 0;

            children = 0;
            youth = 0;
            adults = 0;
            seniors = 0;
            elders = 0;

            treasuryBalance = 0f;
            tradeIncome = 0f;
            mintingIncome = 0f;
            tariffsIncome = 0f;
            plunderIncome = 0f;
            totalIncome = 0f;

            armyExpenditure = 0f;
            navyExpenditure = 0f;
            administrationExpenditure = 0f;
            constructionExpenditure = 0f;
            inflationExpenditure = 0f;
            totalExpenditure = 0f;

            wheatProduction = 0f;
            wheatPercent = 0f;
            meatProduction = 0f;
            meatPercent = 0f;
            woodProduction = 0f;
            woodPercent = 0f;
            linenProduction = 0f;
            linenPercent = 0f;
            olivesProduction = 0f;
            olivesPercent = 0f;
            horsesProduction = 0f;
            horsesPercent = 0f;
            bronzeProduction = 0f;
            bronzePercent = 0f;
            produceProduction = 0f;
            producePercent = 0f;
        }

        private static float RandomProductionPercent(bool primaryGood)
        {
            float min = primaryGood ? -3.5f : -8f;
            float max = primaryGood ? 15f : 15f;

            return RoundOne(UnityEngine.Random.Range(min, max));
        }

        private static float RoundMoney(float value)
        {
            return Mathf.Round(value / 100f) * 100f;
        }

        private static float RoundGoods(float value)
        {
            return Mathf.Round(value / 25f) * 25f;
        }

        private static float RoundOne(float value)
        {
            return Mathf.Round(value * 10f) / 10f;
        }
    }


    [Serializable]
    public class RomeCityData
    {
        public Sprite cityIcon;
        public Sprite cityMoodIcon;
        public string cityName;
        public string provinceName;
        public int population; // 50k-1mil
        public int growth; // -5/+5
        public int taxes; // 25k-600k
        public int taxPerCapita;
        public int mood; // 0-100

        public int dependant;
        public int slaves;
        public int plebs;
        public int freemen;
        public int merchants;
        public int patricians;

        public int children; // 0-12
        public int youth; // 13-20
        public int adults; // 21-44
        public int seniors; // 45-64
        public int elders; // 65+


        public bool hasHistoricalData;
        public float[] historicalPopulation;
        public float[] historicalTax;
        public float[] historicalMood;
        public float[] historicalTaxCapita;


        public void GenerateHistoricalData()
        {
            const int dataPointCount = 200;
            int currentIndex = dataPointCount - 1;

            if (hasHistoricalData)
                return;

            historicalPopulation = new float[dataPointCount];
            historicalTax = new float[dataPointCount];
            historicalMood = new float[dataPointCount];
            historicalTaxCapita = new float[dataPointCount];

            float safePopulation = Mathf.Max(1, population);
            float safeTaxCapita = taxPerCapita;

            if (safeTaxCapita <= 0f)
                safeTaxCapita = Mathf.Max(1f, taxes / safePopulation);

            float taxScale = taxes / Mathf.Max(1f, safePopulation * safeTaxCapita);
            taxScale = Mathf.Clamp(taxScale, 0.65f, 1.35f);

            float populationValue = safePopulation;
            float moodValue = Mathf.Clamp(mood, 0, 100);
            float taxCapitaValue = safeTaxCapita;
            float taxEfficiencyValue = taxScale;

            float baseAnnualGrowth = growth * 0.0012f + UnityEngine.Random.Range(-0.001f, 0.001f);

            float growthPhaseA = UnityEngine.Random.Range(0f, 100f);
            float growthPhaseB = UnityEngine.Random.Range(0f, 100f);
            float moodPhaseA = UnityEngine.Random.Range(0f, 100f);
            float moodPhaseB = UnityEngine.Random.Range(0f, 100f);
            float taxPhaseA = UnityEngine.Random.Range(0f, 100f);
            float taxPhaseB = UnityEngine.Random.Range(0f, 100f);

            historicalPopulation[currentIndex] = population;
            historicalTax[currentIndex] = taxes;
            historicalMood[currentIndex] = mood;
            historicalTaxCapita[currentIndex] = safeTaxCapita;

            for (int i = currentIndex - 1; i >= 0; i--)
            {
                int yearsBack = currentIndex - i;

                float slowGrowthCycle = Mathf.Sin(yearsBack * 0.045f + growthPhaseA) * 0.0025f;
                float mediumGrowthCycle = Mathf.Sin(yearsBack * 0.13f + growthPhaseB) * 0.0015f;

                float annualGrowth =
                    baseAnnualGrowth +
                    slowGrowthCycle +
                    mediumGrowthCycle +
                    UnityEngine.Random.Range(-0.0025f, 0.0025f);

                if (UnityEngine.Random.value < 0.018f)
                    annualGrowth += UnityEngine.Random.Range(-0.018f, 0.012f);

                annualGrowth = Mathf.Clamp(annualGrowth, -0.025f, 0.025f);

                populationValue /= 1f + annualGrowth;

                float minOldPopulation = Mathf.Max(250f, safePopulation * 0.08f);
                float maxOldPopulation = Mathf.Max(safePopulation * 1.75f, 3000f);

                populationValue = Mathf.Clamp(populationValue, minOldPopulation, maxOldPopulation);

                float moodTarget =
                    55f +
                    Mathf.Sin(yearsBack * 0.055f + moodPhaseA) * 13f +
                    Mathf.Sin(yearsBack * 0.17f + moodPhaseB) * 5f;

                moodValue = Mathf.Lerp(moodValue, moodTarget, 0.05f);
                moodValue += UnityEngine.Random.Range(-1.6f, 1.6f);

                if (UnityEngine.Random.value < 0.025f)
                    moodValue += UnityEngine.Random.Range(-8f, 6f);

                moodValue = Mathf.Clamp(moodValue, 0f, 100f);

                float taxCapitaTarget =
                    safeTaxCapita +
                    Mathf.Sin(yearsBack * 0.035f + taxPhaseA) * 1.35f +
                    Mathf.Sin(yearsBack * 0.11f + taxPhaseB) * 0.45f +
                    (moodValue - 50f) * 0.012f;

                taxCapitaValue = Mathf.Lerp(taxCapitaValue, taxCapitaTarget, 0.065f);
                taxCapitaValue += UnityEngine.Random.Range(-0.06f, 0.06f);

                if (UnityEngine.Random.value < 0.012f)
                    taxCapitaValue += UnityEngine.Random.Range(-0.35f, 0.55f);

                taxCapitaValue = Mathf.Clamp(taxCapitaValue, 0.5f, 14f);

                float efficiencyTarget =
                    taxScale +
                    Mathf.Sin(yearsBack * 0.06f + taxPhaseB) * 0.08f +
                    (moodValue - 50f) * 0.0012f;

                taxEfficiencyValue = Mathf.Lerp(taxEfficiencyValue, efficiencyTarget, 0.05f);
                taxEfficiencyValue += UnityEngine.Random.Range(-0.004f, 0.004f);
                taxEfficiencyValue = Mathf.Clamp(taxEfficiencyValue, 0.55f, 1.35f);

                float taxValue = populationValue * taxCapitaValue * taxEfficiencyValue;

                historicalPopulation[i] = populationValue;
                historicalTax[i] = taxValue;
                historicalMood[i] = moodValue;
                historicalTaxCapita[i] = taxCapitaValue;
            }

            historicalPopulation[currentIndex] = population;
            historicalTax[currentIndex] = taxes;
            historicalMood[currentIndex] = mood;
            historicalTaxCapita[currentIndex] = safeTaxCapita;

            hasHistoricalData = true;
        }
    }

}
