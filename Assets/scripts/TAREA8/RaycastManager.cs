
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using TMPro;
using UnityEngine.UI;

public class RaycastManager : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private TMP_Text modelName;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image iconImage;

    private ModelInfo selectedModel;

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
        else if (modelName != null &&
                 !string.IsNullOrEmpty(modelName.text))
        {
            ClearInfo();
        }

        foreach (var touch in
            UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            if (!touch.began)
                continue;

            if (arCamera == null)
                continue;

            Ray ray = arCamera.ScreenPointToRay(
                touch.screenPosition
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ModelInfo modelInfo =
                    hit.collider.GetComponentInParent<ModelInfo>();

                if (modelInfo != null && modelInfo.Data != null)
                {
                    selectedModel = modelInfo;
                    ShowInfo(modelInfo.Data);
                }
            }
        }
    }

    private void ShowInfo(ModelData data)
    {
        if (modelName != null)
            modelName.text = "Nombre: " + data.name;

        if (descriptionText != null)
            descriptionText.text = "Descripción: " + data.description;

        if (categoryText != null)
            categoryText.text = "Categoría: " + data.category;

        if (levelText != null)
            levelText.text = "Nivel: " + data.level;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        Debug.Log("Objeto seleccionado: " + data);
    }

    private void ClearInfo()
    {
        selectedModel = null;

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
    }
}
