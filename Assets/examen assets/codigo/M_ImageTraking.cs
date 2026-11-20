
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class M_ImageTracking : MonoBehaviour
{
    [Serializable]
    public class ImagePrefab
    {
        [Header("Imagen registrada")]
        public string imageName;

        [Header("Prefab del modelo")]
        public GameObject imagePrefab;

        [Header("Posicion adicional")]
        public Vector3 positionOffset = Vector3.zero;

        [Header("Rotacion adicional")]
        public Vector3 rotationOffset = Vector3.zero;

        [Header("Multiplicador de tamano")]
        [Min(0.01f)]
        public float multiplicadorTamano = 1f;
    }

    [Header("AR Image Manager")]
    [SerializeField]
    private ARTrackedImageManager imageManager;

    [Header("Modelos del museo")]
    [SerializeField]
    private List<ImagePrefab> imagePrefabs =
        new List<ImagePrefab>();

    [Header("Tiempo de tolerancia")]
    [SerializeField, Min(0f)]
    private float trackingTimeout = 1f;

    [Header("Indicador en pantalla")]
    [SerializeField]
    private bool mostrarIndicador = true;

    [Header("Desplazamiento general")]
    [SerializeField]
    private Vector3 desplazamientoGeneral =
        new Vector3(0f, -0.45f, -0.30f);

    [Header("Rotacion Tierra")]
    [SerializeField]
    private Vector3 rotacionTierra =
        new Vector3(90f, 0f, 0f);

    [Header("Rotacion Cohete")]
    [SerializeField]
    private Vector3 rotacionCohete =
        new Vector3(90f, 0f, 0f);

    [Header("Rotacion Saturno")]
    [SerializeField]
    private Vector3 rotacionSaturno =
        new Vector3(90f, 0f, 0f);

    [Header("Rotacion Satelite")]
    [SerializeField]
    private Vector3 rotacionSatelite =
        new Vector3(90f, 0f, 0f);

    private readonly Dictionary<TrackableId, GameObject>
        spawnedObjects =
        new Dictionary<TrackableId, GameObject>();

    private readonly Dictionary<TrackableId, Transform>
        modelContainers =
        new Dictionary<TrackableId, Transform>();

    private readonly HashSet<TrackableId>
        imagesWithValidPose =
        new HashSet<TrackableId>();

    private readonly Dictionary<TrackableId, float>
        lastTrackingTime =
        new Dictionary<TrackableId, float>();

    public event Action ModelsChanged;

    public IEnumerable<GameObject> ActiveModels
    {
        get
        {
            foreach (GameObject model in spawnedObjects.Values)
            {
                if (model != null &&
                    model.activeInHierarchy)
                {
                    yield return model;
                }
            }
        }
    }

    private void Awake()
    {
        if (imageManager == null)
        {
            imageManager =
                GetComponent<ARTrackedImageManager>();
        }
    }

    private void OnEnable()
    {
        if (imageManager == null)
        {
            imageManager =
                GetComponent<ARTrackedImageManager>();
        }

        if (imageManager == null)
            return;

        imageManager.trackablesChanged.AddListener(
            OnImagesChanged
        );

        foreach (ARTrackedImage image in imageManager.trackables)
        {
            UpdateTrackedImage(image);
        }
    }

    private void OnDisable()
    {
        if (imageManager != null)
        {
            imageManager.trackablesChanged.RemoveListener(
                OnImagesChanged
            );
        }

        foreach (GameObject model in spawnedObjects.Values)
        {
            if (model != null)
                model.SetActive(false);
        }

        imagesWithValidPose.Clear();
        lastTrackingTime.Clear();

        ModelsChanged?.Invoke();
    }

    private void Update()
    {
        foreach (var entry in spawnedObjects)
        {
            TrackableId id = entry.Key;
            GameObject model = entry.Value;

            if (model == null)
                continue;

            bool recentlyTracked =
                imagesWithValidPose.Contains(id) &&
                lastTrackingTime.TryGetValue(
                    id, out float lastTime
                ) &&
                Time.time - lastTime <= trackingTimeout;

            if (model.activeSelf != recentlyTracked)
            {
                model.SetActive(recentlyTracked);

                if (recentlyTracked)
                    IniciarAnimaciones(model);

                ModelsChanged?.Invoke();
            }
        }
    }

    private void OnImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args
    )
    {
        foreach (ARTrackedImage image in args.added)
        {
            UpdateTrackedImage(image);
        }

        foreach (ARTrackedImage image in args.updated)
        {
            UpdateTrackedImage(image);
        }

        foreach (var removed in args.removed)
        {
            RemoveTrackedImage(removed.Key);
        }
    }

    private void UpdateTrackedImage(ARTrackedImage image)
    {
        if (image == null)
            return;

        TrackableId id = image.trackableId;

        string detectedName =
            image.referenceImage.name;

        ImagePrefab config =
            FindConfiguration(detectedName);

        if (config == null ||
            config.imagePrefab == null)
        {
            return;
        }

        if (!spawnedObjects.TryGetValue(
            id, out GameObject model
        ) || model == null)
        {
            GameObject container =
                new GameObject(
                    detectedName + "_AR_Posicion"
                );

            container.transform.SetParent(
                image.transform,
                false
            );

            container.transform.localPosition =
                config.positionOffset +
                desplazamientoGeneral;

            Vector3 rotacion =
                config.rotationOffset;

            if (detectedName == "Tierra")
            {
                rotacion += rotacionTierra;
            }
            else if (detectedName == "Cohete")
            {
                rotacion += rotacionCohete;
            }
            else if (detectedName == "Saturno")
            {
                rotacion += rotacionSaturno;
            }
            else if (detectedName == "Satelite")
            {
                rotacion += rotacionSatelite;
            }

            container.transform.localRotation =
                Quaternion.Euler(rotacion);

            float multiplicador =
                Mathf.Max(
                    0.01f,
                    config.multiplicadorTamano
                );

            container.transform.localScale =
                Vector3.one * multiplicador;

            model = Instantiate(
                config.imagePrefab,
                container.transform,
                false
            );

            model.name =
                detectedName + "_AR";

            model.SetActive(false);

            modelContainers[id] =
                container.transform;

            spawnedObjects[id] = model;

            ModelsChanged?.Invoke();
        }

        if (image.trackingState ==
            TrackingState.Tracking)
        {
            imagesWithValidPose.Add(id);

            lastTrackingTime[id] =
                Time.time;

            if (!model.activeSelf)
            {
                model.SetActive(true);

                IniciarAnimaciones(model);

                ModelsChanged?.Invoke();
            }
        }
    }

    private void IniciarAnimaciones(GameObject model)
    {
        Animator[] animators =
            model.GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            if (animator == null ||
                !animator.enabled ||
                !animator.gameObject.activeInHierarchy ||
                animator.runtimeAnimatorController == null)
            {
                continue;
            }

            animator.Rebind();
            animator.Update(0f);

            foreach (
                AnimatorControllerParameter parametro
                in animator.parameters)
            {
                if (parametro.name == "idle" &&
                    parametro.type ==
                    AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("idle", true);
                    break;
                }
            }
        }
    }

    private ImagePrefab FindConfiguration(
        string imageName
    )
    {
        foreach (ImagePrefab item in imagePrefabs)
        {
            if (item != null &&
                item.imageName == imageName)
            {
                return item;
            }
        }

        return null;
    }

    private void RemoveTrackedImage(TrackableId id)
    {
        spawnedObjects.TryGetValue(
            id, out GameObject model
        );

        spawnedObjects.Remove(id);

        if (modelContainers.TryGetValue(
            id, out Transform container
        ))
        {
            modelContainers.Remove(id);

            if (container != null)
                Destroy(container.gameObject);
        }
        else if (model != null)
        {
            Destroy(model);
        }

        imagesWithValidPose.Remove(id);
        lastTrackingTime.Remove(id);

        ModelsChanged?.Invoke();
    }

    private void OnGUI()
    {
        if (!mostrarIndicador)
            return;

        string mensaje =
            "SIN IMAGEN DETECTADA";

        string detalle =
            "Buscando imagenes...";

        Color color =
            new Color(0.18f, 0.22f, 0.28f);

        if (imageManager != null)
        {
            ARTrackedImage seleccionada = null;

            foreach (ARTrackedImage image
                in imageManager.trackables)
            {
                if (image.trackingState ==
                    TrackingState.Tracking)
                {
                    seleccionada = image;
                    break;
                }
            }

            if (seleccionada != null)
            {
                string nombre =
                    seleccionada.referenceImage.name;

                mensaje =
                    "IMAGEN DETECTADA: " +
                    nombre.ToUpper();

                color =
                    new Color(0.12f, 0.48f, 0.32f);

                if (
                    spawnedObjects.TryGetValue(
                        seleccionada.trackableId,
                        out GameObject modelo
                    ) &&
                    modelo != null &&
                    modelo.activeInHierarchy)
                {
                    detalle = "MODELO ACTIVO";
                }
                else
                {
                    detalle = "MODELO NO VISIBLE";
                }
            }
        }

        float ancho =
            Mathf.Min(
                Screen.width - 24f,
                650f
            );

        float alto = 100f;

        float x =
            (Screen.width - ancho) / 2f;

        float y =
            Screen.height -
            Screen.safeArea.yMax + 15f;

        Rect fondo =
            new Rect(x, y, ancho, alto);

        Color anterior = GUI.color;

        GUI.color = color;

        GUI.DrawTexture(
            fondo,
            Texture2D.whiteTexture
        );

        GUI.color = anterior;

        GUIStyle titulo =
            new GUIStyle(GUI.skin.label);

        titulo.alignment =
            TextAnchor.MiddleCenter;

        titulo.fontStyle =
            FontStyle.Bold;

        titulo.fontSize =
            Mathf.Clamp(
                Screen.width / 35,
                16,
                32
            );

        titulo.normal.textColor =
            Color.white;

        GUIStyle subtitulo =
            new GUIStyle(titulo);

        subtitulo.fontStyle =
            FontStyle.Normal;

        subtitulo.fontSize =
            Mathf.Clamp(
                Screen.width / 45,
                13,
                25
            );

        GUI.Label(
            new Rect(
                x + 5,
                y + 12,
                ancho - 10,
                40
            ),
            mensaje,
            titulo
        );

        GUI.Label(
            new Rect(
                x + 5,
                y + 53,
                ancho - 10,
                32
            ),
            detalle,
            subtitulo
        );
    }
}
