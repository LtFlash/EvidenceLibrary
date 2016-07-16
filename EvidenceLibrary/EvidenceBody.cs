using System;
using Rage;

namespace EvidenceLibrary
{
    public abstract class EvidenceBody : EvidencePerson
    {
        public EvidenceBody(string id, string description, SpawnPoint spawn, Model model) : base(id, description, spawn, model)
        {
            Ped.Kill();
        }

        private enum EState
        {
            InterpolateCam,
            InspectingEvidence,
            InterpolateCamBack,
        }
        private EState _state = EState.InterpolateCam;

        protected override void Process()
        {
        }
    }
}
