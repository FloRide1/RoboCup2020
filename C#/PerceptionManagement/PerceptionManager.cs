using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perception
{
    public class PerceptionManager
    {
        string robotName = "";

        public PerceptionManager(string name)
        {
            robotName = name;
        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            //Fonctions de traitement
        }
    }
}
