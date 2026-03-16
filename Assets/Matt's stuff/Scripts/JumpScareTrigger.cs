using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpScareTrigger : MonoBehaviour
{
    [SerializeField] private JumpScareUI jumpScareUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Explorer"))
        {
            jumpScareUI.Trigger();
            gameObject.SetActive(false);
        }
    }
}
