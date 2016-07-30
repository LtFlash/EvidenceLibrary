using System;
using System.Collections.Generic;
using Rage;
using Rage.Native;
using System.Media;

namespace EvidenceLibrary.BaseClasses
{
    public abstract class EvidenceBase : IHandleable
    {
        //PUBLIC
        public string Id { get; private set; }
        public string Description { get; private set; }
        public abstract Vector3 Position { get; }
        public bool Collected
        {
            get
            {
                return _collected;
            }
            protected set
            {
                _collected = value;
                if (IsImportant && PlaySoundImportantEvidenceCollected) _soundImportantEvidenceCollected.Play();
            }
        }
        public bool Checked { get; protected set; }
        public bool IsImportant { get; set; }
        public List<ETraces> Traces { get; } = new List<ETraces>();
        public float DistanceCanBeActivated
        {
            get
            {
                return _distanceEvidenceClose;
            }
            set
            {
                _distanceEvidenceClose = value;
            }
        }
        public virtual bool CanBeActivated //TODO: rename? Close
        {
            get
            {
                return Vector3.Distance(Game.LocalPlayer.Character.Position, Position) <= _distanceEvidenceClose;
            }
        }

        public bool PlaySoundPlayerNearby
        {
            set
            {
                if (value) ActivateStage(PlaySoundEvidenceNearby);
                else DeactivateStage(PlaySoundEvidenceNearby);
            }
        }
        public bool PlaySoundImportantEvidenceCollected { get; set; } = true;

        public SoundPlayer SoundPlayerNearby { set { _soundEvidenceNearby = value; } }
        public SoundPlayer SoundImportantEvidenceCollected { set { _soundImportantEvidenceCollected = value; } }

        public Blip Blip { get; protected set; }

        public System.Windows.Forms.Keys KeyInteract { get; set; } = System.Windows.Forms.Keys.I;
        public System.Windows.Forms.Keys KeyCollect { get; set; } = System.Windows.Forms.Keys.C;
        public System.Windows.Forms.Keys KeyLeave { get; set; } = System.Windows.Forms.Keys.L;

        public abstract PoolHandle Handle { get; }

        //PROTECTED
        protected abstract Entity EvidenceEntity { get; }
        protected float _distanceEvidenceClose = 3f;


        //PRIVATE
        private SoundPlayer _soundEvidenceNearby = new SoundPlayer(Properties.Resources.EvidenceNearby);
        private SoundPlayer _soundImportantEvidenceCollected = new SoundPlayer(Properties.Resources.ImportantEvidenceCollected);

        private bool _collected = false;
        private GameFiber _process;
        private bool _canRun = true;
        private Camera _camera;
        private Camera _gameCam;
        private bool _prevState_CanBeActivated = false; // to play sounds

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

            AddStage(PlaySoundEvidenceNearby);
            ActivateStage(PlaySoundEvidenceNearby);

            ActivateStage(AwayOrClose);
        }
        
        private void PlaySoundEvidenceNearby()
        {
            if(HasStateChanged(ref _prevState_CanBeActivated, CanBeActivated))
            {
                if(CanBeActivated) 
                {
                    _soundEvidenceNearby.Play();
                }
            }
        }

        private bool HasStateChanged(ref bool previous, bool current)
        {
            if (current == previous) return false;
            else
            {
                previous = current;
                return true;
            }
        }

        public void CreateBlip(System.Drawing.Color color, BlipSprite sprite = BlipSprite.Health, float scale = 0.25f)
        {
            RemoveBlip();

            Blip = new Blip(EvidenceEntity);
            Blip.Sprite = sprite;
            Blip.Color = color;
            Blip.Scale = scale;
        }

        public void RemoveBlip()
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

            if(Game.IsKeyDown(KeyInteract))
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
            _soundEvidenceNearby.Dispose();
            _soundImportantEvidenceCollected.Dispose();
        }

        protected abstract void End();

        private void InternalProcess()
        {
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

            SetLocalPlayerPropertiesWhileCamOn(true);
        }

        private void SetLocalPlayerPropertiesWhileCamOn(bool on)
        {
            NativeFunction.Natives.FreezeEntityPosition(Game.LocalPlayer.Character, on);

            Game.LocalPlayer.Character.IsInvincible = on;
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

            SetLocalPlayerPropertiesWhileCamOn(false);
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

            SetLocalPlayerPropertiesWhileCamOn(false);
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

        private void CamInterpolate(Camera camfrom, Camera camto, int totaltime, bool easeLocation, bool easeRotation, bool waitForCompletion, float x = 0f, float y = 0f, float z = 0f)
        {
            NativeFunction.Natives.SET_CAM_ACTIVE_WITH_INTERP(camto, camfrom, totaltime, easeLocation, easeRotation);
            if (waitForCompletion) GameFiber.Sleep(totaltime);
        }

        public virtual void Dismiss()
        {
            Game.LogVerbose("EvidenceBase.Dismiss()");
            RemoveBlip();
            _canRun = false;
            _process.Abort();
        }

        public abstract bool IsValid();

        public bool Equals(IHandleable other)
        {
            return ReferenceEquals(other, this);
        }
    }
}
