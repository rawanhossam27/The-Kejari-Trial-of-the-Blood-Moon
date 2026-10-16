using System.Collections.Generic;

/// <summary>
/// World pickups that share the E key. Only the closest one in range may collect.
/// </summary>
public interface IArenaPickup
{
    bool InRange { get; }
    float DistanceSqr { get; }
    string Prompt { get; }
}

public static class ArenaPickups
{
    static readonly List<IArenaPickup> all = new List<IArenaPickup>();

    public static void Register(IArenaPickup pickup)
    {
        if (pickup != null && !all.Contains(pickup))
            all.Add(pickup);
    }

    public static void Unregister(IArenaPickup pickup)
    {
        all.Remove(pickup);
    }

    public static bool IsClosest(IArenaPickup self)
    {
        if (self == null || !self.InRange)
            return false;

        float mine = self.DistanceSqr;
        for (int i = 0; i < all.Count; i++)
        {
            var other = all[i];
            if (other == null || ReferenceEquals(other, self) || !other.InRange)
                continue;
            if (other.DistanceSqr < mine)
                return false;
        }

        return true;
    }

    public static IArenaPickup ClosestInRange()
    {
        IArenaPickup best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < all.Count; i++)
        {
            var pickup = all[i];
            if (pickup == null || !pickup.InRange)
                continue;
            if (pickup.DistanceSqr < bestDistance)
            {
                best = pickup;
                bestDistance = pickup.DistanceSqr;
            }
        }

        return best;
    }
}
