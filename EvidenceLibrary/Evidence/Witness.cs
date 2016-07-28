using Rage;
using EvidenceLibrary.BaseClasses;
using LSPD_First_Response.Mod.API;

namespace EvidenceLibrary.Evidence
{
    public class Witness : EvidencePed
    {
        public bool IsArrested { get; protected set; }
        public bool IsCompliant { get; set; }
        public string[] DialogRefuseTransportToStation { get; set; }

        private Dialog _dialog;
        private Services.Transport _witnessTransport;
        private Vector3 _pickupPos;
        private string[] _dialogRefuseBeingTransported = new string[]
        {
            "No way, I'm not going to get involved!",
            "I'd be dead in 12 hours!",
        };

        public Witness(string id, string description, SpawnPoint spawn, Model model, string[] dialog, Vector3 pickupPos) : base(id, description, spawn, model)
        {
            _dialog = new Dialog(dialog);
            _pickupPos = pickupPos;

            DialogRefuseTransportToStation = _dialogRefuseBeingTransported;
        }

        protected override void DisplayInfoInteractWithEvidence()
        {
            Game.DisplayHelp($"Press ~y~{KeyInteract} ~s~to talk to the witness.", 100);
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
            if(!Ped)
            {
                Dismiss();
            }

            if(Functions.IsPedArrested(Ped))
            {
                IsArrested = true;
                Dismiss(); //TODO: test if doesn't 'cancel' the arrest state
            }

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
        }

        protected virtual void WaitForFurtherInstruction()
        {
            if (!CanBeActivated) return;

            Game.DisplayHelp($"Press ~y~{KeyInteract} ~s~to release the witness.~n~Press ~y~{KeyLeave} ~s~to tell the witness to stay at scene.~n~Press ~y~{KeyCollect} ~s~to transport the witness to the station.");

            //release -> done
            //tell to stay -> set Checked to true and set state to WaitFor... in the next contact?

            if (Game.IsKeyDown(KeyInteract))
            {
                SetEvidenceCollected();

                Ped.Tasks.Wander();
            }
            else if (Game.IsKeyDown(KeyLeave))
            {
                _state = EState.CheckIfDialogFinished; //prevent from reading _keyInteract 2x -> releasing the suspect
                SwapStages(Process, AwayOrClose);
            }
            else if (Game.IsKeyDown(KeyCollect))
            {
                if (IsCompliant)
                {
                    SetEvidenceCollected();
                    _witnessTransport = new Services.Transport(Ped, _pickupPos);
                }
                else
                {
                    Dialog refuseBeingTransported = new Dialog(DialogRefuseTransportToStation);
                    refuseBeingTransported.StartDialog();
                    while(!refuseBeingTransported.HasEnded)
                    {
                        GameFiber.Yield();
                    }
                }
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
