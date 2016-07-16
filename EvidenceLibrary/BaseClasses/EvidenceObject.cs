using Rage;
using Rage.Native;

namespace EvidenceLibrary.BaseClasses
{
    public abstract class EvidenceObject : EvidenceBase
    {
        protected Rage.Object _object;
        protected override Vector3 EvidencePosition
        {
            get
            {
                return (_object?.Position).GetValueOrDefault(Vector3.Zero);
            }
        }

        public EvidenceObject(string id, string description, Model model, Vector3 position) : base(id, description)
        {
            _object = new Rage.Object(model, position);

            PlaceObjectOnGround(_object);

            NativeFunction.CallByName<uint>("SET_ENTITY_HAS_GRAVITY", _object, true);
            GameFiber.Sleep(3000);
            _object.IsPositionFrozen = true;

            CreateBlip(_object, BlipSprite.Enemy, System.Drawing.Color.Gray, 0.5f);
        }

        private void PlaceObjectOnGround(Rage.Object obj)
        {
            const ulong PLACE_OBJECT_ON_GROUND_PROPERLY = 0x58A850EAEE20FAA3;
            NativeFunction.CallByHash<uint>(PLACE_OBJECT_ON_GROUND_PROPERLY, obj);
        }

        public override void Dismiss()
        {
            _object?.Dismiss();
        }
    }
}
