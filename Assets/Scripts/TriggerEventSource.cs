using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class OnCollisionEvent : UnityEvent<GameObject> { }

public class TriggerEventSource : MonoBehaviour
{
    [SerializeField] private OnCollisionEvent collisionEvent;

    private void OnTriggerEnter(Collider other)
    {
        collisionEvent.Invoke(other.gameObject);
    }
}
