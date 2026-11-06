
using UnityEngine;

[CreateAssetMenu(fileName = "NewModelData", menuName = "AR/Model Data")]
public class ModelData : ScriptableObject
{
    public new string name;

    [TextArea(3, 5)]
    public string description;

    public string category;
    public int level;
    public Sprite icon;
}
