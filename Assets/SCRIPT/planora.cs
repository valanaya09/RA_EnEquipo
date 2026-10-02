using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class planora : MonoBehaviour
{
    public Material matHorizontal;
    public Material matVertical;
    public TextMeshPro etiquetaPrefab;

    private ARPlane plano;
    private MeshRenderer renderizador;
    private TextMeshPro etiqueta;

    void Start()
    {
        plano = GetComponent<ARPlane>();
        renderizador = GetComponent<MeshRenderer>();

        etiqueta = Instantiate(
            etiquetaPrefab,
            transform
        );
    }

    void LateUpdate()
    {
        bool horizontal =
            plano.alignment == PlaneAlignment.HorizontalUp ||
            plano.alignment == PlaneAlignment.HorizontalDown;

        bool vertical =
            plano.alignment == PlaneAlignment.Vertical;

        // Asignar el color.
        if (horizontal)
        {
            renderizador.sharedMaterial = matHorizontal;
        }
        else if (vertical)
        {
            renderizador.sharedMaterial = matVertical;
        }

        // Mostrar la etiqueta solo en planos válidos.
        etiqueta.gameObject.SetActive(
            (horizontal || vertical) &&
            plano.subsumedBy == null
        );

        if (!horizontal && !vertical)
            return;

        // Actualizar las medidas en metros.
        etiqueta.text =
            plano.size.x.ToString("F2") + " m x " +
            plano.size.y.ToString("F2") + " m";

        // Situar la etiqueta en el centro del plano.
        etiqueta.transform.position =
            plano.center + plano.normal * 0.02f;

        // Orientar la etiqueta hacia la cámara.
        if (Camera.main != null)
        {
            etiqueta.transform.rotation =
                Camera.main.transform.rotation;
        }
    }
}