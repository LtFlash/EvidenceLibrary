using System;
using System.Collections.Generic;
using Rage;
using Rage.Native;

namespace EvidenceLibrary.BaseClasses
{
    public abstract class EvidenceBase
    {
        public string Id { get; private set; }
        public string Description { get; private set; }
        public bool Collected { get; protected set; }
        public bool Checked { get; protected set; }
        public Blip Blip { get; protected set; }
        public virtual bool CanBeActivated
        {
            get
            {
                return Vector3.Distance(Game.LocalPlayer.Character.Position, EvidencePosition) <= _distanceEvidenceClose;
            }
        }

        protected abstract Vector3 EvidencePosition { get; } //ped, object etc.
        protected float _distanceEvidenceClose = 3f;

        protected System.Windows.Forms.Keys _keyInteract = System.Windows.Forms.Keys.I;
        protected System.Windows.Forms.Keys _keyCollect = System.Windows.Forms.Keys.C;
        protected System.Windows.Forms.Keys _keyLeave = System.Windows.Forms.Keys.L;

        //PRIVATE
        private GameFiber _process;
        private bool _canRun = true;
        private Camera _camera;
        private Camera _gameCam;

        private List<Stage> _stages = new List<Stage>();
        private class Stage
        {
            public Action Function;
            public bool Active;

            public Stage(Action act, bool active)
            {
                Function = act;
                Active = active;
            }
        }

        public EvidenceBase(string id, string description)
        {
            Id = id;
            Description = description;

            _process = new GameFiber(InternalProcess);
            _process.Start();
            RegisterStages();
        }

        private void RegisterStages()
        {
            AddStage(AwayOrClose);
            AddStage(Process);
            AddStage(InternalEnd);

            ActivateStage(AwayOrClose);
        }

        protected void CreateBlip(Entity attachTo, BlipSprite sprite, System.Drawing.Color color, float scale)
        {
            RemoveBlip();

            Blip = new Blip(attachTo);
            Blip.Sprite = sprite;
            Blip.Color = color;
            Blip.Scale = scale;
        }

        protected void RemoveBlip()
        {
            if (Blip.Exists()) Blip.Delete();
        }

        protected void AddStage(Action stage)
        {
            _stages.Add(new Stage(stage, false));
        }

        protected void ActivateStage(Action stage)
        {
            _stages.Find(a => a.Function == stage).Active = true;
        }

        protected void DeactivateStage(Action stage)
        {
            _stages.Find(a => a.Function == stage).Active = false;
        }

        protected void SwapStages(Action toDisable, Action toEnable)
        {
            DeactivateStage(toDisable);
            ActivateStage(toEnable);
        }

        private void ExecStages()
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].Active) _stages[i].Function();
            }
        }
        //protected - to SwapStage from derived classes
        protected void AwayOrClose()
        {
            if (!CanBeActivated) return;

            DisplayInfoInteractWithEvidence();

            if(Game.IsKeyDown(_keyInteract))
            {
                SwapStages(AwayOrClose, Process);
            }
        }

        protected abstract void DisplayInfoInteractWithEvidence();

        protected abstract void Process();

        protected void SetEvidenceCollected()
        {
            Collected = true;
            DisplayInfoEvidenceCollected();
            SwapStages(Process, InternalEnd);
        }

        protected abstract void DisplayInfoEvidenceCollected();

        protected void SetEvidenceLeft()
        {
            SwapStages(Process, AwayOrClose);
        }

        private void InternalEnd()
        {
            End();

            _canRun = false;
            _process.Abort();
            if (Blip.Exists()) Blip.Delete();
        }

        protected abstract void End();

        private void InternalProcess()
        {
            //is close -> press key -> process -> collect -> that's it
            //is close -> press key -> process -> leave ->

            //abstract CameraMovement()?

            while(_canRun)
            {
                ExecStages();

                GameFiber.Yield();
            }
        }

        protected void FocusCamOnObjectWithInterpolation(Vector3 camPos, Entity pointAt)
        {
            _camera = new Camera(false);

            _camera.Position = camPos;
            _camera.PointAtEntity(pointAt, Vector3.Zero, false);

            _gameCam = RetrieveGameCam();
            _gameCam.Active = true;
            CamInterpolate(_gameCam, _camera, 3000, true, true, true);
            _camera.Active = true;
        }

        protected void InterpolateCameraBack()
        {
            if (_gameCam == null || _camera == null) return;

            CamInterpolate(_camera, _gameCam, 3000, true, true, true);

            _camera.Active = false;
            _camera.Delete();
            _camera = null;
            _gameCam.Delete();
            _gameCam = null;
        }

        protected void DisableCustomCam()
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
            Camera gamecam = new Camera(false);
            gamecam.FOV = NativeFunction.Natives.GET_GAMEPLAY_CAM_FOV<float>();
            gamecam.Position = NativeFunction.Natives.GET_GAMEPLAY_CAM_COORD<Vector3>();
            Vector3 rot = NativeFunction.Natives.GET_GAMEPLAY_CAM_ROT<Vector3>(0);
//doesn't work with Rotator as a return val
            var rot1 = new Rotator(rot.X, rot.Y, rot.Z);
            gamecam.Rotation = rot1;

            gamecam.Heading = NativeFunction.Natives.GetGameplayCamRelativeHeading<float>();

            return gamecam;
        }

        Vector3 RotationToDirection(Vector3 Rotation)
        {
            float rotZ = MathHelper.ConvertDegreesToRadians(Rotation.Z);
            float rotX = MathHelper.ConvertDegreesToRadians(Rotation.X);
            float multXY = Math.Abs((float)Math.Cos(rotX));
            Vector3 res;
            res.X = (float)(-Math.Sin(rotZ)) * multXY;
            res.Y = (float)(Math.Cos(rotZ)) * multXY;
            res.Z = (float)(Math.Sin(rotX));

            return res;
        }

        private void CamInterpolate(Camera camfrom, Camera camto, int totaltime, bool easeLocation, bool easeRotation, bool waitForCompletion, float x = 0f, float y = 0f, float z = 0f)
        {
            NativeFunction.Natives.SET_CAM_ACTIVE_WITH_INTERP(camto, camfrom, totaltime, easeLocation, easeRotation);
            if (waitForCompletion)
                GameFiber.Sleep(totaltime);

        }

        public abstract void Dismiss();
    }
}
