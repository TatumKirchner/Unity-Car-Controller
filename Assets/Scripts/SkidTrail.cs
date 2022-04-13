using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkidTrail : MonoBehaviour
{
    [SerializeField] private float _persistTime;

    private IEnumerator Start()
    {
        while (true)
        {
            yield return null;

            if (transform.parent == null)
                Destroy(gameObject, _persistTime);
        }
    }
}
