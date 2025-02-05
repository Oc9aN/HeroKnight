using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Sensor : MonoBehaviour
{
    [SerializeField] string tagToCheck;
    public UnityAction TriggerEnterAction;
    public UnityAction TriggerExitAction;
    private bool isCollided = false;
    public bool IsCollided { get { return isCollided; } }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagToCheck))
        {
            isCollided = true;
            TriggerEnterAction?.Invoke();
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(tagToCheck))
        {
            isCollided = false;
            TriggerExitAction?.Invoke();
        }
    }
    private void OnDestroy()
    {
        TriggerEnterAction = null;
        TriggerExitAction = null;
    }
}
