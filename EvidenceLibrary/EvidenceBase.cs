using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;

namespace EvidenceLibrary
{
    public abstract class EvidenceBase
    {
        public string Id { get; private set; }
        public string Description { get; private set; }
        public bool Collected { get; private set; }
        public Blip Blip { get; protected set; }
        public virtual bool CanBeActivated
        {
            get
            {
                return Vector3.Distance(Game.LocalPlayer.Character.Position, EvidencePosition) <= _distanceEvidenceClose;
            }
        }

        protected abstract Vector3 EvidencePosition { get; } //ped, object etc.
        protected float _distanceEvidenceClose = 2f;
        protected System.Windows.Forms.Keys _keyInteract = System.Windows.Forms.Keys.I;

        //PRIVATE
        private GameFiber _process;
        private bool _canRun = true;

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
    }
}
