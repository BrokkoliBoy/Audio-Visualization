using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObjectAfterTime : MonoBehaviour
{
    [SerializeField] private float _timeToDestroy = 5f;
    // Start is called before the first frame update
    //void Start()
    //{
    //    StartCoroutine("DestroyAfterTime");
    //}

    private void Update()
    {
        if (Time.time > _timeToDestroy)
            Destroy(gameObject);
    }

    //private IEnumerable DestroyAfterTime()
    //{
    //    yield return new WaitForSeconds(_timeToDestroy);
    //    Destroy(gameObject);
    //}
}
