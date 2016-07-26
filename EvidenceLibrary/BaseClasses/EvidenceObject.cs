using System;
using Rage;
using Rage.Native;

namespace EvidenceLibrary.BaseClasses
{
    public abstract class EvidenceObject : EvidenceBase
    {
        protected Rage.Object _object;

        public override Vector3 EvidencePosition
        {
            get
            {
                return _object ? _object.Position : Vector3.Zero;
            }
        }

        public override PoolHandle Handle
        {
            get
            {
                return _object ? _object.Handle : new PoolHandle();
            }
        }

        protected override Entity EvidenceEntity
        {
            get
            {
                return _object;
            }
        }

        public EvidenceObject(string id, string description, Model model, Vector3 position) : base(id, description)
        {
            _object = new Rage.Object(model, position);

            PlaceObjectOnGround(_object);

            NativeFunction.CallByName<uint>("SET_ENTITY_HAS_GRAVITY", _object, true);
            GameFiber.Sleep(3000);
            _object.IsPositionFrozen = true;
        }

        private void PlaceObjectOnGround(Rage.Object obj)
        {
            const ulong PLACE_OBJECT_ON_GROUND_PROPERLY = 0x58A850EAEE20FAA3;
            NativeFunction.CallByHash<uint>(PLACE_OBJECT_ON_GROUND_PROPERLY, obj);
        }

        public override void Dismiss()
        {
            if(_object) _object.Dismiss();
            base.Dismiss();
        }

        public override bool IsValid()
        {
            return _object;
        }
    }
}
