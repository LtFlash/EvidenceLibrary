using System;
using Rage;
using EvidenceLibrary.BaseClasses;
namespace EvidenceLibrary.BaseClasses
{
    public abstract class EvidenceBody : EvidencePerson
    {
        public EvidenceBody(string id, string description, SpawnPoint spawn, Model model) : base(id, description, spawn, model)
        {
            Ped.Kill();
        }

        protected override void DisplayInfoInteractWithEvidence()
        {
            Game.DisplayHelp($"Press ~y~{_keyInteract}~s~ to inspect the body.", 100);
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
            switch (_state)
            {
                case EState.InterpolateCam:
                    Vector3 camPos = new Vector3(EvidencePosition.X, EvidencePosition.Y, EvidencePosition.Z + 0.75f);
                    FocusCamOnObjectWithInterpolation(camPos, Ped);
                    _state = EState.InspectingEvidence;
                    break;
                case EState.InspectingEvidence:
                    Game.DisplayHelp($"Press ~y~{_keyLeave}~s~ to quit inspecting the body.", 100);
                    if(Game.IsKeyDown(_keyLeave))
                    {
                        _state = EState.InterpolateCamBack;
                    }
                    break;
                case EState.InterpolateCamBack:
                    InterpolateCameraBack();
                    Collected = true;

                    SwapStages(Process, AwayOrClose);
                    _state = EState.InterpolateCam;
                    break;
                default:
                    break;
            }
        }
    }
}
