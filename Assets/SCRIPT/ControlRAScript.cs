
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ControlRA : MonoBehaviour
{
    // Componentes de AR Foundation
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;

    // Prefabs de los modelos
    public GameObject amoungUsPrefab;
    public GameObject finnPrefab;

    // Elementos de la interfaz
    public TMP_Text mensajeEstado;
    public Button botonAmoungUs;
    public Button botonFinn;

    // 0 = Amoung Us
    // 1 = Finn
    private int modeloSeleccionado = 0;

    private List<ARRaycastHit> impactos =
        new List<ARRaycastHit>();

    private HashSet<TrackableId> detectados =
        new HashSet<TrackableId>();

    void OnEnable()
    {
        if (planeManager != null)
        {
            planeManager.trackablesChanged.AddListener(
                AlCambiarPlanos
            );
        }
    }

    void OnDisable()
    {
        if (planeManager != null)
        {
            planeManager.trackablesChanged.RemoveListener(
                AlCambiarPlanos
            );
        }
    }

    void Start()
    {
        mensajeEstado.text = "Escaneando entorno...";
        SeleccionarAmoungUs();
    }

    // Seleccionar Amoung Us
    public void SeleccionarAmoungUs()
    {
        modeloSeleccionado = 0;

        botonAmoungUs.image.color =
            new Color(1f, 0.95f, 0.75f);

        botonFinn.image.color = Color.white;
    }

    // Seleccionar Finn
    public void SeleccionarFinn()
    {
        modeloSeleccionado = 1;

        botonFinn.image.color =
            new Color(0.75f, 0.90f, 1f);

        botonAmoungUs.image.color = Color.white;
    }

    // Detectar cambios en los planos
    void AlCambiarPlanos(
        ARTrackablesChangedEventArgs<ARPlane> cambios)
    {
        foreach (ARPlane plano in cambios.added)
        {
            InformarDeteccion(plano);
        }

        foreach (ARPlane plano in cambios.updated)
        {
            InformarDeteccion(plano);
        }
    }

    void InformarDeteccion(ARPlane plano)
    {
        if (plano.alignment != PlaneAlignment.HorizontalUp &&
            plano.alignment != PlaneAlignment.Vertical)
        {
            return;
        }

        if (!detectados.Add(plano.trackableId))
            return;

        if (plano.alignment ==
            PlaneAlignment.HorizontalUp)
        {
            mensajeEstado.text =
                "Superficie horizontal detectada!";
        }
        else
        {
            mensajeEstado.text =
                "Superficie vertical detectada!";
        }
    }

    void Update()
    {
        // Detectar si el usuario toca la pantalla
        if (Input.touchCount == 0)
            return;

        Touch toque = Input.GetTouch(0);

        if (toque.phase != TouchPhase.Began)
            return;

        // Evitar colocar objetos al tocar botones
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(
                toque.fingerId))
        {
            return;
        }

        // Lanzar el Raycast
        if (!raycastManager.Raycast(
            toque.position,
            impactos,
            TrackableType.PlaneWithinPolygon))
        {
            return;
        }

        foreach (ARRaycastHit impacto in impactos)
        {
            ARPlane plano = planeManager.GetPlane(
                impacto.trackableId
            );

            if (plano == null)
                continue;

            if (plano.trackingState != TrackingState.Tracking)
                continue;

            if (plano.subsumedBy != null)
                continue;

            bool horizontal =
                plano.alignment ==
                PlaneAlignment.HorizontalUp;

            bool vertical =
                plano.alignment ==
                PlaneAlignment.Vertical;

            // Colocar Amoung Us en planos horizontales
            if (modeloSeleccionado == 0 && horizontal)
            {
                Vector3 arriba = plano.normal;

                Vector3 frente =
                    Vector3.ProjectOnPlane(
                        plano.transform.forward,
                        arriba
                    );

                if (frente.sqrMagnitude < 0.0001f)
                {
                    frente = Vector3.ProjectOnPlane(
                        Vector3.forward,
                        arriba
                    );
                }

                Quaternion rotacion =
                    Quaternion.LookRotation(
                        frente.normalized,
                        arriba
                    );

                Instantiate(
                    amoungUsPrefab,
                    impacto.pose.position +
                        arriba * 0.005f,
                    rotacion
                );

                mensajeEstado.text =
                    "Objeto colocado en plano horizontal";

                return;
            }

            // Colocar Finn en planos verticales
            if (modeloSeleccionado == 1 && vertical)
            {
                Vector3 normal = plano.normal;

                // Orientar a Finn hacia la cámara
                if (Camera.main != null)
                {
                    Vector3 haciaCamara =
                        Camera.main.transform.position -
                        impacto.pose.position;

                    if (Vector3.Dot(
                        normal,
                        haciaCamara) < 0f)
                    {
                        normal = -normal;
                    }
                }

                Quaternion rotacion =
                    Quaternion.LookRotation(
                        normal,
                        Vector3.up
                    );

                Instantiate(
                    finnPrefab,
                    impacto.pose.position +
                        normal * 0.012f,
                    rotacion
                );

                mensajeEstado.text =
                    "Objeto colocado en plano vertical";

                return;
            }
        }

        mensajeEstado.text =
            "El modelo no corresponde a este plano";
    }
}
