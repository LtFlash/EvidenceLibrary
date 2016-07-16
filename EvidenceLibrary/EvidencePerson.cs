using Rage;

namespace EvidenceLibrary
{
    public abstract class EvidencePerson : EvidenceBase
    {
        public Ped Ped { get; protected set; }
        protected override Vector3 EvidencePosition
        {
            get
            {
                return (Ped?.Position).GetValueOrDefault(Vector3.Zero);
            }
        }

        public EvidencePerson(string id, string description, SpawnPoint spawn, Model model) : base(id, description)
        {
            Ped = new Ped(model, spawn.Position, spawn.Heading);
            Ped.RandomizeVariation();
            Ped.BlockPermanentEvents = true;
            CreateBlip(Ped, BlipSprite.Enemy, System.Drawing.Color.Green, 0.25f);
        }
    }
}
