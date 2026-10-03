
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ControlRA : MonoBehaviour
{
    // COMPONENTES AR
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;

    // PREFABS
    public GameObject amoungUsPrefab;
    public GameObject finnPrefab;

    // INTERFAZ
    public TMP_Text mensajeEstado;
    public Button botonAmoungUs;
    public Button botonFinn;

    // Tamano del modelo en metros
    public float tamanoModelo = 0.25f;

    // 0 = Among Us
    // 1 = Finn
    private int modeloSeleccionado = 0;

    private List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    private HashSet<TrackableId> detectados =
        new HashSet<TrackableId>();

    // ACTIVAR EL NUEVO SISTEMA DE TOQUES

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        if (planeManager != null)
        {
            planeManager.trackablesChanged.AddListener(
                AlCambiarPlanos);
        }
    }

    void OnDisable()
    {
        if (planeManager != null)
        {
            planeManager.trackablesChanged.RemoveListener(
                AlCambiarPlanos);
        }

        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        mensajeEstado.text = "Escaneando entorno...";
        SeleccionarAmoungUs();
    }

    // SELECCIONAR AMONG US

    public void SeleccionarAmoungUs()
    {
        modeloSeleccionado = 0;

        botonAmoungUs.image.color =
            new Color(1f, 0.95f, 0.75f);

        botonFinn.image.color = Color.white;
    }

    // SELECCIONAR FINN

    public void SeleccionarFinn()
    {
        modeloSeleccionado = 1;

        botonFinn.image.color =
            new Color(0.75f, 0.90f, 1f);

        botonAmoungUs.image.color = Color.white;
    }

    // DETECTAR SUPERFICIES

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
        if (plano.alignment !=
            PlaneAlignment.HorizontalUp &&
            plano.alignment !=
            PlaneAlignment.Vertical)
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

    // COMPROBAR SI SE TOCO UN BOTON

    bool TocoBoton(
        Button boton,
        Vector2 posicion)
    {
        if (boton == null)
            return false;

        Canvas canvas =
            boton.GetComponentInParent<Canvas>();

        Camera camaraUI = null;

        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            camaraUI = canvas.worldCamera;
        }

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                boton.GetComponent<RectTransform>(),
                posicion,
                camaraUI);
    }

    // CREAR UN MODELO SOBRE EL PLANO

    bool CrearModelo(
        GameObject prefab,
        Vector3 posicion,
        Quaternion rotacion,
        Vector3 normal)
    {
        if (prefab == null)
        {
            Debug.LogError("Falta asignar el Prefab.");
            return false;
        }

        // Instanciar el modelo

        GameObject modelo = Instantiate(
            prefab,
            posicion,
            rotacion);

        modelo.SetActive(true);

        // Buscar la geometria visible

        Renderer[] renderizadores =
            modelo.GetComponentsInChildren<Renderer>();

        Bounds limites = new Bounds();

        bool encontrado = false;

        foreach (Renderer r in renderizadores)
        {
            if (!r.enabled)
                continue;

            if (!encontrado)
            {
                limites = r.bounds;
                encontrado = true;
            }
            else
            {
                limites.Encapsulate(r.bounds);
            }
        }

        if (!encontrado)
        {
            Debug.LogError(
                "El Prefab no tiene geometria visible: "
                + prefab.name);

            Destroy(modelo);
            return false;
        }

        // Ajustar el tamano del modelo

        float dimension = Mathf.Max(
            limites.size.x,
            limites.size.y,
            limites.size.z);

        if (dimension > 0.0001f)
        {
            float factor =
                Mathf.Max(0.01f, tamanoModelo)
                / dimension;

            modelo.transform.localScale *= factor;
        }

        // Recalcular los limites

        encontrado = false;

        foreach (Renderer r in renderizadores)
        {
            if (!r.enabled)
                continue;

            if (!encontrado)
            {
                limites = r.bounds;
                encontrado = true;
            }
            else
            {
                limites.Encapsulate(r.bounds);
            }
        }

        if (!encontrado)
            return false;

        normal.Normalize();

        // Calcular la distancia desde el
        // centro del modelo hasta el plano

        float distancia =
            Mathf.Abs(normal.x) * limites.extents.x +
            Mathf.Abs(normal.y) * limites.extents.y +
            Mathf.Abs(normal.z) * limites.extents.z;

        Vector3 centroDeseado =
            posicion +
            normal * (distancia + 0.005f);

        // Evitar que el modelo quede
        // dentro de la superficie

        modelo.transform.position +=
            centroDeseado - limites.center;

        Debug.Log(
            "Modelo creado: " + prefab.name);

        return true;
    }

    // RAYCAST CON EL NUEVO INPUT SYSTEM

    void Update()
    {
        foreach (var toque in
            UnityEngine.InputSystem.EnhancedTouch
            .Touch.activeTouches)
        {
            // Detectar el inicio del toque

            if (!toque.began)
                continue;

            Vector2 posicion =
                toque.screenPosition;

            // BOTON AMONG US

            if (TocoBoton(
                botonAmoungUs,
                posicion))
            {
                SeleccionarAmoungUs();
                return;
            }

            // BOTON FINN

            if (TocoBoton(
                botonFinn,
                posicion))
            {
                SeleccionarFinn();
                return;
            }

            // Comprobar componentes AR

            if (planeManager == null ||
                raycastManager == null)
            {
                Debug.LogError(
                    "Faltan los administradores AR.");

                return;
            }

            // REALIZAR EL RAYCAST

            hits.Clear();

            if (!raycastManager.Raycast(
                posicion,
                hits,
                TrackableType.PlaneWithinPolygon))
            {
                Debug.LogWarning(
                    "El Raycast no encontro un plano.");

                return;
            }

            // RECORRER LOS PLANOS ENCONTRADOS

            foreach (ARRaycastHit hit in hits)
            {
                ARPlane plano =
                    planeManager.GetPlane(
                        hit.trackableId);

                if (plano == null)
                    continue;

                if (plano.subsumedBy != null)
                    continue;

                Pose hitPose = hit.pose;

                // AMONG US EN PLANO HORIZONTAL

                if (modeloSeleccionado == 0 &&
                    plano.alignment ==
                    PlaneAlignment.HorizontalUp)
                {
                    Vector3 arriba =
                        plano.normal.normalized;

                    // Respetar la rotacion
                    // del plano detectado

                    Quaternion rotacion =
                        hitPose.rotation;

                    Quaternion correccion =
                        Quaternion.FromToRotation(
                            rotacion * Vector3.up,
                            arriba);

                    rotacion =
                        correccion * rotacion;

                    bool creado = CrearModelo(
                        amoungUsPrefab,
                        hitPose.position,
                        rotacion,
                        arriba);

                    if (creado)
                    {
                        mensajeEstado.text =
                            "Objeto colocado en plano horizontal";
                    }

                    return;
                }

                // FINN EN PLANO VERTICAL

                if (modeloSeleccionado == 1 &&
                    plano.alignment ==
                    PlaneAlignment.Vertical)
                {
                    Vector3 normal =
                        plano.normal.normalized;

                    // Orientar hacia el lado
                    // de la pared donde esta
                    // la camara

                    if (Camera.main != null)
                    {
                        Vector3 haciaCamara =
                            Camera.main.transform.position -
                            hitPose.position;

                        if (Vector3.Dot(
                            normal,
                            haciaCamara) < 0f)
                        {
                            normal = -normal;
                        }
                    }

                    // Mantener el modelo
                    // derecho y orientado
                    // segun el plano vertical

                    Quaternion rotacion =
                        Quaternion.LookRotation(
                            normal,
                            Vector3.up);

                    bool creado = CrearModelo(
                        finnPrefab,
                        hitPose.position,
                        rotacion,
                        normal);

                    if (creado)
                    {
                        mensajeEstado.text =
                            "Objeto colocado en plano vertical";
                    }

                    return;
                }
            }

            Debug.Log(
                "El modelo seleccionado no corresponde al plano.");

            return;
        }
    }
}
