using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoDemographicsTableController : MonoBehaviour
    {
        public StrategyLedgerDemoController myStrategyGameLedger;

        public RectTransform rowContainer;
        public DemoDemographicsGraphController graphController;
        public DemoDemographicsPieChartController pieChartController;
        public TextMeshProUGUI cityNameLabel;
        
        public TableHeaderButton cityNameHeaderButton;
        public TableHeaderButton ProvinceNameHeaderButton;
        public TableHeaderButton populationHeaderButton;
        public TableHeaderButton growthHeaderButton;
        public TableHeaderButton taxHeaderButton;
        public TableHeaderButton taxCapitaHeaderButton;
        public TableHeaderButton moodHeaderButton;

        public DemoDemographicsProvinceRowView cityRowPrefab;
        public List<DemoDemographicsProvinceRowView> myRowsList = new List<DemoDemographicsProvinceRowView>();

        private bool wasInitialized;

        private List<RomeCityData> internalSortedDataList = new List<RomeCityData>();

        private Dictionary<TableHeaderButton, SortColumn> headerButtonToSortColumn;
        private Dictionary<SortColumn, SortDirection> lastDirectionByColumn = new Dictionary<SortColumn, SortDirection>();

        private SortColumn currentSortColumn = SortColumn.CityName;
        private SortDirection currentSortDirection = SortDirection.Ascending;

        private RomeCityData currentDataToDisplayForGraphOnTheRightSide;

        private enum SortColumn
        {
            CityName,
            ProvinceName,
            Population,
            Growth,
            Taxes,
            TaxPerCapita,
            Mood
        }

        private enum SortDirection
        {
            Ascending,
            Descending
        }

        

        private void InnerInitialization()
        {
            if (wasInitialized)
                return;

            wasInitialized = true;

            headerButtonToSortColumn = new Dictionary<TableHeaderButton, SortColumn>
            {
                { cityNameHeaderButton, SortColumn.CityName },
                { ProvinceNameHeaderButton, SortColumn.ProvinceName },
                { populationHeaderButton, SortColumn.Population },
                { growthHeaderButton, SortColumn.Growth },
                { taxHeaderButton, SortColumn.Taxes },
                { taxCapitaHeaderButton, SortColumn.TaxPerCapita },
                { moodHeaderButton, SortColumn.Mood }
            };

            foreach (var pair in headerButtonToSortColumn)
            {
                pair.Key.OnButtonActivatedClicked += HeaderButtonWasClicked;
            }

            lastDirectionByColumn[SortColumn.CityName] = SortDirection.Ascending;
            lastDirectionByColumn[SortColumn.ProvinceName] = SortDirection.Ascending;
            lastDirectionByColumn[SortColumn.Population] = SortDirection.Descending;
            lastDirectionByColumn[SortColumn.Growth] = SortDirection.Descending;
            lastDirectionByColumn[SortColumn.Taxes] = SortDirection.Descending;
            lastDirectionByColumn[SortColumn.TaxPerCapita] = SortDirection.Descending;
            lastDirectionByColumn[SortColumn.Mood] = SortDirection.Descending;
        }

        private void OnDestroy()
        {
            if (!wasInitialized)
                return;

            foreach (var pair in headerButtonToSortColumn)
            {
                pair.Key.OnButtonActivatedClicked -= HeaderButtonWasClicked;
            }
        }

        private void HeaderButtonWasClicked(
            TableHeaderButton theButton,
            TableHeaderButton.TableHeaderClickSource clickData
        )
        {
            SortColumn clickedColumn = headerButtonToSortColumn[theButton];
            SortBy(clickedColumn, clickData);
        }

        private void SortBy(SortColumn sortColumn, TableHeaderButton.TableHeaderClickSource clickData)
        {
            currentSortColumn = sortColumn;
            currentSortDirection = GetSortDirection(sortColumn, clickData);

            lastDirectionByColumn[sortColumn] = currentSortDirection;

            SortCurrentDataIntoInternalList();
            DisplayRows(internalSortedDataList);
        }
        private SortDirection GetSortDirection(SortColumn sortColumn, TableHeaderButton.TableHeaderClickSource clickData)
        {
            switch (clickData)
            {
                case TableHeaderButton.TableHeaderClickSource.arrowUp:
                    return SortDirection.Ascending;

                case TableHeaderButton.TableHeaderClickSource.arrowDown:
                    return SortDirection.Descending;

                case TableHeaderButton.TableHeaderClickSource.nothingJustButton:
                    if (currentSortColumn == sortColumn)
                        return GetOppositeDirection(currentSortDirection);

                    return lastDirectionByColumn[sortColumn];

                default:
                    return lastDirectionByColumn[sortColumn];
            }
        }

        public void Initialize()
        {
            InnerInitialization();

            SortCurrentDataIntoInternalList();
            DisplayRows(internalSortedDataList);
            currentDataToDisplayForGraphOnTheRightSide = internalSortedDataList[0];
            InitializeGraphAndPieChartForCurrentData();
        }

        private void SortCurrentDataIntoInternalList()
        {
            internalSortedDataList = new List<RomeCityData>(myStrategyGameLedger.myData);
            internalSortedDataList.Sort(CompareRomeCityData);
        }

        private int CompareRomeCityData(RomeCityData a, RomeCityData b)
        {
            int result;

            switch (currentSortColumn)
            {
                case SortColumn.CityName:
                    result = CompareText(a.cityName, b.cityName);
                    break;

                case SortColumn.ProvinceName:
                    result = CompareText(a.provinceName, b.provinceName);
                    break;

                case SortColumn.Population:
                    result = a.population.CompareTo(b.population);
                    break;

                case SortColumn.Growth:
                    result = a.growth.CompareTo(b.growth);
                    break;

                case SortColumn.Taxes:
                    result = a.taxes.CompareTo(b.taxes);
                    break;

                case SortColumn.TaxPerCapita:
                    result = a.taxPerCapita.CompareTo(b.taxPerCapita);
                    break;

                case SortColumn.Mood:
                    result = a.mood.CompareTo(b.mood);
                    break;

                default:
                    result = CompareText(a.cityName, b.cityName);
                    break;
            }

            if (currentSortDirection == SortDirection.Descending)
                result *= -1;

            if (result != 0)
                return result;

            result = CompareText(a.cityName, b.cityName);

            if (result != 0)
                return result;

            return CompareText(a.provinceName, b.provinceName);
        }

        private int CompareText(string a, string b)
        {
            return string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase);
        }

        private void DisplayRows(List<RomeCityData> data)
        {
            while (myRowsList.Count < data.Count)
            {
                DemoDemographicsProvinceRowView newRow = Instantiate(cityRowPrefab, rowContainer);
                myRowsList.Add(newRow);
            }

            for (int i = 0; i < myRowsList.Count; i++)
            {
                bool shouldBeActive = i < data.Count;

                myRowsList[i].gameObject.SetActive(shouldBeActive);

                if (shouldBeActive)
                {
                    myRowsList[i].Initialize(data[i]);
                    myRowsList[i].myHighlightButton.OnButtonActivatedClicked -=DataRowClicked;
                    myRowsList[i].myHighlightButton.OnButtonActivatedClicked +=DataRowClicked;
                }
            }
            
           
        }
        
        private SortDirection GetOppositeDirection(SortDirection direction)
        {
            if (direction == SortDirection.Ascending)
                return SortDirection.Descending;

            return SortDirection.Ascending;
        }

        private void DataRowClicked(RowHighlightButton theButton)
        {
            var parentRow = theButton.transform.parent;
            var rowMonoComponent = parentRow.GetComponent<DemoDemographicsProvinceRowView>();
            var index = myRowsList.IndexOf(rowMonoComponent);
            currentDataToDisplayForGraphOnTheRightSide = internalSortedDataList[index];
            InitializeGraphAndPieChartForCurrentData();
        }

        private void InitializeGraphAndPieChartForCurrentData()
        {
            graphController.InitializeForDemographicsData(currentDataToDisplayForGraphOnTheRightSide);
            pieChartController.InitializeForDemographics(currentDataToDisplayForGraphOnTheRightSide);
            cityNameLabel.SetText(currentDataToDisplayForGraphOnTheRightSide.cityName);
        }
    }
}
