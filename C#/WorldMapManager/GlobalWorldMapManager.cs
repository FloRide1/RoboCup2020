using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WorldMap;

namespace WorldMapManager
{
    public class GlobalWorldMapManager
    {
        double freqRafraichissementWolrdMap = 30;
        Dictionary<int, LocalWorldMap> localWorldMapDictionary = new Dictionary<int, LocalWorldMap>();
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();
        Timer globalWorldMapSendTimer;


        public GlobalWorldMapManager()
        {
            globalWorldMapSendTimer = new Timer(1000/freqRafraichissementWolrdMap);
            globalWorldMapSendTimer.Elapsed += GlobalWorldMapSendTimer_Elapsed;
            globalWorldMapSendTimer.Start();
        }

        private void GlobalWorldMapSendTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnGlobalWorldMap(globalWorldMap);
        }

        public void OnLocalWorldMapReceived(object sender, EventArgsLibrary.LocalWorldMapArgs e)
        {
            AddOrUpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
        }

        private void AddOrUpdateLocalWorldMap(int id, LocalWorldMap localWorldMap)
        {
            lock (localWorldMapDictionary)
            {
                if (localWorldMapDictionary.ContainsKey(id))
                    localWorldMapDictionary[id] = localWorldMap;
                else
                    localWorldMapDictionary.Add(id, localWorldMap);
            }
            MergeLocalWorldMaps();
        }

        private void MergeLocalWorldMaps()
        {
            //Fusion des World Map locales pour construire la world map globale

            //Position des robots
            lock (localWorldMapDictionary)
            {
                foreach (var localMap in localWorldMapDictionary)
                {
                    globalWorldMap.AddOrUpdateRobotLocation(localMap.Key, localMap.Value.robotLocation);
                    globalWorldMap.AddOrUpdateRobotDestination(localMap.Key, localMap.Value.destinationLocation);
                    globalWorldMap.AddOrUpdateRobotWayPoint(localMap.Key, localMap.Value.waypointLocation);
                    globalWorldMap.AddOrUpdateOpponentsList(localMap.Key, localMap.Value.opponentLocationList);
                }

                //Fusion des listes d'adversaires récupérées.
            }
        }

        public delegate void GlobalWorldMapEventHandler(object sender, GlobalWorldMapArgs e);
        public event EventHandler<GlobalWorldMapArgs> OnGlobalWorldMapEvent;
        public virtual void OnGlobalWorldMap(GlobalWorldMap globalWorldMap)
        {
            var handler = OnGlobalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new GlobalWorldMapArgs {GlobalWorldMap = this.globalWorldMap });
            }
        }
    }
}
