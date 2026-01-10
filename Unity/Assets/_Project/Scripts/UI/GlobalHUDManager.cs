using UnityEngine;
using TMPro; // DETTE ER LINJEN DU MANGLER
using Assets.Scripts.Domain.Entities;

public class GlobalHUDManager : MonoBehaviour
{
    public static GlobalHUDManager Instance;

    [Header("Ressource Tekst Referencer")]
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text stoneText;
    [SerializeField] private TMP_Text metalText;

    private void Awake()
    {
        // Singleton logik: Sørger for at denne HUD overlever scene-skift
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Metode til at opdatere teksterne fra andre scripts
    public void UpdateHUD(City city)
    {
        if (city == null) return;

        // Vi tjekker om referencerne er sat i Inspector før vi skriver til dem
        if (woodText != null) woodText.text = $"Træ: {System.Math.Floor(city.Wood)}";
        if (stoneText != null) stoneText.text = $"Sten: {System.Math.Floor(city.Stone)}";
        if (metalText != null) metalText.text = $"Metal: {System.Math.Floor(city.Metal)}";
    }
}