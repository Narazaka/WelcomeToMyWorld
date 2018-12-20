using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelcomeToMyWorld {
    public class Watcher {
        VrcApi VrcApi { get; }
        string WorldId { get; }
        WorldCallback Callback { get; }
        WorldErrorCallback ErrorCallback { get; }
        TimeSpan Interval { get; }

        public delegate void WorldCallback(WorldDiff diff);
        public delegate void WorldErrorCallback(Exception error);

        Timer Timer { get; set; }

        public Watcher(string username, string password, string worldId, WorldCallback callback, WorldErrorCallback errorCallback, TimeSpan? interval = null) {
            VrcApi = new VrcApi(username, password);
            WorldId = worldId;
            Callback = callback;
            ErrorCallback = errorCallback;
            Interval = interval ?? TimeSpan.FromMinutes(3);
        }

        public void Start() {
            Timer = new Timer(Check, null, TimeSpan.Zero, Interval);
        }

        public void Stop() {
            Timer.Dispose();
        }

        async void Check(object stateInfo) {
            try {
                var world = await VrcApi.GetWorld(WorldId);
                var worldInstances = new List<WorldInstance>();
                foreach (var instance in world.Instances) {
                    // worldInstances.Add(await VrcApi.GetWorldInstance(WorldId, instance.Id));
                }
                Callback(WorldDiff.GetDiff(world, worldInstances));
            } catch (Exception e) {
                ErrorCallback(e);
            }
        }

        public class WorldDiff {
            public static World _PreviousWorld { get; set; }
            public static List<WorldInstance> _PreviousWorldInstances { get; set; }
            public static WorldDiff GetDiff(World world, List<WorldInstance> worldInstances) {
                if (_PreviousWorld == null || _PreviousWorld.Id != world.Id) {
                    _PreviousWorld = world;
                    _PreviousWorldInstances = worldInstances;
                    return new WorldDiff { PreviousWorld = world, PreviousWorldInstances = worldInstances, World = world, WorldInstances = worldInstances };
                }
                var previousWorld = _PreviousWorld;
                var previousWorldInstances = _PreviousWorldInstances;
                _PreviousWorld = world;
                _PreviousWorldInstances = worldInstances;
                return new WorldDiff {
                    PreviousWorld = previousWorld,
                    PreviousWorldInstances = previousWorldInstances,
                    World = world,
                    WorldInstances = worldInstances,
                    TotalLikesChanged = previousWorld.TotalLikes != world.TotalLikes,
                    TotalVisitsChanged = previousWorld.TotalVisits != world.TotalVisits,
                    ReleaseStatusChanged = previousWorld.ReleaseStatus != world.ReleaseStatus,
                    InstanceCountChanged = previousWorld.Instances.Count != world.Instances.Count,
                    OccupantsChanged = previousWorld.Occupants != world.Occupants,
                };
            }

            public World PreviousWorld { get; set; }
            public List<WorldInstance> PreviousWorldInstances { get; set; }
            public World World { get; set; }
            public List<WorldInstance> WorldInstances { get; set; }
            public bool Changed {
                get {
                    return
                        TotalLikesChanged ||
                        TotalVisitsChanged ||
                        ReleaseStatusChanged ||
                        OccupantsChanged ||
                        InstanceCountChanged;
                }
            }
            public bool TotalLikesChanged { get; set; }
            public bool TotalVisitsChanged { get; set; }
            public bool ReleaseStatusChanged { get; set; }
            public bool InstanceCountChanged { get; set; }
            public bool OccupantsChanged { get; set; }
            public override string ToString() {
                var str = "";
                if (ReleaseStatusChanged) str += $"status {PreviousWorld.ReleaseStatus} => {World.ReleaseStatus}\n";
                if (InstanceCountChanged) str += $"instance {PreviousWorld.Instances.Count} => {World.Instances.Count}\n";
                if (OccupantsChanged) str += $"users {PreviousWorld.Occupants} => {World.Occupants}\n";
                return str;
            }
        }
    }
}
