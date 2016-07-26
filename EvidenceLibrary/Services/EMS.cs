using Rage;
using Rage.Native;

namespace EvidenceLibrary.Services
{
    public class EMS
    {
        private Ped _EMTDriver;
        private Ped _EMT;
        private Blip _blipEmt;
        private string[] _paramedicModels = new string[]
        {
            "s_m_m_paramedic_01",
        };

        private Vehicle _ambulance;
        private string[] _ambulanceModels = new string[]
        {
            "AMBULANCE",
        };

        private Ped _patient;
        private SpawnPoint _point;
        private bool _spawnAtScene;
        private bool _takeToHospital;
        private string[] _dialogEMS;

        private SpawnPoint spawn = new SpawnPoint(0, new Vector3());

        private Dialog _dialog;
        private GameFiber _process;
        private bool _canRun = true;
        private System.Windows.Forms.Keys _keyStartDialog = System.Windows.Forms.Keys.Y;

        public EMS(Ped patient, SpawnPoint dispatchTo, string[] dialog,
            bool transportToHospital, bool spawnAtScene = false, EHospitals dispatchFrom = EHospitals.Closest)
        {
            _spawnAtScene = spawnAtScene;
            _patient = patient;
            _point = dispatchTo;
            _takeToHospital = transportToHospital;
            _dialogEMS = dialog;
            spawn = _spawnAtScene ? _point : GetSpawn(dispatchFrom);

            _dialog = new Dialog(_dialogEMS);
            _process = new GameFiber(Process);
            _process.Start();
        }

        private enum EState
        {
            CreateEntities,
            DispatchFromHospital,
            WaitForArrival,
            GoToPatient,
            CheckIfWithPatient,
            Procedures,
            ReadyToReport,
            WaitForDialogActivation,
            WaitDialogToEnd,
            BackToAmbulance,
            CheckIfBackInAmbo,
            CheckIfCanBeDisposed,
        }
        private EState _state = EState.CreateEntities;

        private void Process()
        {
            while (_canRun)
            {
                switch (_state)
                {
                    case EState.CreateEntities:


                        _ambulance = new Vehicle(_ambulanceModels[MathHelper.GetRandomInteger(_ambulanceModels.Length)], spawn.Position);
                        _ambulance.Heading = spawn.Heading;
                        _ambulance.MakePersistent();
                        _ambulance.AttachBlip();
                        _ambulance.IsSirenOn = true;
                        _ambulance.IsInvincible = true;

                        _EMTDriver = new Ped(_paramedicModels[MathHelper.GetRandomInteger(_paramedicModels.Length)], _ambulance.Position.Around2D(5f), 0f);
                        _EMTDriver.RandomizeVariation();
                        _EMTDriver.WarpIntoVehicle(_ambulance, -1);
                        _EMTDriver.BlockPermanentEvents = true;
                        _EMTDriver.KeepTasks = true;

                        _EMT = new Ped(_paramedicModels[MathHelper.GetRandomInteger(_paramedicModels.Length)], _ambulance.Position.Around2D(5f), 0f);
                        _EMT.RandomizeVariation();
                        _EMT.WarpIntoVehicle(_ambulance, 0);
                        _EMT.BlockPermanentEvents = true;
                        _EMT.KeepTasks = true;

                        _state = _spawnAtScene ? EState.GoToPatient : EState.DispatchFromHospital;

                        break;
                    case EState.DispatchFromHospital:

                        _EMTDriver.Tasks.DriveToPosition(_ambulance, _point.Position, 8f, VehicleDrivingFlags.Emergency, 2f);

                        _state = EState.WaitForArrival;

                        break;
                    case EState.WaitForArrival:

                        if(_ambulance.Position.DistanceTo(_point.Position) <= 10f && _ambulance.Speed == 0f)
                        {
                            _ambulance.IsSirenSilent = true;
                            _state = EState.GoToPatient;
                        }

                        break;
                    case EState.GoToPatient:

                        _EMT.Tasks.GoToOffsetFromEntity(_patient, 1f, 0f, 5f);
                        _EMTDriver.Tasks.GoToOffsetFromEntity(_patient, 4f, 8f, 5f);

                        _state = EState.CheckIfWithPatient;
                        break;

                    case EState.CheckIfWithPatient:

                        if (_EMT.Position.DistanceTo(_patient) < 2f && _EMTDriver.Position.DistanceTo(_patient) < 5f)
                        {
                            _state = EState.Procedures;
                        }

                            break;
                    case EState.Procedures:

                        //TODO: make an alternative function using TaskSequence
                        Procedures();

                        if (_takeToHospital) MovePatientToAmbulance(_patient);

                        _blipEmt = new Blip(_EMT);
                        _blipEmt.Color = System.Drawing.Color.Green;
                        _blipEmt.Sprite = BlipSprite.Health;
                        _blipEmt.Scale = 0.25f;

                        Game.DisplayNotification("Talk to EMS to receive a medical report.");
                        _state = EState.WaitForDialogActivation;
                        break;

                    case EState.WaitForDialogActivation:

                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, _EMT.Position) <= 3f)
                        {
                            Game.DisplayHelp($"Press ~y~{_keyStartDialog}~s~ to talk to the paramedic.");

                            if (Game.IsKeyDown(_keyStartDialog))
                            {
                                if (_blipEmt) _blipEmt.Delete();

                                _dialog.StartDialog(_EMT, Game.LocalPlayer.Character);
                                _state = EState.WaitDialogToEnd;
                            }
                        }

                        break;

                    case EState.WaitDialogToEnd:
                        if (_dialog.HasEnded) _state = EState.BackToAmbulance;
                        break;

                    case EState.BackToAmbulance:

                        _EMT.Tasks.GoToOffsetFromEntity(_ambulance, 0.1f, 0f, 1f);
                        _EMTDriver.Tasks.GoToOffsetFromEntity(_ambulance, 0.1f, 0f, 1f);

                        bool emt = true, emtd = true;
                        while (true)
                        {
                            if (emt && Vector3.Distance(_EMT.Position, _ambulance.Position) <= 5f)
                            {
                                _EMT.Tasks.EnterVehicle(_ambulance, 0);
                                emt = false;
                            }

                            if (emtd && Vector3.Distance(_EMTDriver.Position, _ambulance.Position) <= 5f)
                            {
                                _EMTDriver.Tasks.EnterVehicle(_ambulance, -1);
                                emtd = false;
                            }

                            if (!emt && !emtd) break;

                            GameFiber.Yield();
                        }

                        _state = EState.CheckIfBackInAmbo;
                        break;

                    case EState.CheckIfBackInAmbo:

                        if (_EMTDriver.IsInVehicle(_ambulance, false) && _EMT.IsInVehicle(_ambulance, false))
                        {
                            _EMTDriver.Tasks.DriveToPosition(spawn.Position, 10f, VehicleDrivingFlags.Emergency);
                            _ambulance.GetAttachedBlip().Delete();
                            _state = EState.CheckIfCanBeDisposed; 
                        }

                        break;

                    case EState.CheckIfCanBeDisposed:
                        
                        if(Vector3.Distance(Game.LocalPlayer.Character.Position, _ambulance.Position) >= 200f ||
                            Vector3.Distance(_ambulance.Position, spawn.Position) <= 5f)
                        {
                            Dispose();
                        }

                        break;

                    default:
                        break;
                }

                GameFiber.Yield();
            }
        }

        public void Dispose()
        {
            _canRun = false;
            _process?.Abort();

            if (_ambulance) _ambulance.Dismiss();
            if (_EMT) _EMT.Dismiss();
            if (_EMTDriver) _EMTDriver.Dismiss();
        }

        public void Procedures()
        {
            bool emt = false, emtd = false;
            GameFiber.StartNew(delegate
            {
                GameFiber.Sleep(3000);
                _EMT.Position = _patient.LeftPosition;
                NativeFunction.CallByName<uint>("TASK_TURN_PED_TO_FACE_ENTITY", _EMTDriver, _patient, 1000);
                GameFiber.Sleep(1100);
                _EMTDriver.Tasks.PlayAnimation("amb@medic@standing@timeofdeath@enter", "enter", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(9000);

                _EMTDriver.Tasks.PlayAnimation("amb@medic@standing@timeofdeath@idle_a", "idle_b", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(6000);

                _EMTDriver.Tasks.PlayAnimation("amb@medic@standing@timeofdeath@exit", "exit", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(7000);

                emtd = true;
            });
            //====================
            GameFiber.StartNew(delegate
            {

                NativeFunction.CallByName<uint>("TASK_TURN_PED_TO_FACE_ENTITY", _EMT, _patient, 1000);
                GameFiber.Sleep(1100);

                _EMT.Tasks.PlayAnimation("amb@medic@standing@tendtodead@enter", "enter", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(2000);

                _EMT.Tasks.PlayAnimation("amb@medic@standing@tendtodead@idle_a", "idle_b", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(4000);

                _EMT.Tasks.PlayAnimation("amb@medic@standing@tendtodead@exit", "exit", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(2000);

                _EMT.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_intro", "idle_intro", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(1500);

                _EMT.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_b", "idle_d", 3, AnimationFlags.None);
                GameFiber.Sleep(9000);

                emt = true;
            });
            
            while(!emt && !emtd)
            {
                GameFiber.Yield();
            }
        }

        private void MovePatientToAmbulance(Ped patient)
        {
            for (int i = 1; i < 51; i++)
            {
                if(patient) NativeFunction.Natives.SetEntityAlpha(patient, 255 - i * 5, false);
                GameFiber.Wait(10);
            }

            if (patient) patient.Delete();
        }

        private SpawnPoint GetSpawn(EHospitals dispatchFrom)
        {
            if(dispatchFrom == EHospitals.Closest)
            {
                return Hospitals.GetClosestHospitalSpawn(_patient.Position);
            }
            else
            {
                return Hospitals.GetHospitalSpawn(dispatchFrom);
            }
        }
    }
}
