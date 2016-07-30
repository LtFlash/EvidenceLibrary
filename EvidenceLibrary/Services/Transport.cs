using Rage;

namespace EvidenceLibrary.Services
{
    public class Transport
    {
        private Vehicle _policeCar;
        private readonly string[] _policeCarModels = new string[]
        {
            "POLICE",
            "POLICE2",
            "POLICE3",
        };
        private readonly SpawnPoint[] _policeCarInitPositions = new SpawnPoint[]
        {
            new SpawnPoint(230.815689f, new Vector3(399.9877f,-1598.8855f,28.7242641f)),
        };
        private SpawnPoint _policeCarSpawn;

        private Ped _copDriver;
        private readonly string[] _copModels = new string[]
        {
            "s_m_y_cop_01",
        };

        private Ped _ped;
        private Vector3 _positionNearTransportedPed;

        private GameFiber _process;
        private bool _canRun = true;

        public Transport(Ped ped, Vector3 pickupPos, EPoliceStations dispatchFrom = EPoliceStations.Closest)
        {
            _policeCarSpawn = dispatchFrom == EPoliceStations.Closest ? PoliceStations.GetClosestPoliceStationSpawn(pickupPos) : PoliceStations.GetPoliceStationSpawn(dispatchFrom);

            _positionNearTransportedPed = pickupPos;
            _ped = ped;
            _process = new GameFiber(Process);
            _process.Start();
        }

        private enum EState
        {
            CreateEntities,
            SetTaskDrive,
            WaitForArrival,
            SetPedTaskEnter, //if dist > x -> task goto pos,then enter
            WaitForPed,
            DriveAway,
            CheckIfCanBeDisposed,
            Dispose,
        }
        private EState _state = EState.CreateEntities;

        //TODO: cop get out the rmp -> goes to ped, talks to him -> they get back to veh and drive away
        private void Process()
        {
            while (_canRun)
            {
                switch (_state)
                {
                    case EState.CreateEntities:

                        string _policeCarModel = _policeCarModels[MathHelper.GetRandomInteger(_policeCarModels.Length)];

                        _policeCar = new Vehicle(_policeCarModel, _policeCarSpawn.Position);
                        _policeCar.Heading = _policeCarSpawn.Heading;
                        _policeCar.AttachBlip();
                        _policeCar.IsInvincible = true;

                        string _copModel = _copModels[MathHelper.GetRandomInteger(_copModels.Length)];
                        _copDriver = new Ped(_copModel, _policeCar.Position.Around(2f), 0f);
                        _copDriver.WarpIntoVehicle(_policeCar, -1); 
                        _copDriver.KeepTasks = true;
                        _copDriver.BlockPermanentEvents = true;
                        _copDriver.RelationshipGroup = new RelationshipGroup("CIVMALE");

                        _state = EState.SetTaskDrive;

                        break;
                    case EState.SetTaskDrive: 

                        //_positionNearTransportedPed = World.GetNextPositionOnStreet(_ped.Position);
                        _copDriver.Tasks.DriveToPosition(_positionNearTransportedPed, 10f, VehicleDrivingFlags.StopAtDestination | VehicleDrivingFlags.Emergency);

                        _state = EState.WaitForArrival;

                        break;
                    case EState.WaitForArrival:

                        if (_policeCar.Position.DistanceTo(_positionNearTransportedPed) <= 5.5f)
                        {
                            _state = EState.SetPedTaskEnter;
                        }

                        break;
                    case EState.SetPedTaskEnter:

                        Game.LogVerbose("Transport.SetPedTaskEnter.Beginning");
                        _ped.Tasks.ClearImmediately();
                        _ped.RelationshipGroup = _copDriver.RelationshipGroup;

                        _ped.Tasks.GoToOffsetFromEntity(_policeCar, 3f, 90f, 1f);

                        while(true)
                        {
                            if (_ped.Position.DistanceTo(_policeCar.Position) <= 7f) break;

                            GameFiber.Yield();
                        }

                        _ped.Tasks.ClearImmediately();
                        _ped.Tasks.EnterVehicle(_policeCar, 2);
                        Game.LogVerbose("Transport.SetPedTaskEnter.End");

                        _state = EState.WaitForPed;

                        break;
                    case EState.WaitForPed:

                        if (_ped.IsInVehicle(_policeCar, false))
                        {
                            Game.LogVerbose("Transport.WitnessInVehicle");
                            _policeCar.GetAttachedBlip().Delete();
                            _state = EState.DriveAway;
                        }
                         
                        break;
                    case EState.DriveAway: 
                         
                        _copDriver.Tasks.DriveToPosition(_policeCarSpawn.Position, 10f, VehicleDrivingFlags.Normal);
                        
                        _state = EState.CheckIfCanBeDisposed;
                        break;
                         
                    case EState.CheckIfCanBeDisposed:
                         
                        if (_policeCar.Position.DistanceTo(Game.LocalPlayer.Character.Position) >= 200f)
                        {
                            _state = EState.Dispose;
                        }

                        break; 
                         
                    case EState.Dispose:

                        if (_ped.Exists()) _ped.Dismiss(); 
                        if (_copDriver.Exists()) _copDriver.Dismiss();
                        if (_policeCar.Exists()) _policeCar.Dismiss();

                        _canRun = false;
                        _process.Abort();

                        break;

                    default:
                        break;
                }

                GameFiber.Yield(); 
            }
        }
    }
}
