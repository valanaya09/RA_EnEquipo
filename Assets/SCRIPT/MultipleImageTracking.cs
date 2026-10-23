
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class MultipleImageTracking : MonoBehaviour
{
    [System.Serializable]
    public class ImagePrefab
    {
        public string imageName;
        public GameObject imagePrefab;
    }

    [SerializeField] private ARTrackedImageManager imageManager;
    [SerializeField] private List<ImagePrefab> imagePrefabs;

    private void Awake()
    {
        if (imageManager == null)
        {
            imageManager = GetComponent<ARTrackedImageManager>();
        }
    }

    private void OnEnable()
    {
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
    }

    private void OnImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)
        {
            foreach (var item in imagePrefabs)
            {
                if (image.referenceImage.name == item.imageName &&
                    item.imagePrefab != null)
                {
                    Instantiate(
                        item.imagePrefab,
                        image.transform.position,
                        image.transform.rotation,
                        image.transform
                    );

                    break;
                }
            }
        }
    }
}


