using Rage;
using EvidenceLibrary.BaseClasses;

namespace EvidenceLibrary.Evidence
{
    public class DeadBody : EvidenceBody
    {
        public DeadBody(string id, string description, SpawnPoint spawn, Model model) : base(id, description, spawn, model)
        {

        }

        protected override void DisplayInfoEvidenceCollected()
        {
            Game.DisplayNotification("Body has beed inspected!");
        }
    }
}
