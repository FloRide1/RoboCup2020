using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldMap;

namespace WorldMapManager
{
    public class GlobalWorldMapManager
    {
        Dictionary<string, LocalWorldMap> localWorldMapDictionary = new Dictionary<string, LocalWorldMap>();
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();
        
        public void OnLocalWorldMapReceived(object sender, EventArgsLibrary.LocalWorldMapArgs e)
        {
            AddOrUpdateLocalWorldMap(e.RobotName, e.LocalWorldMap);
        }

        private void AddOrUpdateLocalWorldMap(string name, LocalWorldMap localWorldMap)
        {
            if (localWorldMapDictionary.ContainsKey(name))
                localWorldMapDictionary[name] = localWorldMap;
            else
                localWorldMapDictionary.Add(name, localWorldMap);

            MergeLocalWorldMaps();
        }

        private void MergeLocalWorldMaps()
        {
            //Fusion des World Map locales pour construire la world map globale

            //Position des robots
            foreach(var localMap in localWorldMapDictionary)
            {
                globalWorldMap.AddOrUpdateRobotLocation(localMap.Key, localMap.Value.robotLocation);
                globalWorldMap.AddOrUpdateRobotDestination(localMap.Key, localMap.Value.destinationLocation);
                globalWorldMap.AddOrUpdateRobotWayPoint(localMap.Key, localMap.Value.waypointLocation);
            }

            OnGlobalWorldMap(globalWorldMap);
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
