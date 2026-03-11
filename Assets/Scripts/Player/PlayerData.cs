namespace BattleBucks.Player
{
    // Pure C# data model — no MonoBehaviour dependency.
    public sealed class PlayerData
    {
        public readonly int   Id;
        public readonly string Name;

        public int   Score     { get; internal set; }
        public bool  IsAlive   { get; internal set; }

        // Absolute game-time when this player may respawn (0 = not pending)
        public float RespawnAt { get; internal set; }

        public PlayerData(int id, string name)
        {
            Id    = id;
            Name  = name;
            Score = 0;
            IsAlive = true;
            RespawnAt = 0f;
        }

        public void Reset()
        {
            Score     = 0;
            IsAlive   = true;
            RespawnAt = 0f;
        }
    }
}
