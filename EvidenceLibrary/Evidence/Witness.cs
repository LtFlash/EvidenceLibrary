using Rage;
using EvidenceLibrary.BaseClasses;

namespace EvidenceLibrary.Evidence
{
    public class Witness : EvidencePerson
    {
        private Dialog _dialog;
        private Transport _witnessTransport;
        private Vector3 _pickupPos;

        public Witness(string id, string description, SpawnPoint spawn, Model model, string[] dialog, Vector3 pickupPos) : base(id, description, spawn, model)
        {
            _dialog = new Dialog(dialog);
            _pickupPos = pickupPos;
        }

        protected override void DisplayInfoInteractWithEvidence()
        {
            Game.DisplayHelp($"Press ~y~{_keyInteract} ~s~to talk to the witness.", 100);
        } 

        private enum EState
        {
            InitDialog,
            CheckIfDialogFinished,
            WaitForFurtherInstructions, // stay at this stage; player can go away and get back to see the same set of instructions
        }
        private EState _state = EState.InitDialog;

        protected override void Process()
        {
            switch (_state)
            {
                case EState.InitDialog:

                    if (Collected) return;

                    _dialog.StartDialog(Ped, Game.LocalPlayer.Character);
                    _state = EState.CheckIfDialogFinished;

                    break;
                case EState.CheckIfDialogFinished:
                    if(_dialog.HasEnded)
                    {
                        Checked = true;
                        Collected = true;
                        _state = EState.WaitForFurtherInstructions;
                    }
                    break;
                case EState.WaitForFurtherInstructions:

                    WaitForFurtherInstruction(); 

                    break;
                default:
                    break;
            }


            //start dialog -> check for end -> set as collected -> set ped's task to wander?

            //ability to send a victim to a station to make an official statement == 'collect evidence'?
            //it would need a 'transport' class: responding RMP picks up a witness and transport him

            //options:
            // 1. release witness
            // 2. send witness to the station house
            // 3. tell witness to stay on scene == 'checked but not collected' when his testimony
            //    seems unreleated
        }

        protected virtual void WaitForFurtherInstruction()
        {
            if (!CanBeActivated) return;

            Game.DisplayHelp($"Press ~y~{_keyInteract} ~s~to release the witness.~n~Press ~y~{_keyLeave} ~s~to tell the witness to stay at scene.~n~Press ~y~{_keyCollect} ~s~to transport the witness to the station.");

            //release -> done
            //tell to stay -> set Checked to true and set state to WaitFor... in the next contact?

            if (Game.IsKeyDown(_keyInteract))
            {
                SetEvidenceCollected();

                Ped.Tasks.Wander();
                Game.LogVerbose("Witness.Process.ReleaseWitness");
            }
            else if (Game.IsKeyDown(_keyLeave))
            {
                _state = EState.CheckIfDialogFinished; //prevent from reading _keyInteract 2x -> releasing the suspect
                SwapStages(Process, AwayOrClose);
                Game.LogVerbose("Witness.Process.WitnessStay");
            }
            else if (Game.IsKeyDown(_keyCollect))
            {
                //transport
                SetEvidenceCollected();
                _witnessTransport = new Transport(Ped, _pickupPos);
            }
        }

        protected override void DisplayInfoEvidenceCollected()
        {

        }

        protected override void End()
        {
            //if(Ped.Exists()) Ped.Dismiss();
        }
    }
}
