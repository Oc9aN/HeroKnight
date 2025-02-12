using UnityEngine;

public interface ITarget
{
    public void Damaged(int damage);
    public float Distance(Vector3 from);
    public int GetTargetViewId();
}
