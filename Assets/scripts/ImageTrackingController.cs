
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackingController : MonoBehaviour
{
    public GameObject prefab;

    private ARTrackedImageManager imageManager;

    private Dictionary<TrackableId, GameObject> objetos =
        new Dictionary<TrackableId, GameObject>();

    void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        imageManager.trackablesChanged.AddListener(OnImagesChanged);
    }

    void OnDisable()
    {
        imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
    }

    void OnImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var imagen in args.added)
        {
            GameObject objeto = Instantiate(
                prefab, imagen.transform);

            objeto.transform.localPosition =
                new Vector3(0, 0.05f, 0);

            objeto.transform.localRotation =
                Quaternion.identity;

            objetos.Add(imagen.trackableId, objeto);

            objeto.SetActive(
                imagen.trackingState == TrackingState.Tracking);
        }

        foreach (var imagen in args.updated)
        {
            if (objetos.TryGetValue(
                imagen.trackableId, out GameObject objeto))
            {
                objeto.SetActive(
                    imagen.trackingState == TrackingState.Tracking);
            }
        }

        foreach (var imagen in args.removed)
        {
            if (objetos.TryGetValue(
                imagen.Key, out GameObject objeto))
            {
                Destroy(objeto);
                objetos.Remove(imagen.Key);
            }
        }
    }
}
