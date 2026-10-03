
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PruebaRaycastCubo : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public TMP_Text mensajePrueba;

    private List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        Mostrar("Prueba activa. Toca el plano.");
    }

    void Mostrar(string mensaje)
    {
        Debug.Log(mensaje);

        if (mensajePrueba != null)
            mensajePrueba.text = mensaje;
    }

    void Update()
    {
        foreach (var toque in
            UnityEngine.InputSystem.EnhancedTouch
            .Touch.activeTouches)
        {
            if (!toque.began)
                continue;

            Mostrar("TOQUE DETECTADO");

            if (raycastManager == null)
            {
                Mostrar("Falta Raycast Manager");
                return;
            }

            hits.Clear();

            if (!raycastManager.Raycast(
                toque.screenPosition,
                hits,
                TrackableType.PlaneWithinPolygon))
            {
                Mostrar("RAYCAST SIN IMPACTO");
                return;
            }

            GameObject cubo =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            cubo.transform.position =
                hits[0].pose.position +
                Vector3.up * 0.1f;

            cubo.transform.localScale =
                Vector3.one * 0.2f;

            Mostrar("CUBO CREADO");
            return;
        }
    }
}
