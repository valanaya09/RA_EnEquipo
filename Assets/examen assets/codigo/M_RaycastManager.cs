
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class M_RaycastManager : MonoBehaviour
{
    [Header("Camara AR")]
    [SerializeField] private Camera arCamera;

    [Header("Panel de informacion")]
    [SerializeField] private GameObject infoPanel;

    [Header("Nombre")]
    [SerializeField] private TMP_Text modelName;

    [Header("Descripcion")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("Categoria")]
    [SerializeField] private TMP_Text categoryText;

    [Header("Nivel o periodo")]
    [SerializeField] private TMP_Text levelText;

    [Header("Icono")]
    [SerializeField] private Image iconImage;

    [Header("Dato curioso")]
    [SerializeField] private TMP_Text funFactText;

    [Header("Soundtrack")]
    [SerializeField] private AudioSource soundtrackPlayer;

    [Header("Image Tracking")]
    [SerializeField] private M_ImageTracking imageTracking;

    [Header("Tamano de botones")]
    [SerializeField]
    private Vector2 buttonSize =
        new Vector2(200f, 50f);

    [SerializeField] private float buttonSpacing = 15f;
    [SerializeField] private float buttonTopMargin = 20f;

    private M_ModelInfo selectedModel;
    private bool funFactVisible;

    private Button funFactButton;
    private Button closeButton;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (imageTracking == null)
            imageTracking =
                FindFirstObjectByType<M_ImageTracking>();

        CreateButtons();
        ClearInfo();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        ClearInfo();
    }

    private void OnDisable()
    {
        StopSoundtrack();
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        // Cerrar la ficha si desaparece el modelo.
        if (selectedModel != null &&
            !selectedModel.gameObject.activeInHierarchy)
        {
            ClearInfo();
        }

        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null)
            return;

        foreach (var touch in
            UnityEngine.InputSystem.EnhancedTouch
                .Touch.activeTouches)
        {
            if (!touch.began)
                continue;

            Vector2 position = touch.screenPosition;

            // No atravesar botones de la interfaz.
            if (TouchOverButton(position))
                continue;

            // No cambiar de modelo mientras
            // una ficha esta abierta.
            if (infoPanel != null &&
                infoPanel.activeInHierarchy)
                continue;

            Ray ray = arCamera.ScreenPointToRay(
                position
            );

            // Buscar todos los Colliders alcanzados.
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                Mathf.Infinity,
                ~0,
                QueryTriggerInteraction.Collide
            );

            System.Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(b.distance)
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                // Metodo del codigo original:
                // buscar la informacion en los padres.
                M_ModelInfo modelInfo =
                    hit.collider
                        .GetComponentInParent<M_ModelInfo>();

                // Si no esta en los padres,
                // buscarla en el Prefab AR
                // correspondiente al Collider.
                if (modelInfo == null &&
                    imageTracking != null)
                {
                    foreach (GameObject root
                        in imageTracking.ActiveModels)
                    {
                        if (root == null)
                            continue;

                        Transform colliderTransform =
                            hit.collider.transform;

                        bool belongsToModel =
                            colliderTransform == root.transform ||
                            colliderTransform.IsChildOf(
                                root.transform
                            );

                        if (!belongsToModel)
                            continue;

                        modelInfo =
                            root.GetComponentInChildren
                                <M_ModelInfo>(true);

                        break;
                    }
                }

                if (modelInfo == null ||
                    modelInfo.Data == null ||
                    !modelInfo.gameObject.activeInHierarchy)
                {
                    continue;
                }

                selectedModel = modelInfo;
                ShowInfo(modelInfo.Data);

                return;
            }
        }
    }

    private bool TouchOverButton(Vector2 position)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointer =
            new PointerEventData(
                EventSystem.current
            );

        pointer.position = position;

        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointer,
            results
        );

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
                continue;

            Button button =
                result.gameObject
                    .GetComponentInParent<Button>();

            if (button != null &&
                button.IsActive() &&
                button.IsInteractable())
            {
                return true;
            }
        }

        return false;
    }

    private void ShowInfo(M_ModelData data)
    {
        if (data == null)
            return;

        if (modelName != null)
            modelName.text =
                "Nombre: " + data.name;

        if (descriptionText != null)
            descriptionText.text =
                "Descripción: " + data.description;

        if (categoryText != null)
            categoryText.text =
                "Categoría: " + data.category;

        if (levelText != null)
            levelText.text =
                "Nivel / Periodo: " + data.level;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        funFactVisible = false;

        if (funFactText != null)
        {
            funFactText.text = "Dato curioso:";
            funFactText.gameObject.SetActive(true);
        }

        // Mostrar la ficha solo al seleccionar.
        if (infoPanel != null)
            infoPanel.SetActive(true);

        PositionButtons();
        PlaySoundtrack(data.soundtrack);
    }

    public void ShowFunFact()
    {
        if (selectedModel == null ||
            selectedModel.Data == null ||
            funFactText == null)
        {
            return;
        }

        funFactVisible = !funFactVisible;

        funFactText.text = funFactVisible
            ? "Dato curioso: " +
              selectedModel.Data.funFact
            : "Dato curioso:";
    }

    public void CloseInfo()
    {
        ClearInfo();
    }

    private void ClearInfo()
    {
        selectedModel = null;
        funFactVisible = false;

        StopSoundtrack();

        if (modelName != null)
            modelName.text = "";

        if (descriptionText != null)
            descriptionText.text = "";

        if (categoryText != null)
            categoryText.text = "";

        if (levelText != null)
            levelText.text = "";

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (funFactText != null)
            funFactText.text = "";

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void PlaySoundtrack(AudioClip clip)
    {
        if (soundtrackPlayer == null)
            return;

        soundtrackPlayer.Stop();

        soundtrackPlayer.clip = clip;
        soundtrackPlayer.loop = true;

        if (clip != null)
            soundtrackPlayer.Play();
    }

    private void StopSoundtrack()
    {
        if (soundtrackPlayer == null)
            return;

        soundtrackPlayer.Stop();
        soundtrackPlayer.clip = null;
    }

    private void CreateButtons()
    {
        if (infoPanel == null)
            return;

        RectTransform parent =
            infoPanel.GetComponent<RectTransform>();

        if (parent == null)
            return;

        funFactButton = CreateButton(
            parent,
            "Boton_FunFact",
            "FUN FACT",
            new Color(0.16f, 0.39f, 0.58f)
        );

        closeButton = CreateButton(
            parent,
            "Boton_Cerrar",
            "CERRAR",
            new Color(0.50f, 0.22f, 0.22f)
        );

        funFactButton.onClick.RemoveListener(
            ShowFunFact
        );

        funFactButton.onClick.AddListener(
            ShowFunFact
        );

        closeButton.onClick.RemoveListener(
            CloseInfo
        );

        closeButton.onClick.AddListener(
            CloseInfo
        );

        PositionButtons();
    }

    private Button CreateButton(
        RectTransform parent,
        string objectName,
        string labelText,
        Color color
    )
    {
        Transform existing =
            parent.Find(objectName);

        GameObject obj;

        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            obj.transform.SetParent(
                parent,
                false
            );
        }

        Image background =
            obj.GetComponent<Image>();

        Button button =
            obj.GetComponent<Button>();

        if (background == null)
            background = obj.AddComponent<Image>();

        if (button == null)
            button = obj.AddComponent<Button>();

        background.color = color;
        background.raycastTarget = true;
        button.targetGraphic = background;

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta = buttonSize;

        Transform existingText =
            obj.transform.Find("Texto");

        GameObject textObj;

        if (existingText != null)
        {
            textObj = existingText.gameObject;
        }
        else
        {
            textObj = new GameObject(
                "Texto",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            textObj.transform.SetParent(
                obj.transform,
                false
            );
        }

        RectTransform textRect =
            textObj.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            textObj.GetComponent<TextMeshProUGUI>();

        text.text = labelText;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget = false;

        if (modelName != null &&
            modelName.font != null)
        {
            text.font = modelName.font;
        }

        return button;
    }

    private void PositionButtons()
    {
        if (infoPanel == null ||
            funFactButton == null ||
            closeButton == null)
        {
            return;
        }

        RectTransform panel =
            infoPanel.GetComponent<RectTransform>();

        if (panel == null)
            return;

        Canvas.ForceUpdateCanvases();

        float bottom = float.MaxValue;

        TMP_Text[] fields =
        {
            modelName,
            descriptionText,
            categoryText,
            levelText,
            funFactText
        };

        foreach (TMP_Text field in fields)
        {
            if (field == null)
                continue;

            RectTransform rect =
                field.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local =
                    panel.InverseTransformPoint(
                        corners[i]
                    );

                bottom = Mathf.Min(
                    bottom,
                    local.y
                );
            }
        }

        if (bottom == float.MaxValue)
            bottom = 0f;

        float y =
            bottom -
            buttonTopMargin -
            buttonSize.y * 0.5f;

        float x =
            (buttonSize.x + buttonSpacing) *
            0.5f;

        RectTransform funRect =
            funFactButton
                .GetComponent<RectTransform>();

        RectTransform closeRect =
            closeButton
                .GetComponent<RectTransform>();

        // Como los botones tienen anclas
        // al centro, usamos coordenadas
        // locales del panel.
        funRect.localPosition =
            new Vector3(-x, y, 0f);

        closeRect.localPosition =
            new Vector3(x, y, 0f);

        funRect.sizeDelta = buttonSize;
        closeRect.sizeDelta = buttonSize;
    }
}
