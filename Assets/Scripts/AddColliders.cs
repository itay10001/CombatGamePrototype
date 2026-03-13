using UnityEngine;

public class AddColliders : MonoBehaviour
{
    void Start()
    {
        int count = 0;
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.GetComponent<MeshCollider>() == null)
            {
                mf.gameObject.AddComponent<MeshCollider>();
                count++;
            }
        }
        Debug.Log("Added " + count + " colliders");
    }
}
