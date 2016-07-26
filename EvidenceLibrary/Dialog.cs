using Rage;

namespace EvidenceLibrary
{
    //TODO: remove first from a List<string> instead of using index?
    public class Dialog
    {
        public bool HasEnded { get; private set; }

        //use to play anims?
        //private Ped _ped1;
        //private Ped _ped2;

        //TODO: properties to change times
        private const int TIME_LINE = 2500;
        private const int TIME_LINE_PAUSE = 3000;

        private string[] _dialog;
        private int timeTimer = TIME_LINE_PAUSE;
        private int timeLine = TIME_LINE;
        private System.Timers.Timer _timer;
         
        private int _currentLine = 0; 
        private int _linesInDialog;

        public Dialog(string[] dialog)
        {
            _dialog = dialog;
            _linesInDialog = _dialog.Length;

            _timer = new System.Timers.Timer(timeTimer);
            _timer.AutoReset = true;
            _timer.Elapsed += delegate
            {
                ShowLine();
            };
        }

        public void StartDialog()
        {
            _timer.Start();
            ShowLine();
        }

        public void StartDialog(Ped ped1, Ped ped2)
        {
            TurnTo(ped1, ped2);
            TurnTo(ped1, ped2);

            StartDialog();
        } 
         
        private void TurnTo(Ped ped, Entity entity, int duration = 1500)
        {
            const ulong TASK_TURN_PED_TO_FACE_ENTITY = 0x5AD23D40115353AC;
            Rage.Native.NativeFunction.CallByHash<uint>(TASK_TURN_PED_TO_FACE_ENTITY, ped, entity, duration);
        }

        private void ShowLine()
        {
            GameFiber.StartNew(delegate
            {
                Game.DisplaySubtitle(_dialog[_currentLine], timeLine);
                //Game.LogVerbose($"Dialog.ShowLine.Line: {_currentLine}, text: {_dialog[_currentLine]}");
             
                _currentLine++;

                if (_currentLine == _linesInDialog) 
                {
                    _timer.Stop();
                    End();
                }
            });
        }

        private void End()
        {
            HasEnded = true;
        }
    }
}
