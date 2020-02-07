using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void ChangeReplaySpeedRatio(object sender, EventArgs args)
        {

        }

        private void SeekToPosition(object sender, EventArgs args)
        {

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


        //Output Events
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

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBoxLoop.IsChecked)
                OnLoop(true);
            else
                OnLoop(false);
        }
    }
}
