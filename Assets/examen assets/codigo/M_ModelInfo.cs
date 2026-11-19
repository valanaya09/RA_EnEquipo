
using UnityEngine;

public class M_ModelInfo : MonoBehaviour
{
    [Header("Datos de esta exhibicion")]
    [SerializeField]
    private M_ModelData modelData;

    public M_ModelData Data
    {
        get
        {
            return modelData;
        }
    }
}
