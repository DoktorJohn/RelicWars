using UnityEngine;

public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance;

    private void Awake()
    {
        // Singleton mønster: Sikrer der kun er én HUD
        if (Instance == null)
        {
            Instance = this;
            // Dette er nøglen: Objektet slettes ikke ved scene-skift
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}