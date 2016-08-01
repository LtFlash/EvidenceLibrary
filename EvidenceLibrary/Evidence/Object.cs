using Rage;
using EvidenceLibrary.BaseClasses;

namespace EvidenceLibrary.Evidence
{
    public class Object : EvidenceObject
    {
        //PROTECTED
        protected System.Windows.Forms.Keys _keyRotate = System.Windows.Forms.Keys.R;

        public Object(string id, string description, Model model, Vector3 position) :
            base(id, description, model, position)
        {
            TextInteractWithEvidence = $"Press ~y~{KeyInteract} ~s~to examine the object.";
        }

        //protected override void DisplayInfoInteractWithEvidence()
        //{
        //    Game.DisplayHelp(, 100);
        //}

        private enum EStages
        {
            InterpolateCam,
            ManipulateItem,
        }
        private EStages stage = EStages.InterpolateCam;

        protected override void Process()
        {
            switch (stage)
            {
                case EStages.InterpolateCam:

                    Vector3 camPos = new Vector3(Position.X, Position.Y, Position.Z + 0.25f);

                    FocusCamOnObjectWithInterpolation(camPos, _object);
                    Checked = true;
                    stage = EStages.ManipulateItem;

                    break;
                case EStages.ManipulateItem:

                    Game.DisplayHelp($@"Press ~y~{_keyRotate} ~s~to flip the object.~n~Press ~y~{KeyCollect} ~s~to include the item to the evidence.~n~Press ~y~{KeyLeave} ~s~to leave the object.");

                    if (Game.IsKeyDown(_keyRotate))
                    {
                        _object.SetRotationRoll(MathHelper.RotateHeading(_object.Rotation.Roll, 180));
                    }
                    if (Game.IsKeyDown(KeyCollect))
                    {
                        SetEvidenceCollected();

                        InterpolateCameraBack();
                        stage = EStages.InterpolateCam;
                    }
                    if(Game.IsKeyDown(KeyLeave))
                    {
                        SetEvidenceLeft();

                        InterpolateCameraBack();
                        stage = EStages.InterpolateCam;
                    }
                    break;

                default:
                    break;
            }
        }
        
        protected override void DisplayInfoEvidenceCollected()
        {
            Game.DisplayNotification($"Object: {Description} has been included to the evidence.");
        }

        protected override void End()
        {
            if(_object) _object.Delete();
            RemoveBlip();
        }
    }
}
