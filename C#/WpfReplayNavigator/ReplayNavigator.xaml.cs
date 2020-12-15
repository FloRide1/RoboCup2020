using EventArgsLibrary;
using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace WpfReplayNavigator
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class ReplayNavigator : Window
    {
        public ReplayNavigator()
        {
            InitializeComponent();
        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void UpdateFileName(object sender, StringEventArgs args)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            //Utilisation ici d'une methode anonyme
            textBlockFileName.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                textBlockFileName.Text = "Current read file:" + args.value;
            }));
            
        }

        private void ChangeReplaySpeedRatio(object sender, EventArgs args)
        {
            OnSpeedChange(speedRatioSlider.Value);
        }

        private void SeekToPosition(object sender, EventArgs args)
        {

        }

        private void OnMouseDownPrev(object sender, EventArgs args)
        {
            OnPrev();
        }
        private void OnMouseDownPlay(object sender, EventArgs args)
        {
            OnPlay();
        }
            
        private void OnMouseDownPause(object sender, EventArgs args)
        {
            OnPause();
        }
        private void OnMouseDownStop(object sender, EventArgs args)
        {

        }
        private void OnMouseDownNext(object sender, EventArgs args)
        {
            OnNext();
        }


        //Output Events
        public event EventHandler<EventArgs> OnSeekEvent;
        public virtual void OnSeek()
        {
            var handler = OnSeekEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler<DoubleEventArgs> OnSpeedChangeEvent;
        public virtual void OnSpeedChange( double val)
        {
            var handler = OnSpeedChangeEvent;
            if (handler != null)
            {
                handler(this, new DoubleEventArgs() { Value = val });
            }
        }
        //public delegate void SimulatedLidarEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> OnPauseEvent;
        public virtual void OnPause()
        {
            var handler = OnPauseEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> OnPlayEvent;
        public virtual void OnPlay()
        {
            var handler = OnPlayEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler<BoolEventArgs> OnLoopEvent;
        public virtual void OnLoop( bool val)
        {
            var handler = OnLoopEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs() { value = val });
            }
        }

        public event EventHandler<BoolEventArgs> OnRepeatEvent;
        public virtual void OnRepeat(bool val)
        {
            var handler = OnRepeatEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs() { value = val });
            }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> OnNextEvent;
        public virtual void OnNext()
        {
            var handler = OnNextEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> OnPrevEvent;
        public virtual void OnPrev()
        {
            var handler = OnPrevEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler<StringEventArgs> OnOpenFileEvent;
        public virtual void OnOpenFile(string filepath)
        {
            var handler = OnOpenFileEvent;
            if (handler != null)
            {
                handler(this, new StringEventArgs() { value = filepath });
            }
        }
        public event EventHandler<StringEventArgs> OnOpenFolderEvent;
        public virtual void OnOpenFolder(string folderpath)
        {
            var handler = OnOpenFolderEvent;
            if (handler != null)
            {
                handler(this, new StringEventArgs() { value = folderpath });
            }
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBoxLoop.IsChecked)
                OnLoop(true);
            else
                OnLoop(false);
        }

        private void CheckBoxRepeat_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBoxRepeat.IsChecked)
                OnRepeat(true);
            else
                OnRepeat(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dial = new Microsoft.Win32.OpenFileDialog();
            dial.Filter = "robot files (*.rbt)|*.rbt";
            var result = dial.ShowDialog();
            if (result ?? false && !string.IsNullOrWhiteSpace(dial.FileName))
            {
                OnOpenFile(dial.FileName);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result== System.Windows.Forms.DialogResult.OK  && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                OnOpenFolder(dialog.SelectedPath);
            }
        }
    }
}
