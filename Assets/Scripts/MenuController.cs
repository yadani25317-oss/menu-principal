using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controla la navegación entre paneles del menú y los ajustes
/// (nombre, calidad gráfica, brillo, pantalla completa).
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject scoresPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Botones (orden: Jugar, Ajustes, Puntuaciones, Créditos, Salir)")]
    [SerializeField] private Button[] menuButtons;
    [SerializeField] private Button[] backButtons;

    [Header("Ajustes")]
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Opcional")]
    [Tooltip("Imagen negra a pantalla completa; su alfa simula el brillo.")]
    [SerializeField] private Image brightnessOverlay;

    [Header("Escena")]
    [SerializeField] private string gameSceneName = "GameScene";

    private const string KeyName = "PlayerName";
    private const string KeyQuality = "QualityLevel";
    private const string KeyBrightness = "Brightness";
    private const string KeyFullscreen = "Fullscreen";

    private void Start()
    {
        LoadSettings();
        WireButtons();
        WireSettings();
        ShowPanel(mainPanel);
    }

    // ---------- Navegación ----------

    private void WireButtons()
    {
        if (menuButtons != null && menuButtons.Length >= 5)
        {
            menuButtons[0].onClick.AddListener(PlayGame);
            menuButtons[1].onClick.AddListener(() => ShowPanel(settingsPanel));
            menuButtons[2].onClick.AddListener(() => ShowPanel(scoresPanel));
            menuButtons[3].onClick.AddListener(() => ShowPanel(creditsPanel));
            menuButtons[4].onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogWarning("MenuController: asigna los 5 botones en menuButtons.");
        }

        if (backButtons != null)
        {
            foreach (Button b in backButtons)
            {
                if (b != null) b.onClick.AddListener(() => ShowPanel(mainPanel));
            }
        }
    }

    private void ShowPanel(GameObject target)
    {
        mainPanel.SetActive(mainPanel == target);
        settingsPanel.SetActive(settingsPanel == target);
        scoresPanel.SetActive(scoresPanel == target);
        creditsPanel.SetActive(creditsPanel == target);
    }

    private void PlayGame()
    {
        SaveSettings();
        SceneManager.LoadScene(gameSceneName);
    }

    private void QuitGame()
    {
        SaveSettings();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Ajustes ----------

    private void WireSettings()
    {
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        brightnessSlider.onValueChanged.AddListener(SetBrightness);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        playerNameField.onEndEdit.AddListener(_ => SaveSettings());
    }

    private void SetQuality(int index)
    {
        int max = QualitySettings.names.Length - 1;
        QualitySettings.SetQualityLevel(Mathf.Clamp(index, 0, max), true);
    }

    private void SetBrightness(float value)
    {
        // 1 = brillo máximo (overlay transparente), 0 = muy oscuro.
        if (brightnessOverlay != null)
        {
            Color c = brightnessOverlay.color;
            c.a = 1f - value;
            brightnessOverlay.color = c;
        }
    }

    private void SetFullscreen(bool isOn)
    {
        Screen.fullScreen = isOn;
    }

    // ---------- Guardado ----------

    private void LoadSettings()
    {
        playerNameField.text = PlayerPrefs.GetString(KeyName, "");

        int q = PlayerPrefs.GetInt(KeyQuality, 2); // 2 = Alta
        qualityDropdown.SetValueWithoutNotify(q);
        SetQuality(q);

        float b = PlayerPrefs.GetFloat(KeyBrightness, 0.8f);
        brightnessSlider.SetValueWithoutNotify(b);
        SetBrightness(b);

        bool fs = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
        fullscreenToggle.SetIsOnWithoutNotify(fs);
        SetFullscreen(fs);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetString(KeyName, playerNameField.text);
        PlayerPrefs.SetInt(KeyQuality, qualityDropdown.value);
        PlayerPrefs.SetFloat(KeyBrightness, brightnessSlider.value);
        PlayerPrefs.SetInt(KeyFullscreen, fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}
