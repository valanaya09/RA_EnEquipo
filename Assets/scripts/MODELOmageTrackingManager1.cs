
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TresModelosImageTracking : MonoBehaviour
{
    [Header("Reference Image Library")]
    public XRReferenceImageLibrary imageLibrary;

    [Header("Modelos 3D")]
    public GameObject vicPrefab;
    public GameObject gato2Prefab;
    public GameObject mandarinaPrefab;

    [Header("Raycast")]
    public Camera arCamera;

    [Header("Interfaz")]
    public TMP_Text textoSeleccionado;
    public TMP_Text textoDetectados;

    private ARTrackedImageManager imageManager;

    private Dictionary<TrackableId, GameObject> modelos =
        new Dictionary<TrackableId, GameObject>();

    private Dictionary<TrackableId, string> nombres =
        new Dictionary<TrackableId, string>();

    private TrackableId modeloSeleccionadoId;
    private bool haySeleccion = false;

    private void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();

        if (imageManager != null && imageLibrary != null)
        {
            imageManager.referenceLibrary = imageLibrary;
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        if (imageManager != null)
        {
            imageManager.trackablesChanged.AddListener(OnImagesChanged);
        }
    }

    private void OnDisable()
    {
        if (imageManager != null)
        {
            imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
        }

        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        ActualizarUI();
    }

    private void OnImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)
        {
            string nombre = image.referenceImage.name;
            GameObject prefab = null;

            if (nombre == "Vic")
            {
                prefab = vicPrefab;
            }
            else if (nombre == "gato 2")
            {
                prefab = gato2Prefab;
            }
            else if (nombre == "mandarina")
            {
                prefab = mandarinaPrefab;
            }

            if (prefab == null)
                continue;

            GameObject modelo = Instantiate(
                prefab,
                image.transform
            );

            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;

            modelos[image.trackableId] = modelo;
            nombres[image.trackableId] = nombre;

            modelo.SetActive(
                image.trackingState == TrackingState.Tracking
            );
        }

        foreach (var image in args.updated)
        {
            if (modelos.TryGetValue(
                image.trackableId, out GameObject modelo))
            {
                modelo.SetActive(
                    image.trackingState == TrackingState.Tracking
                );

                if (haySeleccion &&
                    modeloSeleccionadoId == image.trackableId &&
                    !modelo.activeSelf)
                {
                    haySeleccion = false;
                }
            }
        }

        foreach (var removed in args.removed)
        {
            TrackableId id = removed.Key;

            if (modelos.TryGetValue(id, out GameObject modelo))
            {
                Destroy(modelo);
                modelos.Remove(id);
            }

            nombres.Remove(id);

            if (haySeleccion && modeloSeleccionadoId == id)
            {
                haySeleccion = false;
            }
        }

        ActualizarUI();
    }

    private void Update()
    {
        if (arCamera == null)
            return;

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase !=
                UnityEngine.InputSystem.TouchPhase.Began)
                continue;

            Ray ray = arCamera.ScreenPointToRay(
                touch.screenPosition
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                foreach (var modelo in modelos)
                {
                    if (modelo.Value == null ||
                        !modelo.Value.activeInHierarchy)
                        continue;

                    if (hit.transform == modelo.Value.transform ||
                        hit.transform.IsChildOf(modelo.Value.transform))
                    {
                        modeloSeleccionadoId = modelo.Key;
                        haySeleccion = true;

                        ActualizarUI();
                        return;
                    }
                }
            }
        }
    }

    private void ActualizarUI()
    {
        if (textoSeleccionado != null)
        {
            string seleccionado = "Ninguno";

            if (haySeleccion &&
                nombres.TryGetValue(
                    modeloSeleccionadoId, out string nombre))
            {
                seleccionado = nombre;
            }

            textoSeleccionado.text =
                "Modelo seleccionado: " + seleccionado;
        }

        if (textoDetectados != null)
        {
            string leyendas = "Imágenes detectadas:";
            int cantidad = 0;

            foreach (var imagen in nombres)
            {
                if (modelos.TryGetValue(
                    imagen.Key, out GameObject modelo))
                {
                    if (modelo != null && modelo.activeSelf)
                    {
                        leyendas += "\n" + imagen.Value;
                        cantidad++;
                    }
                }
            }

            if (cantidad == 0)
            {
                leyendas += "\nNinguna";
            }

            textoDetectados.text = leyendas;
        }
    }
}
