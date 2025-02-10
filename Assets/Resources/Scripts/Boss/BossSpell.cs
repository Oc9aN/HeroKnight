using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossSpell : MonoBehaviour
{
    public event UnityAction AttackEndAction;
    private ITarget target;
    private int damage;
    public void Init(Vector2 position, int damage)
    {
        transform.position = position;
        this.damage = damage;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.TryGetComponent<ITarget>(out target);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            target = null;
    }
    private void OnDestroy()
    {
        AttackEndAction = null;
    }
    private void AE_AttackSkill()
    {
        if (target == null) return;
        target.Damaged(damage);
    }
    private void AE_AttackEnd()
    {
        AttackEndAction?.Invoke();
        Destroy(gameObject);
    }
}
