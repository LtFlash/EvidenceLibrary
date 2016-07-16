using System;
using Rage;

namespace EvidenceLibrary.BaseClasses
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
        public override PoolHandle Handle
        {
            get
            {
                return (Ped?.Handle).GetValueOrDefault();
            }
        }

        protected override Entity EvidenceEntity
        {
            get
            {
                return Ped;
            }
        }

        public EvidencePerson(string id, string description, SpawnPoint spawn, Model model) : base(id, description)
        {
            Ped = new Ped(model, spawn.Position, spawn.Heading);
            Ped.RandomizeVariation();
            Ped.BlockPermanentEvents = true;
        }

        protected override void End()
        {
            Ped?.Dismiss();
        }

        public override void Dismiss()
        {
            End();
            base.Dismiss();
        }

        public override bool IsValid()
        {
            return Ped != null && Ped.IsValid();
        }
    }
}
