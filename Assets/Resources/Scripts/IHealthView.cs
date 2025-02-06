public interface IHealthView
{
    public int maxHealth { get; set; }
    public void OnHealthChanged(int health);
}
