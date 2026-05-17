using UnityEngine;

public class SelectCharacter : MonoBehaviour
{

    public void SelectThisCharacter(GameObject prefab)
    {
        SelectionData.prefabName = prefab.name;
    }
}
