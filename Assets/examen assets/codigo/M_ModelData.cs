
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewM_ModelData",
    menuName = "M_ModelData"
)]
public class M_ModelData : ScriptableObject
{
    [Header("Informacion del modelo")]

    public new string name;

    [TextArea(3, 6)]
    public string description;

    public string category;

    [Header("Periodo o nivel")]
    public string level;

    [Header("Icono")]
    public Sprite icon;

    [Header("Dato curioso")]

    [TextArea(3, 6)]
    public string funFact;

    [Header("Soundtrack")]
    public AudioClip soundtrack;
}
