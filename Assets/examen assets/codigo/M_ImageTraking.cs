
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

        [Header("Posicion respecto a la foto (metros)")]
        public Vector3 positionOffset = Vector3.zero;

        [Header("Rotacion respecto a la foto (grados)")]
        public Vector3 rotationOffset = Vector3.zero;
    }

    [Header("AR Image Manager")]
    [SerializeField]
    private ARTrackedImageManager imageManager;

    [Header("Modelos del museo")]
    [SerializeField]
    private List<ImagePrefab> imagePrefabs =
        new List<ImagePrefab>();

    [Header("Tiempo de tolerancia (segundos)")]
    [SerializeField, Min(0f)]
    private float trackingTimeout = 1f;

    private readonly Dictionary<TrackableId, GameObject>
        spawnedObjects =
        new Dictionary<TrackableId, GameObject>();

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
                if (model != null && model.activeInHierarchy)
                    yield return model;
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
        {
            Debug.LogError(
                "M_ImageTracking: falta ARTrackedImageManager."
            );
            return;
        }

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

            bool visible =
                lastTrackingTime.TryGetValue(
                    id, out float lastTime
                ) &&
                Time.time - lastTime <= trackingTimeout;

            if (model.activeSelf != visible)
            {
                model.SetActive(visible);
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

        foreach (var removedImage in args.removed)
        {
            RemoveTrackedImage(removedImage.Key);
        }
    }

    private void UpdateTrackedImage(ARTrackedImage image)
    {
        if (image == null)
            return;

        TrackableId id = image.trackableId;

        ImagePrefab config = FindConfiguration(
            image.referenceImage.name
        );

        if (config == null || config.imagePrefab == null)
            return;

        // Crear el modelo solamente una vez
        // por cada imagen rastreada.
        if (!spawnedObjects.TryGetValue(
            id, out GameObject model
        ) || model == null)
        {
            model = Instantiate(
                config.imagePrefab,
                image.transform,
                false
            );

            model.name = config.imageName + "_AR";

            // Desplazamiento respecto al marcador.
            model.transform.localPosition =
                config.positionOffset;

            model.transform.localRotation =
                Quaternion.Euler(config.rotationOffset);

            // Conservamos la escala del Prefab.
            model.SetActive(false);

            spawnedObjects[id] = model;

            Debug.Log(
                "M_ImageTracking: modelo creado para " +
                config.imageName
            );

            ModelsChanged?.Invoke();
        }

        // El modelo es hijo del ARTrackedImage.
        // Sigue su posicion automaticamente.
        if (image.trackingState == TrackingState.Tracking)
        {
            lastTrackingTime[id] = Time.time;

            if (!model.activeSelf)
            {
                model.SetActive(true);
                ModelsChanged?.Invoke();
            }
        }
    }

    private ImagePrefab FindConfiguration(string imageName)
    {
        foreach (ImagePrefab item in imagePrefabs)
        {
            if (item != null && item.imageName == imageName)
                return item;
        }

        return null;
    }

    private void RemoveTrackedImage(TrackableId id)
    {
        if (spawnedObjects.TryGetValue(
            id, out GameObject model
        ))
        {
            spawnedObjects.Remove(id);

            if (model != null)
                Destroy(model);
        }

        lastTrackingTime.Remove(id);
        ModelsChanged?.Invoke();
    }
}
