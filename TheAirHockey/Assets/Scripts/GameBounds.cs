using System;
using UnityEngine;

public class GameBounds : MonoBehaviour
{
    public Action<GameObject> outOfBounds;

    private void OnTriggerExit(Collider other)
    {
        outOfBounds?.Invoke(other.gameObject);
    }
}
