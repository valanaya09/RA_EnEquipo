
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultipleImageTracking : MonoBehaviour
{
    [Serializable]
    public class ImagePrefab
    {
        public string imageName;
        public GameObject imagePrefab;
    }

    [SerializeField] private ARTrackedImageManager imageManager;
    [SerializeField] private List<ImagePrefab> imagePrefabs;

    private readonly Dictionary<TrackableId, GameObject> spawnedObjects =
        new Dictionary<TrackableId, GameObject>();

    private readonly HashSet<TrackableId> imagesWithValidPose =
        new HashSet<TrackableId>();

    private readonly Dictionary<TrackableId, float> lastTrackingTime =
        new Dictionary<TrackableId, float>();

    private const float trackingTimeout = 1.0f;

    public event Action ModelsChanged;

    public IEnumerable<GameObject> ActiveModels
    {
        get
        {
            foreach (var model in spawnedObjects.Values)
            {
                if (model != null && model.activeInHierarchy)
                    yield return model;
            }
        }
    }

    private void Awake()
    {
        if (imageManager == null)
            imageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        if (imageManager == null)
            imageManager = GetComponent<ARTrackedImageManager>();

        if (imageManager == null)
        {
            Debug.LogError("Falta ARTrackedImageManager");
            return;
        }

        imageManager.trackablesChanged.AddListener(OnImagesChanged);

        foreach (var image in imageManager.trackables)
            UpdateImage(image);
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnImagesChanged);

        foreach (var model in spawnedObjects.Values)
        {
            if (model != null)
                model.SetActive(false);
        }

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
                lastTrackingTime.TryGetValue(id, out float lastTime) &&
                Time.time - lastTime <= trackingTimeout;

            if (model.activeSelf != recentlyTracked)
            {
                model.SetActive(recentlyTracked);
                ModelsChanged?.Invoke();
            }
        }
    }

    private void OnImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)
            UpdateImage(image);

        foreach (var image in args.updated)
            UpdateImage(image);

        foreach (var removed in args.removed)
            RemoveImage(removed.Key);
    }

    private void UpdateImage(ARTrackedImage image)
    {
        TrackableId id = image.trackableId;

        if (!spawnedObjects.TryGetValue(id, out GameObject model)
            || model == null)
        {
            if (imagePrefabs == null)
                return;

            foreach (var item in imagePrefabs)
            {
                if (item.imageName != image.referenceImage.name)
                    continue;

                if (item.imagePrefab == null)
                {
                    Debug.LogError("Falta prefab: " + item.imageName);
                    return;
                }

                model = Instantiate(
                    item.imagePrefab,
                    image.transform,
                    false
                );

                model.name = item.imageName;

                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                model.SetActive(false);

                spawnedObjects[id] = model;

                Debug.Log("Imagen identificada: " + item.imageName);

                ModelsChanged?.Invoke();
                break;
            }

            if (model == null)
                return;
        }

        if (image.trackingState == TrackingState.Tracking)
        {
            imagesWithValidPose.Add(id);
            lastTrackingTime[id] = Time.time;

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            if (!model.activeSelf)
            {
                model.SetActive(true);
                ModelsChanged?.Invoke();
            }
        }

        bool visible =
            imagesWithValidPose.Contains(id) &&
            lastTrackingTime.TryGetValue(id, out float lastTime) &&
            Time.time - lastTime <= trackingTimeout;

        if (model.activeSelf != visible)
        {
            model.SetActive(visible);
            ModelsChanged?.Invoke();
        }
    }

    private void RemoveImage(TrackableId id)
    {
        if (!spawnedObjects.TryGetValue(id, out GameObject model))
            return;

        spawnedObjects.Remove(id);
        imagesWithValidPose.Remove(id);
        lastTrackingTime.Remove(id);

        if (model != null)
            Destroy(model);

        ModelsChanged?.Invoke();
    }
}
