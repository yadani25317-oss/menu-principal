using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construye automáticamente todo el menú de la actividad (Canvas, 4 paneles,
/// botones, Ajustes, MusicPlayer, MenuController) y crea las escenas
/// MainMenu y GameScene. Uso: Tools > Crear Menú UI.
/// Compatible con Unity 2022.2.23f1 (TextMeshPro 3.0.6 + uGUI 1.0.0).
/// </summary>
public static class MenuBuilder
{
    static readonly Color Bg = Hex("0A3D4A");
    static readonly Color Field = Hex("0F5566");
    static readonly Color Cream = Hex("E8F4F4");
    static readonly Color Gold = Hex("F0C14B");
    static readonly Color Teal = Hex("3ECFCF");
    static readonly Color Gray = Hex("A0A0A0");

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c);
        return c;
    }

    static Sprite Builtin(string n) => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + n);

    [MenuItem("Tools/Crear Menú UI")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Prefabs");
        const string menuPath = "Assets/Scenes/MainMenu.unity";
        const string gamePath = "Assets/Scenes/GameScene.unity";

        // ---------- Escena de juego de prueba ----------
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject gc = MakeCanvas("Canvas");
        TextMeshProUGUI gt = MakeText(gc.transform, "Texto", "Escena de juego", 90, Color.white, TextAlignmentOptions.Center);
        Stretch(gt.rectTransform);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), gamePath);

        // ---------- Escena de menú ----------
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BuildMenuScene();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), menuPath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(menuPath, true),
            new EditorBuildSettingsScene(gamePath, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Menú creado. Pulsa Play en la escena MainMenu.");
    }

    static void BuildMenuScene()
    {
        // EventSystem
        var es = new GameObject("EventSystem", typeof(EventSystem));
        Type inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        es.AddComponent(inputModule ?? typeof(StandaloneInputModule));

        GameObject canvas = MakeCanvas("Canvas");
        Transform c = canvas.transform;

        // ---------- Paneles ----------
        GameObject main = MakePanel("MainPanel", c);
        GameObject settings = MakePanel("SettingsPanel", c);
        GameObject scores = MakePanel("ScoresPanel", c);
        GameObject credits = MakePanel("CreditsPanel", c);

        // ---------- MainPanel ----------
        var vlg = main.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 24;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;

        TextMeshProUGUI mainTitle = MakeText(main.transform, "Titulo", "MENÚ PRINCIPAL", 80, Cream, TextAlignmentOptions.Center);
        mainTitle.rectTransform.sizeDelta = new Vector2(900, 120);

        string[] labels = { "Jugar", "Ajustes", "Puntuaciones", "Créditos", "Salir" };
        string[] icons = { "Knob.psd", "Knob.psd", "Knob.psd", "Knob.psd", "Checkmark.psd" };
        var menuButtons = new Button[5];
        for (int i = 0; i < 5; i++)
            menuButtons[i] = MakeMenuButton(main.transform, labels[i], Builtin(icons[i]));
        PrefabUtility.SaveAsPrefabAsset(menuButtons[0].gameObject, "Assets/Prefabs/MenuButton.prefab");

        // ---------- SettingsPanel ----------
        SetupPanelLayout(settings);
        TextMeshProUGUI st = MakeText(settings.transform, "Titulo", "Ajustes", 60, Cream, TextAlignmentOptions.Center);
        AddLE(st.gameObject, -1, 80);
        Button backSettings = MakeBackButton(settings.transform);

        TMP_InputField nameField = MakeInputField(settings.transform);
        TMP_Dropdown dropdown = MakeDropdown(settings.transform);

        TextMeshProUGUI bl = MakeText(settings.transform, "BrilloLabel", "Brillo", 36, Gold, TextAlignmentOptions.MidlineLeft);
        AddLE(bl.gameObject, 560, 50);
        Slider slider = MakeSlider(settings.transform);
        Toggle toggle = MakeToggle(settings.transform);

        // ---------- ScoresPanel ----------
        SetupPanelLayout(scores);
        TextMeshProUGUI sct = MakeText(scores.transform, "Titulo", "Mejores puntuaciones", 56, Cream, TextAlignmentOptions.Center);
        AddLE(sct.gameObject, -1, 80);
        string[] rows = { "1. JugadorA — 12500", "2. JugadorB — 9800", "3. JugadorC — 7200", "4. JugadorD — 4100" };
        for (int i = 0; i < rows.Length; i++)
        {
            TextMeshProUGUI r = MakeText(scores.transform, "Score" + (i + 1), rows[i], 36, Cream, TextAlignmentOptions.Center);
            AddLE(r.gameObject, -1, 50);
        }
        Button backScores = MakeBackButton(scores.transform);

        // ---------- CreditsPanel ----------
        TextMeshProUGUI cr = MakeText(credits.transform, "Creditos",
            "Menú principal en Unity UI\nEquipo / Asignatura", 56, Cream, TextAlignmentOptions.Center);
        cr.rectTransform.anchorMin = cr.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        cr.rectTransform.sizeDelta = new Vector2(1200, 300);
        Button backCredits = MakeBackButton(credits.transform);

        // ---------- Overlay de brillo (opcional) ----------
        GameObject ov = NewUI("BrightnessOverlay", c);
        Stretch(ov.GetComponent<RectTransform>());
        Image ovImg = ov.AddComponent<Image>();
        ovImg.color = new Color(0, 0, 0, 0.2f);
        ovImg.raycastTarget = false;

        // ---------- MusicPlayer ----------
        var music = new GameObject("MusicPlayer");
        AudioSource src = music.AddComponent<AudioSource>();
        src.loop = true; src.playOnAwake = true;

        // ---------- MenuController ----------
        var ctrlGO = new GameObject("MenuController");
        var ctrl = ctrlGO.AddComponent<MenuController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("mainPanel").objectReferenceValue = main;
        so.FindProperty("settingsPanel").objectReferenceValue = settings;
        so.FindProperty("scoresPanel").objectReferenceValue = scores;
        so.FindProperty("creditsPanel").objectReferenceValue = credits;
        SetArray(so.FindProperty("menuButtons"), menuButtons);
        SetArray(so.FindProperty("backButtons"), new[] { backSettings, backScores, backCredits });
        so.FindProperty("playerNameField").objectReferenceValue = nameField;
        so.FindProperty("qualityDropdown").objectReferenceValue = dropdown;
        so.FindProperty("brightnessSlider").objectReferenceValue = slider;
        so.FindProperty("fullscreenToggle").objectReferenceValue = toggle;
        so.FindProperty("brightnessOverlay").objectReferenceValue = ovImg;
        so.FindProperty("gameSceneName").stringValue = "GameScene";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= Helpers =================

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", path.Substring("Assets/".Length));
    }

    static void SetArray(SerializedProperty p, UnityEngine.Object[] items)
    {
        p.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static LayoutElement AddLE(GameObject go, float w, float h)
    {
        // TryGetComponent evita el "fake null" de GetComponent en el Editor (el operador ?? no lo detecta).
        if (!go.TryGetComponent(out LayoutElement le)) le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth = w;
        if (h >= 0) le.preferredHeight = h;
        return le;
    }

    static GameObject MakeCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject MakePanel(string name, Transform parent)
    {
        GameObject go = NewUI(name, parent);
        Stretch(go.GetComponent<RectTransform>());
        var img = go.AddComponent<Image>();
        img.sprite = null;
        img.color = Bg;
        return go;
    }

    static void SetupPanelLayout(GameObject panel)
    {
        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(100, 100, 70, 70);
        v.spacing = 32;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = false; v.childForceExpandHeight = false;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static void SetButtonColors(Button b)
    {
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Teal;
        cb.pressedColor = Gray;
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.1f;
        b.colors = cb;
        b.transition = Selectable.Transition.ColorTint;
    }

    static Button MakeMenuButton(Transform parent, string label, Sprite iconSprite)
    {
        GameObject go = NewUI(label, parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(580, 95);
        var img = go.AddComponent<Image>();
        img.sprite = Builtin("UISprite.psd");
        img.type = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        SetButtonColors(btn);

        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 14;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        GameObject icon = NewUI("Icon", go.transform);
        var iimg = icon.AddComponent<Image>();
        iimg.sprite = iconSprite; iimg.color = Bg; iimg.raycastTarget = false;
        AddLE(icon, 44, 44);

        TextMeshProUGUI t = MakeText(go.transform, "Text (TMP)", label, 56, Bg, TextAlignmentOptions.Center);
#pragma warning disable 618
        t.enableWordWrapping = false;
#pragma warning restore 618
        return btn;
    }

    static Button MakeBackButton(Transform parent)
    {
        GameObject go = NewUI("BackButton", parent);
        AddLE(go, -1, -1).ignoreLayout = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(65, -65);
        rt.sizeDelta = new Vector2(85, 85);
        var img = go.AddComponent<Image>();
        img.sprite = Builtin("Knob.psd"); img.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        SetButtonColors(btn);

        GameObject icon = NewUI("Icon", go.transform);
        var irt = icon.GetComponent<RectTransform>();
        irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(44, 44);
        irt.localRotation = Quaternion.Euler(0, 0, -90); // flecha hacia la izquierda
        var iimg = icon.AddComponent<Image>();
        iimg.sprite = Builtin("DropdownArrow.psd"); iimg.color = Bg; iimg.raycastTarget = false;
        return btn;
    }

    // ----- Controles creados con los menús de Unity (TMP) -----

    static GameObject CreateViaMenu(string menuItem, Transform parent)
    {
        Selection.activeTransform = parent;
        if (!EditorApplication.ExecuteMenuItem(menuItem))
            throw new Exception("No se encontró el menú: " + menuItem +
                ". ¿Importaste TMP Essentials? (Window > TextMeshPro > Import TMP Essential Resources)");
        GameObject go = Selection.activeGameObject;
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go;
    }

    static TMP_InputField MakeInputField(Transform parent)
    {
        GameObject go = CreateViaMenu("GameObject/UI/Input Field - TextMeshPro", parent);
        go.name = "PlayerNameField";
        AddLE(go, 560, 55);
        go.GetComponent<Image>().color = Field;
        var f = go.GetComponent<TMP_InputField>();
        f.contentType = TMP_InputField.ContentType.Standard;
        f.characterLimit = 16;
        var ph = f.placeholder as TextMeshProUGUI;
        ph.text = "Tu nombre…"; ph.fontSize = 32; ph.color = Hex("88AAAA");
        f.textComponent.fontSize = 36; f.textComponent.color = Cream;
        return f;
    }

    static TMP_Dropdown MakeDropdown(Transform parent)
    {
        GameObject go = CreateViaMenu("GameObject/UI/Dropdown - TextMeshPro", parent);
        go.name = "QualityDropdown";
        AddLE(go, 560, 55);
        go.GetComponent<Image>().color = Field;
        var d = go.GetComponent<TMP_Dropdown>();
        d.ClearOptions();
        d.AddOptions(new List<string> { "Baja", "Media", "Alta", "Ultra" });
        d.value = 2;
        d.captionText.fontSize = 34; d.captionText.color = Cream;
        d.itemText.fontSize = 30;
        Transform arrow = go.transform.Find("Arrow");
        if (arrow != null) arrow.GetComponent<Image>().color = Cream;
        d.RefreshShownValue();
        return d;
    }

    static Slider MakeSlider(Transform parent)
    {
        GameObject go = CreateViaMenu("GameObject/UI/Slider", parent);
        go.name = "BrightnessSlider";
        AddLE(go, 560, 48);
        var s = go.GetComponent<Slider>();
        s.minValue = 0; s.maxValue = 1; s.wholeNumbers = false; s.value = 0.8f;

        var bg = go.transform.Find("Background").GetComponent<RectTransform>();
        bg.anchorMin = new Vector2(0, 0.5f); bg.anchorMax = new Vector2(1, 0.5f);
        bg.anchoredPosition = Vector2.zero; bg.sizeDelta = new Vector2(0, 16);
        bg.GetComponent<Image>().color = Color.white;

        s.fillRect.GetComponent<Image>().color = Teal;
        go.transform.Find("Fill Area").GetComponent<RectTransform>().offsetMax = new Vector2(-20, 0);

        var slide = go.transform.Find("Handle Slide Area").GetComponent<RectTransform>();
        slide.offsetMin = new Vector2(20, 0); slide.offsetMax = new Vector2(-20, 0);
        s.handleRect.sizeDelta = new Vector2(40, 0);
        var hi = s.handleRect.GetComponent<Image>();
        hi.color = Color.white; hi.preserveAspect = true;
        return s;
    }

    static Toggle MakeToggle(Transform parent)
    {
        GameObject go = NewUI("FullscreenToggle", parent);
        AddLE(go, 560, 65);
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 20; h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        GameObject bg = NewUI("Background", go.transform);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = Builtin("UISprite.psd"); bgImg.type = Image.Type.Sliced; bgImg.color = Color.white;
        AddLE(bg, 60, 60);

        GameObject ck = NewUI("Checkmark", bg.transform);
        var crt = ck.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(34, 34);
        var ckImg = ck.AddComponent<Image>();
        ckImg.sprite = Builtin("Checkmark.psd"); ckImg.color = Bg;

        TextMeshProUGUI label = MakeText(go.transform, "Label", "Pantalla completa", 36, Gold, TextAlignmentOptions.MidlineLeft);
#pragma warning disable 618
        label.enableWordWrapping = false;
#pragma warning restore 618

        var t = go.AddComponent<Toggle>();
        t.transition = Selectable.Transition.ColorTint;
        t.targetGraphic = bgImg;
        t.graphic = ckImg;
        t.isOn = true;
        return t;
    }
}
