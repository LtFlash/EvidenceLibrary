﻿using Rage;
using Rage.Native;

namespace EvidenceLibrary
{
    public class MaterialEvidence : EvidenceBase
    {
        //PROTECTED
        protected override Vector3 EvidencePosition
        {
            get
            {
                return (_object?.Position).GetValueOrDefault(Vector3.Zero);
            }
        }

        protected System.Windows.Forms.Keys _keyRotate = System.Windows.Forms.Keys.R;

        //PRIVATE
        private Object _object;
        private Camera _camera;

        public MaterialEvidence(string id, string description, Model model, Vector3 position) : base(id, description)
        {
            _object = new Rage.Object(model, position);

            PlaceObjectOnGround(_object);

            NativeFunction.CallByName<uint>("SET_ENTITY_HAS_GRAVITY", _object, true);
            GameFiber.Sleep(1500);
            _object.IsPositionFrozen = true;

            CreateBlip(_object, BlipSprite.Enemy, System.Drawing.Color.Gray, 0.5f);
            Game.LogVerbose("MaterialEvidence.Constructor");
        }

        private void PlaceObjectOnGround(Rage.Object obj)
        {
            const ulong PLACE_OBJECT_ON_GROUND_PROPERLY = 0x58A850EAEE20FAA3;
            NativeFunction.CallByHash<uint>(PLACE_OBJECT_ON_GROUND_PROPERLY, obj);
        }

        protected override void DisplayInfoInteractWithEvidence()
        {
            Game.DisplayHelp($"Press ~g~{_keyInteract} ~s~to examine the object.", 100);
        }
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

                    FocusCamOnObjectWithInterpolation();

                    stage = EStages.ManipulateItem;

                    break;
                case EStages.ManipulateItem:

                    Game.DisplayHelp($@"Press ~y~{_keyRotate} ~s~to flip the object.~n~Press ~y~{_keyCollect} ~s~to include the item to the evidence.~n~Press ~y~{_keyLeave} ~s~to leave the object.");

                    if (Game.IsKeyDown(_keyRotate))
                    {
                        //TODO: rotate object
                    }
                    if (Game.IsKeyDown(_keyCollect))
                    {
                        SetEvidenceCollected();

                        SetCamBack();
                        stage = EStages.InterpolateCam;
                    }
                    if(Game.IsKeyDown(_keyLeave))
                    {
                        SetEvidenceLeft();

                        SetCamBack();
                        stage = EStages.InterpolateCam;
                    }
                    break;

                default:
                    break;
            }
        }
        //TODO: move to EvidenceBase
        //TODO: params of camPos to use with other type of evidence
        private void FocusCamOnObjectWithInterpolation()
        {
            _camera = new Camera(false);
            Vector3 camPos = new Vector3(EvidencePosition.X, EvidencePosition.Y, EvidencePosition.Z + 0.25f);
            Game.LogVerbose(camPos.ToString());

            _camera.Position = camPos;
            _camera.PointAtEntity(_object, Vector3.Zero, false);

            Camera gameCam = RetrieveGameCam();
            gameCam.Active = true;
            CamInterpolate(gameCam, _camera, 3000, true, true, true);
            _camera.Active = true;
        }

        private void SetCamBack()
        {
            Game.FadeScreenOut(1000);

            if (_camera.Exists())
            {
                _camera.Active = false;
                _camera.Delete();
            }

            Game.FadeScreenIn(1000);
        }

        private Camera RetrieveGameCam()
        {
            Camera gamecan = new Camera(false);
            gamecan.FOV = NativeFunction.Natives.GET_GAMEPLAY_CAM_FOV<float>();
            gamecan.Position = NativeFunction.Natives.GET_GAMEPLAY_CAM_COORD<Vector3>();
            gamecan.Rotation = NativeFunction.Natives.GET_GAMEPLAY_CAM_ROT<Rotator>();

            return gamecan;
        }

        private void CamInterpolate(Camera camfrom, Camera camto, int totaltime, bool easeLocation, bool easeRotation, bool waitForCompletion, float x = 0f, float y = 0f, float z = 0f)
        {
            NativeFunction.Natives.SET_CAM_ACTIVE_WITH_INTERP(camto, camfrom, totaltime, easeLocation, easeRotation);
            if (waitForCompletion)
                GameFiber.Sleep(totaltime);

        }

        protected override void DisplayInfoEvidenceCollected()
        {
            Game.DisplayNotification($"Object: {Description} has been included to the evidence.");
        }

        protected override void End()
        {
            _object?.Delete();
            RemoveBlip();
        }
    }
}
