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
        GlobalWorldMapStorage globalWorldMapStorage = new GlobalWorldMapStorage();
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
            MergeLocalWorldMaps();
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
        }

        private void MergeLocalWorldMaps()
        {
            //Fusion des World Map locales pour construire la world map globale
            lock (localWorldMapDictionary)
            {
                //On rassemble les infos issues des cartes locales de chacun des robots
                foreach (var localMap in localWorldMapDictionary)
                {
                    globalWorldMapStorage.AddOrUpdateRobotLocation(localMap.Key, localMap.Value.robotLocation);
                    globalWorldMapStorage.AddOrUpdateBallLocation(localMap.Key, localMap.Value.ballLocation);
                    globalWorldMapStorage.AddOrUpdateRobotDestination(localMap.Key, localMap.Value.destinationLocation);
                    globalWorldMapStorage.AddOrUpdateRobotWayPoint(localMap.Key, localMap.Value.waypointLocation);
                    globalWorldMapStorage.AddOrUpdateOpponentsList(localMap.Key, localMap.Value.opponentLocationList);
                }

                //Fusion des listes d'adversaires récupérées.
                //TODO : faire un algo de fusion 
                //La fusion porte avant tout sur la balle et sur les adversaires.
                globalWorldMap = new WorldMap.GlobalWorldMap();
                
                //Pour l'instant on prend la position de balle vue par le robot 1 comme vérité, mais c'est à améliorer !
                if (localWorldMapDictionary.Count>0)
                    globalWorldMap.ballLocation = localWorldMapDictionary.First().Value.ballLocation;
                globalWorldMap.teamLocationList = new Dictionary<int, PerceptionManagement.Location>();
                globalWorldMap.opponentLocationList = new List<PerceptionManagement.Location>();

                //Pour l'instant on ajoute tous les opposants vus par chacun des robots, mais c'est à fusionner !
                foreach (var localMap in localWorldMapDictionary)
                {
                    //On ajoute la position des robots de l'équipe dans la WorldMap
                    globalWorldMap.teamLocationList.Add(localMap.Key, localMap.Value.robotLocation);
                    //On ajoute la position des adversaires dans la WorldMap
                    var opponentLocationList = localMap.Value.opponentLocationList.ToList();
                    foreach (var oppLocation in opponentLocationList)
                    {
                        globalWorldMap.opponentLocationList.Add(oppLocation);
                    }                    
                }
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
                handler(this, new GlobalWorldMapArgs {GlobalWorldMap = this.globalWorldMapStorage });
            }
        }
    }
}
