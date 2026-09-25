using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class RaycastController : MonoBehaviour
{

    [SerializeField] ARRaycastManager raycastManager;

    [SerializeField] GameObject horizontalObject;
    [SerializeField] GameObject verticalObject;


    List<ARRaycastHit> hits = new List<ARRaycastHit>();


    void Update()
    {

        if (Input.touchCount == 0)
            return;


        Touch touch = Input.GetTouch(0);


        if (touch.phase == TouchPhase.Began)
        {

            if (raycastManager.Raycast(
                touch.position,
                hits,
                TrackableType.PlaneWithinPolygon))
            {


                Pose hitPose = hits[0].pose;


                ARPlane plane = hits[0]
                    .trackable
                    .GetComponent<ARPlane>();


                if (plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    Instantiate(
                    horizontalObject,
                    hitPose.position,
                    hitPose.rotation);
                }


                if (plane.alignment == PlaneAlignment.Vertical)
                {
                    Instantiate(
                    verticalObject,
                    hitPose.position,
                    hitPose.rotation);
                }

            }

        }

    }
}