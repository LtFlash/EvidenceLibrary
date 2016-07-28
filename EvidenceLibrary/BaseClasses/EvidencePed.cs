using Rage;

namespace EvidenceLibrary.BaseClasses
{
    public abstract class EvidencePed : EvidenceBase
    {
        public Ped Ped { get; protected set; }

        public override Vector3 Position
        {
            get
            {
                return Ped ? Ped.Position : Vector3.Zero;
            }
        }

        public override PoolHandle Handle
        {
            get
            {
                return Ped ? Ped.Handle : new PoolHandle();
            }
        }

        protected override Entity EvidenceEntity
        {
            get
            {
                return Ped;
            }
        }

        public EvidencePed(string id, string description, SpawnPoint spawn, Model model) : base(id, description)
        {
            Ped = new Ped(model, spawn.Position, spawn.Heading);
            Ped.RandomizeVariation();
            Ped.BlockPermanentEvents = true;
        }

        protected override void End()
        {
            if(Ped) Ped.Dismiss();
        }

        public override void Dismiss()
        {
            Game.LogVerbose("EvidencePerson.Dismiss()");
            End();
            base.Dismiss();
        }

        public override bool IsValid()
        {
            return Ped;
        }
    }
}
