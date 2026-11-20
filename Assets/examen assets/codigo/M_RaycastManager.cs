
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

    [Header("Nombre del modelo")]
    [SerializeField] private TMP_Text modelName;

    [Header("Descripcion")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("Categoria")]
    [SerializeField] private TMP_Text categoryText;

    [Header("Periodo o nivel")]
    [SerializeField] private TMP_Text levelText;

    [Header("Icono")]
    [SerializeField] private Image iconImage;

    [Header("Dato curioso")]
    [SerializeField] private TMP_Text funFactText;

    [Header("Soundtrack")]
    [SerializeField] private AudioSource soundtrackPlayer;

    [Header("Image Tracking")]
    [SerializeField] private M_ImageTracking imageTracking;

    [Header("Botones automaticos")]
    [SerializeField]
    private Vector2 buttonSize =
        new Vector2(200f, 120f);

    [SerializeField]
    private float buttonSpacing = 25f;

    [SerializeField]
    private float buttonBottomMargin = 15f;

    [Header("Diagnostico en pantalla")]
    [SerializeField]
    private bool mostrarDiagnostico = true;

    private string estado = "ESPERANDO TOQUE";

    private M_ModelInfo selectedModel;

    private Button funFactButton;
    private Button closeButton;

    private bool funFactVisible = false;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (imageTracking == null)
        {
            imageTracking =
                FindFirstObjectByType<M_ImageTracking>();
        }

        ConfigurarAudio();
        CrearBotones();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        ClearInfo();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        ClearInfo();
    }

    private void Update()
    {
        if (selectedModel != null)
        {
            if (!selectedModel.gameObject.activeInHierarchy)
                ClearInfo();
        }

        if (arCamera == null)
            arCamera = Camera.main;

        if (imageTracking == null)
        {
            imageTracking =
                FindFirstObjectByType<M_ImageTracking>();
        }

        foreach (var touch in
            UnityEngine.InputSystem.EnhancedTouch
                .Touch.activeTouches)
        {
            if (!touch.began)
                continue;

            if (arCamera == null)
                continue;

            Vector2 posicion = touch.screenPosition;

            estado = "TOQUE RECIBIDO";

            if (TocoBoton(posicion))
                continue;

            if (selectedModel != null &&
                infoPanel != null &&
                infoPanel.activeInHierarchy)
            {
                continue;
            }

            Ray ray = arCamera.ScreenPointToRay(
                posicion
            );

            Physics.SyncTransforms();

            M_ModelInfo modelInfo = null;

            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                Mathf.Infinity,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            System.Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(b.distance)
            );

            foreach (RaycastHit hit in hits)
            {
                modelInfo =
                    hit.collider
                        .GetComponentInParent<M_ModelInfo>();

                if (modelInfo != null &&
                    modelInfo.Data != null)
                {
                    break;
                }

                modelInfo = null;

                if (imageTracking == null)
                    continue;

                foreach (GameObject model
                    in imageTracking.ActiveModels)
                {
                    if (model == null)
                        continue;

                    Transform t =
                        hit.collider.transform;

                    if (t == model.transform ||
                        t.IsChildOf(model.transform))
                    {
                        modelInfo =
                            model.GetComponentInChildren
                                <M_ModelInfo>(true);

                        if (modelInfo != null &&
                            modelInfo.Data != null)
                        {
                            break;
                        }

                        modelInfo = null;
                    }
                }

                if (modelInfo != null)
                    break;
            }

            if (modelInfo == null)
                modelInfo = BuscarModeloVisible(posicion);

            if (modelInfo != null &&
                modelInfo.Data != null)
            {
                selectedModel = modelInfo;

                ShowInfo(modelInfo.Data);

                estado = "FICHA ABIERTA: " +
                    modelInfo.Data.name;

                break;
            }
            else
            {
                estado = "MODELO NO ENCONTRADO";
            }
        }
    }

    private M_ModelInfo BuscarModeloVisible(
        Vector2 posicion
    )
    {
        if (imageTracking == null)
            return null;

        M_ModelInfo mejor = null;
        float menorDistancia = float.MaxValue;
        int cantidad = 0;

        M_ModelInfo unico = null;

        foreach (GameObject model
            in imageTracking.ActiveModels)
        {
            if (model == null)
                continue;

            M_ModelInfo info =
                model.GetComponentInChildren
                    <M_ModelInfo>(true);

            if (info == null || info.Data == null)
                continue;

            cantidad++;
            unico = info;

            Renderer[] renderers =
                model.GetComponentsInChildren<Renderer>();

            bool tieneBounds = false;
            Bounds limites = new Bounds();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!tieneBounds)
                {
                    limites = renderer.bounds;
                    tieneBounds = true;
                }
                else
                {
                    limites.Encapsulate(renderer.bounds);
                }
            }

            if (!tieneBounds)
                continue;

            Vector3 pantalla =
                arCamera.WorldToScreenPoint(
                    limites.center
                );

            if (pantalla.z <= 0f)
                continue;

            float distancia =
                Vector2.Distance(
                    posicion,
                    new Vector2(
                        pantalla.x,
                        pantalla.y
                    )
                );

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                mejor = info;
            }
        }

        if (cantidad == 1)
            return unico;

        if (menorDistancia <= 150f)
            return mejor;

        return null;
    }

    private bool TocoBoton(Vector2 posicion)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointer =
            new PointerEventData(
                EventSystem.current
            );

        pointer.position = posicion;

        List<RaycastResult> resultados =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointer,
            resultados
        );

        foreach (RaycastResult resultado in resultados)
        {
            if (resultado.gameObject == null)
                continue;

            Button boton =
                resultado.gameObject
                    .GetComponentInParent<Button>();

            if (boton != null &&
                boton.IsActive() &&
                boton.IsInteractable())
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

        if (infoPanel != null)
            infoPanel.SetActive(true);

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
                "Nivel: " + data.level;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        funFactVisible = false;

        if (funFactText != null)
        {
            funFactText.gameObject.SetActive(true);
            funFactText.text = "Dato curioso:";
        }

        if (funFactButton != null)
            funFactButton.gameObject.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        PosicionarBotones();

        ReproducirSoundtrack(data.soundtrack);
    }

    private void ConfigurarAudio()
    {
        if (soundtrackPlayer == null)
            return;

        soundtrackPlayer.playOnAwake = false;
        soundtrackPlayer.spatialBlend = 0f;
        soundtrackPlayer.volume = 1f;
        soundtrackPlayer.mute = false;
        soundtrackPlayer.loop = true;
    }

    private void ReproducirSoundtrack(AudioClip clip)
    {
        if (soundtrackPlayer == null)
            return;

        ConfigurarAudio();

        soundtrackPlayer.Stop();
        soundtrackPlayer.clip = clip;

        if (clip != null)
        {
            soundtrackPlayer.Play();
        }
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

        if (funFactVisible)
        {
            funFactText.text =
                "Dato curioso: " +
                selectedModel.Data.funFact;
        }
        else
        {
            funFactText.text = "Dato curioso:";
        }
    }

    public void CloseInfo()
    {
        ClearInfo();
        estado = "FICHA CERRADA";
    }

    private void ClearInfo()
    {
        selectedModel = null;
        funFactVisible = false;

        if (soundtrackPlayer != null)
        {
            soundtrackPlayer.Stop();
            soundtrackPlayer.clip = null;
        }

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

    private void CrearBotones()
    {
        if (infoPanel == null)
            return;

        RectTransform panel =
            infoPanel.GetComponent<RectTransform>();

        if (panel == null)
            return;

        funFactButton = CrearBoton(
            panel,
            "Boton_FunFact",
            "FUN FACT",
            new Color(0.16f, 0.39f, 0.58f)
        );

        closeButton = CrearBoton(
            panel,
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

        PosicionarBotones();
    }

    private Button CrearBoton(
        RectTransform padre,
        string nombre,
        string texto,
        Color color
    )
    {
        Transform existente = padre.Find(nombre);

        GameObject objeto;

        if (existente != null)
        {
            objeto = existente.gameObject;
        }
        else
        {
            objeto = new GameObject(
                nombre,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            objeto.transform.SetParent(
                padre,
                false
            );
        }

        Image imagen =
            objeto.GetComponent<Image>();

        if (imagen == null)
            imagen = objeto.AddComponent<Image>();

        imagen.color = color;
        imagen.raycastTarget = true;

        Button boton =
            objeto.GetComponent<Button>();

        if (boton == null)
            boton = objeto.AddComponent<Button>();

        boton.targetGraphic = imagen;

        RectTransform rect =
            objeto.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0f);

        rect.anchorMax =
            new Vector2(0.5f, 0f);

        rect.pivot =
            new Vector2(0.5f, 0f);

        rect.sizeDelta = buttonSize;

        Transform textoExistente =
            objeto.transform.Find("Texto");

        GameObject textoObjeto;

        if (textoExistente != null)
        {
            textoObjeto = textoExistente.gameObject;
        }
        else
        {
            textoObjeto = new GameObject(
                "Texto",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            textoObjeto.transform.SetParent(
                objeto.transform,
                false
            );
        }

        RectTransform textRect =
            textoObjeto.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label =
            textoObjeto.GetComponent<TextMeshProUGUI>();

        label.text = texto;
        label.fontSize = 22;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        if (modelName != null &&
            modelName.font != null)
        {
            label.font = modelName.font;
        }

        return boton;
    }

    private void PosicionarBotones()
    {
        if (funFactButton == null ||
            closeButton == null)
        {
            return;
        }

        RectTransform funRect =
            funFactButton.GetComponent<RectTransform>();

        RectTransform closeRect =
            closeButton.GetComponent<RectTransform>();

        // Colocarlos juntos en la parte
        // inferior del panel.
        float mitad =
            (buttonSize.x + buttonSpacing) * 0.5f;

        funRect.anchorMin =
            new Vector2(0.5f, 0f);

        funRect.anchorMax =
            new Vector2(0.5f, 0f);

        closeRect.anchorMin =
            new Vector2(0.5f, 0f);

        closeRect.anchorMax =
            new Vector2(0.5f, 0f);

        funRect.pivot =
            new Vector2(0.5f, 0f);

        closeRect.pivot =
            new Vector2(0.5f, 0f);

        funRect.anchoredPosition =
            new Vector2(-mitad, buttonBottomMargin);

        closeRect.anchoredPosition =
            new Vector2(mitad, buttonBottomMargin);

        funRect.sizeDelta = buttonSize;
        closeRect.sizeDelta = buttonSize;

        funRect.SetAsLastSibling();
        closeRect.SetAsLastSibling();
    }

    private void OnGUI()
    {
        if (!mostrarDiagnostico)
            return;

        float ancho =
            Mathf.Min(Screen.width - 24f, 680f);

        float x =
            (Screen.width - ancho) * 0.5f;

        float y =
            Screen.height -
            Screen.safeArea.yMax + 125f;

        GUIStyle estilo =
            new GUIStyle(GUI.skin.box);

        estilo.alignment =
            TextAnchor.MiddleCenter;

        estilo.fontSize =
            Mathf.Clamp(
                Screen.width / 35, 16, 30
            );

        estilo.normal.textColor = Color.white;

        GUI.Box(
            new Rect(x, y, ancho, 85f),
            estado,
            estilo
        );
    }
}
