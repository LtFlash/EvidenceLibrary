using Rage;
using Rage.Native;

namespace EvidenceLibrary.Services
{
    public class Coroner
    {
        private Ped _MEdriver;
        private Ped _ME;
        private Blip _blipME;
        private string[] _pedModels = new string[]
        {
            "s_m_m_paramedic_01",
        };

        private Vehicle _vehicle;
        private string[] _meVan = new string[]
        {
            "burrito3",
            "youga",
        };

        private SpawnPoint _coronersOffice = new SpawnPoint(270.346252f, new Vector3(218.361008f, -1381.16431f, 30.1247978f));

        private Ped _body;
        private SpawnPoint _point;
        private bool _spawnAtScene;
        private string[] _dialogME;

        private SpawnPoint spawn = new SpawnPoint(0, new Vector3());

        private Dialog _dialog;
        private GameFiber _process;
        private bool _canRun = true;
        private System.Windows.Forms.Keys _keyStartDialog = System.Windows.Forms.Keys.Y;

        public Coroner(Ped body, SpawnPoint dispatchTo, string[] dialog,
            bool spawnAtScene = false)
            //TODO: different dispatchFrom locations?
        {
            _spawnAtScene = spawnAtScene;
            _body = body;
            _point = dispatchTo;
            _dialogME = dialog;
            spawn = _spawnAtScene ? _point : _coronersOffice;

            _dialog = new Dialog(_dialogME);
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


                        _vehicle = new Vehicle(_meVan[MathHelper.GetRandomInteger(_meVan.Length)], spawn.Position);
                        _vehicle.Heading = spawn.Heading;
                        _vehicle.MakePersistent();
                        _vehicle.AttachBlip();
                        _vehicle.IsInvincible = true;

                        _MEdriver = new Ped(_pedModels[MathHelper.GetRandomInteger(_pedModels.Length)], _vehicle.Position.Around2D(5f), 0f);
                        _MEdriver.RandomizeVariation();
                        _MEdriver.WarpIntoVehicle(_vehicle, -1);
                        _MEdriver.BlockPermanentEvents = true;
                        _MEdriver.KeepTasks = true;

                        _ME = new Ped(_pedModels[MathHelper.GetRandomInteger(_pedModels.Length)], _vehicle.Position.Around2D(5f), 0f);
                        _ME.RandomizeVariation();
                        _ME.WarpIntoVehicle(_vehicle, 0);
                        _ME.BlockPermanentEvents = true;
                        _ME.KeepTasks = true;

                        _state = _spawnAtScene ? EState.GoToPatient : EState.DispatchFromHospital;

                        break;
                    case EState.DispatchFromHospital:

                        _MEdriver.Tasks.DriveToPosition(_vehicle, _point.Position, 8f, VehicleDrivingFlags.Emergency, 2f);

                        _state = EState.WaitForArrival;

                        break;
                    case EState.WaitForArrival:

                        if(_vehicle.Position.DistanceTo(_point.Position) <= 10f && _vehicle.Speed == 0f)
                        {
                            _vehicle.IsSirenSilent = true;
                            _state = EState.GoToPatient;
                        }

                        break;
                    case EState.GoToPatient:

                        _ME.Tasks.GoToOffsetFromEntity(_body, 1f, 0f, 1f);
                        _MEdriver.Tasks.GoToOffsetFromEntity(_body, 4f, 8f, 1f);

                        _state = EState.CheckIfWithPatient;
                        break;

                    case EState.CheckIfWithPatient:

                        if (_ME.Position.DistanceTo(_body) < 2f && _MEdriver.Position.DistanceTo(_body) < 5f)
                        {
                            _state = EState.Procedures;
                        }

                            break;
                    case EState.Procedures:

                        //TODO: make an alternative function using TaskSequence
                        Procedures();

                        MovePatientToAmbulance(_body);

                        _blipME = new Blip(_ME);
                        _blipME.Color = System.Drawing.Color.Green;
                        _blipME.Sprite = BlipSprite.Health;
                        _blipME.Scale = 0.25f;

                        Game.DisplayNotification("Talk to the medical examiner to receive a report.");
                        _state = EState.WaitForDialogActivation;
                        break;

                    case EState.WaitForDialogActivation:

                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, _ME.Position) <= 3f)
                        {
                            Game.DisplayHelp($"Press ~y~{_keyStartDialog}~s~ to talk to the ME.");

                            if (Game.IsKeyDown(_keyStartDialog))
                            {
                                if (_blipME) _blipME.Delete();

                                _dialog.StartDialog(_ME, Game.LocalPlayer.Character);
                                _state = EState.WaitDialogToEnd;
                            }
                        }

                        break;

                    case EState.WaitDialogToEnd:
                        if (_dialog.HasEnded) _state = EState.BackToAmbulance;
                        break;

                    case EState.BackToAmbulance:

                        _ME.Tasks.GoToOffsetFromEntity(_vehicle, 0.1f, 0f, 1f);
                        _MEdriver.Tasks.GoToOffsetFromEntity(_vehicle, 0.1f, 0f, 1f);

                        bool emt = true, emtd = true;
                        while (true)
                        {
                            if (emt && Vector3.Distance(_ME.Position, _vehicle.Position) <= 5f)
                            {
                                _ME.Tasks.EnterVehicle(_vehicle, 0);
                                emt = false;
                            }

                            if (emtd && Vector3.Distance(_MEdriver.Position, _vehicle.Position) <= 5f)
                            {
                                _MEdriver.Tasks.EnterVehicle(_vehicle, -1);
                                emtd = false;
                            }

                            if (!emt && !emtd) break;

                            GameFiber.Yield();
                        }

                        _state = EState.CheckIfBackInAmbo;
                        break;

                    case EState.CheckIfBackInAmbo:

                        if (_MEdriver.IsInVehicle(_vehicle, false) && _ME.IsInVehicle(_vehicle, false))
                        {
                            _MEdriver.Tasks.DriveToPosition(spawn.Position, 10f, VehicleDrivingFlags.Emergency);
                            _vehicle.GetAttachedBlip().Delete();
                            _state = EState.CheckIfCanBeDisposed; 
                        }

                        break;

                    case EState.CheckIfCanBeDisposed:
                        
                        if(Vector3.Distance(Game.LocalPlayer.Character.Position, _vehicle.Position) >= 200f ||
                            Vector3.Distance(_vehicle.Position, spawn.Position) <= 5f)
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

            if (_blipME.Exists()) _blipME.Delete();
            if (_vehicle) _vehicle.Dismiss();
            if (_ME) _ME.Dismiss();
            if (_MEdriver) _MEdriver.Dismiss();
        }

        private void Procedures()
        {
            bool emt = false, emtd = false;
            GameFiber.StartNew(delegate
            {
                GameFiber.Sleep(3000);
                _ME.Position = _body.LeftPosition;
                NativeFunction.CallByName<uint>("TASK_TURN_PED_TO_FACE_ENTITY", _MEdriver, _body, 1000);
                GameFiber.Sleep(1100);
                _MEdriver.Tasks.PlayAnimation("amb@medic@standing@timeofdeath@enter", "enter", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(9000);

                _MEdriver.Tasks.PlayAnimation("amb@medic@standing@timeofdeath@idle_a", "idle_b", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(6000);

                _MEdriver.Tasks.PlayAnimation("amb@medic@standing@timeofdeath@exit", "exit", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(7000);

                emtd = true;
            });
            //====================
            GameFiber.StartNew(delegate
            {

                NativeFunction.CallByName<uint>("TASK_TURN_PED_TO_FACE_ENTITY", _ME, _body, 1000);
                GameFiber.Sleep(1100);

                _ME.Tasks.PlayAnimation("amb@medic@standing@tendtodead@enter", "enter", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(2000);

                _ME.Tasks.PlayAnimation("amb@medic@standing@tendtodead@idle_a", "idle_b", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(4000);

                _ME.Tasks.PlayAnimation("amb@medic@standing@tendtodead@exit", "exit", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(2000);

                _ME.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_intro", "idle_intro", 4, AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(1500);

                _ME.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_b", "idle_d", 3, AnimationFlags.None);
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
                return Hospitals.GetClosestHospitalSpawn(_body.Position);
            }
            else
            {
                return Hospitals.GetHospitalSpawn(dispatchFrom);
            }
        }
    }
}
