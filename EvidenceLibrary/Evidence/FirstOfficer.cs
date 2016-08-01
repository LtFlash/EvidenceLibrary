using Rage;

namespace EvidenceLibrary.Evidence
{
    public class FirstOfficer : Witness
    {
        public FirstOfficer(string id, string description, SpawnPoint spawn, string[] dialog, string model = "s_m_y_cop_01")
            : base(id, description, spawn, model, dialog, Vector3.Zero)
        {
            TextInteractWithEvidence = $"Press ~y~{KeyInteract}~s~ to talk to the first officer at scene.";
        }

        //protected override void DisplayInfoInteractWithEvidence()
        //{
        //    Game.DisplayHelp(TextInteractWithEvidence, 100);
        //}

        protected override void WaitForFurtherInstruction()
        {
        }
    }
}
