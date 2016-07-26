using Rage;

namespace EvidenceLibrary
{
    public struct SpawnPoint
    {
        public float Heading;
        public Vector3 Position;
        public SpawnPoint(float Heading, Vector3 Position)
        {
            this.Heading = Heading;
            this.Position = Position;
        }
        public static SpawnPoint Zero
        {
            get
            {
                return new SpawnPoint(0.0f, Vector3.Zero);
            }
        }
        public float Distance(SpawnPoint spawn)
        {
            return Vector3.Distance(Position, spawn.Position);
        }
    };
}
