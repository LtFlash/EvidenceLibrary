using Rage;

namespace EvidenceLibrary.Evidence
{
    public class FirstOfficer : Witness
    {
        public FirstOfficer(string id, string description, SpawnPoint spawn, string[] dialog, string model = "s_m_y_cop_01")
            : base(id, description, spawn, model, dialog, Vector3.Zero)
        {
                
        }

        protected override void DisplayInfoInteractWithEvidence()
        {
            Game.DisplayHelp($"Press ~y~{KeyInteract}~s~ to talk to the first officer at scene.", 100);
        }

        protected override void WaitForFurtherInstruction()
        {
        }
    }
}
