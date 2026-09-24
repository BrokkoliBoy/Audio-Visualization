using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Module : MonoBehaviour
{
    [Header("--- Module ---")]
    [SerializeField][TextArea] private string _description;
    public string Description => _description;
    [SerializeField] private bool _setNameDynamically = true;

    protected virtual void OnDrawGizmosSelected()
    {
        if(_setNameDynamically)
            SetGameObjectName(GetName());
    }

    protected virtual string GetName()
    {
        return "Module";
    }

    private void SetGameObjectName(string name)
    {
        gameObject.name = name;
    }

    protected virtual bool IsValid()
    {
        return true;
    }


    #region Override
    protected virtual void Awake()
    {

    }
    protected virtual void Start()
    {

    }
    protected virtual void OnDestroy()
    {

    }
    protected virtual void OnEnable()
    {

    }
    protected virtual void OnDisable()
    {

    }

    // update functions have to be declared in the latest modules so they don't drain performance if not needed

    //protected virtual void Update()
    //{

    //}
    //protected virtual void FixedUpdate()
    //{

    //}
    //protected virtual void LateUpdate()
    //{

    //}
    #endregion
}
