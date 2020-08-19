using System;
using UnityEngine;

// Allow the GameObject to follow another transform, constrained by given options
public class FollowTransform : MonoBehaviour
{
    [SerializeField]
    bool m_followPosition = true;
    [SerializeField]
    bool m_followRotation = true;
    [SerializeField]
    bool m_followScale = true;
    [SerializeField]
    Transform m_target = null; 

    // Update is called once per frame
    void Update()
    {
        if (m_followPosition) transform.position = m_target.position;

        if (m_followRotation) transform.rotation = m_target.rotation;

        if (m_followScale) transform.localScale = m_target.localScale;
    }
}
