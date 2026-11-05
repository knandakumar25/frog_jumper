/// <summary>A boss-owned return projectile whose lifetime ends when the arena attempt is abandoned.</summary>
public interface ICampaignProjectile
{
    /// <summary>Redirects the existing projectile rather than applying an immediate hitscan counter.</summary>
    void Reflect();
    /// <summary>Disables processing immediately and defers safe deletion.</summary>
    void Cancel();
}
