using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using EFT;
using System.Collections.Generic;
using Comfort.Common;

namespace Mapcat
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class MapcatPlugin : BaseUnityPlugin
    {
        public static GameObject mapcatObject;
        public static ManualLogSource logger;

        public static int cullingLayer = 30;
        private void Awake()
        {
            logger = Logger;

            // Plugin startup logic
            logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            new MapcatExecuteOnRaidStartPatch().Enable();
            // new MapcatOnBotSpawnPatch().Enable();
            logger.LogInfo("Mapcat patches executed.");

            mapcatObject = new GameObject("MapcatObject");
            DontDestroyOnLoad(mapcatObject);
        }

        public static void Execute(Player player)
        {
            mapcatObject.AddComponent<Camera>();
            mapcatObject.GetComponent<Camera>().enabled = false;
            mapcatObject.GetComponent<Camera>().cullingMask = 1 << cullingLayer;

            mapcatObject.AddComponent<MapcatMain>();
            mapcatObject.GetComponent<MapcatMain>().ply = player;
            mapcatObject.GetComponent<MapcatMain>().isInWorld = true;
        }
    }

    public struct BotBlip
    {
        public BotBlip(BotOwner bot, GameObject blip)
        {
            Bot = bot;
            Blip = blip;
        }

        public BotOwner Bot { get; }
        public GameObject Blip { get; }
    }

    public class MapcatMain : MonoBehaviour
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("MapcatMain");

        public bool isInWorld = false;
        public int camDistance = 50;

        public Camera mapCamera;
        public Player ply;
        public GameObject playerBlip;
        public Camera playerCamera;

        public static List<BotBlip> blipTracker = new List<BotBlip>();

        // Should be called immediately on raid start (since component is created via MapcatPlugin.Execute().)
        public void Start()
        {
            if (!isInWorld)
            {
                throw new System.Exception("Somehow we aren't in the world.");
            }

            // Create player blip primitive.
            playerBlip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerBlip.layer = MapcatPlugin.cullingLayer;
            playerBlip.GetComponent<Renderer>().material.color = Color.green;

            // Player main camera.
            playerCamera = Camera.main;
            playerCamera.cullingMask = playerCamera.cullingMask | ~(1 << MapcatPlugin.cullingLayer);

            // Create "screen" for minimap.
            mapCamera = MapcatPlugin.mapcatObject.GetComponent<Camera>();
            mapCamera.depth = 50;
            mapCamera.enabled = false;
            mapCamera.rect = new Rect(0.0f, 0.75f, 0.25f, 0.25f);

            // Handle bot spawn Action.
            var botGame = (IBotGame) Singleton<AbstractGame>.Instance;
            botGame.BotsController.BotSpawner.OnBotCreated += HandleNewSpawn;

            // Enable and begin minimap tracking.
            mapCamera.enabled = true;
        }

        public void Update()
        {
            if (!isInWorld)
            {
                logger.LogError("Update() called when not in world.");
                return;
            }

            if (!mapCamera.enabled)
            {
                logger.LogError("Mapcam was disabled somehow.");
                return;
            }

            // Minimap tracks player.
            mapCamera.transform.position = new Vector3(ply.Transform.position.x, ply.Transform.position.y + camDistance, ply.Transform.position.z);
            mapCamera.transform.LookAt(ply.Transform.position);

            // Player blip tracking.
            playerBlip.transform.position = new Vector3(ply.Transform.position.x, ply.Transform.position.y + (camDistance - 25f), ply.Transform.position.z);

            // Bot blip tracking.
            foreach (BotBlip obj in blipTracker)
            {
                obj.Blip.transform.position = new Vector3(obj.Bot.Transform.position.x, obj.Bot.Transform.position.y + (camDistance - 25f), obj.Bot.Transform.position.z);
            }
        }

        // Called from patch when a new bot spawns.
        public void HandleNewSpawn(BotOwner owner)
        {
            // TODO: Create List<Player>, add new bot to List, create primitive, then in Update(): Track all entries in List and update blip positions.
            var blip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blip.layer = MapcatPlugin.cullingLayer;
            blip.GetComponent<Renderer>().material.color = Color.red;

            blipTracker.Add(new BotBlip(owner, blip));
        }
    }
}
