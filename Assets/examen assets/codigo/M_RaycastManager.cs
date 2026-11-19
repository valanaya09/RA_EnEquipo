
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class M_RaycastManager : MonoBehaviour
{
    [Header("Camara AR")]
    [SerializeField]
    private Camera arCamera;

    [Header("Panel de informacion")]
    [SerializeField]
    private GameObject infoPanel;

    [Header("Nombre del modelo")]
    [SerializeField]
    private TMP_Text modelName;

    [Header("Descripcion")]
    [SerializeField]
    private TMP_Text descriptionText;

    [Header("Categoria")]
    [SerializeField]
    private TMP_Text categoryText;

    [Header("Periodo o nivel")]
    [SerializeField]
    private TMP_Text levelText;

    [Header("Icono")]
    [SerializeField]
    private Image iconImage;

    [Header("Dato curioso")]
    [SerializeField]
    private TMP_Text funFactText;

    [Header("Soundtrack")]
    [SerializeField]
    private AudioSource soundtrackPlayer;

    private M_ModelInfo selectedModel;

    private void Awake()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        ClearInfo();
    }

    private void OnDisable()
    {
        ClearInfo();
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        // Cerrar el panel si el modelo desaparece.
        if (infoPanel != null &&
            infoPanel.activeSelf &&
            selectedModel == null)
        {
            ClearInfo();
        }
        else if (selectedModel != null &&
                 !selectedModel.gameObject.activeInHierarchy)
        {
            ClearInfo();
        }

        if (arCamera == null)
            return;

        foreach (var touch in
            UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            if (!touch.began)
                continue;

            // No seleccionar modelos al tocar botones.
            if (TouchOverUI(touch.screenPosition))
                continue;

            // No abrir otra ficha sobre la actual.
            if (infoPanel != null && infoPanel.activeSelf)
                continue;

            Ray ray = arCamera.ScreenPointToRay(
                touch.screenPosition
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                M_ModelInfo info =
                    hit.collider.GetComponentInParent<M_ModelInfo>();

                if (info != null && info.Data != null)
                {
                    selectedModel = info;
                    ShowInfo(info.Data);
                    break;
                }
            }
        }
    }

    private bool TouchOverUI(Vector2 position)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

        pointerData.position = position;

        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointerData,
            results
        );

        return results.Count > 0;
    }

    private void ShowInfo(M_ModelData data)
    {
        if (data == null)
            return;

        if (modelName != null)
            modelName.text = data.name;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (categoryText != null)
            categoryText.text = data.category;

        if (levelText != null)
            levelText.text = data.level;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        if (funFactText != null)
        {
            funFactText.text = data.funFact;
            funFactText.gameObject.SetActive(false);
        }

        if (soundtrackPlayer != null)
        {
            soundtrackPlayer.Stop();

            soundtrackPlayer.clip = data.soundtrack;
            soundtrackPlayer.loop = true;

            if (data.soundtrack != null)
                soundtrackPlayer.Play();
        }

        if (infoPanel != null)
            infoPanel.SetActive(true);

        Debug.Log(
            "Exhibicion seleccionada: " + data.name
        );
    }

    // Funcion del boton FUN FACT.
    public void ShowFunFact()
    {
        if (selectedModel == null || funFactText == null)
            return;

        bool currentlyVisible =
            funFactText.gameObject.activeSelf;

        funFactText.gameObject.SetActive(
            !currentlyVisible
        );
    }

    // Funcion del boton CERRAR.
    public void CloseInfo()
    {
        ClearInfo();
    }

    private void ClearInfo()
    {
        selectedModel = null;

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
        {
            funFactText.text = "";
            funFactText.gameObject.SetActive(false);
        }

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }
}
