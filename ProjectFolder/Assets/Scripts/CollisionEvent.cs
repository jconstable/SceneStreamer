using System;
using UnityEngine;
using UnityEngine.Events;

public class CollisionEvent : MonoBehaviour
{
    public UnityEvent CollisionEnter;
    public UnityEvent CollisionExit;
    [HideInInspector]
    public int Layer;

    public UnityEvent TriggerEnter;
    public UnityEvent TriggerExit;

    void OnCollisionEnter(Collision other)
    {
        if ((other.gameObject.layer & ~Layer) == other.gameObject.layer) CollisionEnter.Invoke();
    }

    void OnCollisionExit(Collision other)
    {
        if ((other.gameObject.layer & ~Layer) == other.gameObject.layer) CollisionExit.Invoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.layer & ~Layer) == other.gameObject.layer) TriggerEnter.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if ((other.gameObject.layer & ~Layer) == other.gameObject.layer) TriggerExit.Invoke();
    }
}
