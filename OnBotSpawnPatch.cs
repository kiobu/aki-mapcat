using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using System.Linq;
using System.Reflection;
using EFT;
using Comfort.Common;
using Mapcat;

namespace Mapcat
{
    class MapcatOnBotSpawnPatch : ModulePatch
    {
        private string dType = "BotSpawnerClass";
        private string dMethod = "AddPlayer";
        protected override MethodBase GetTargetMethod()
        {
            var desiredType = PatchConstants.EftTypes.Single(x => x.Name == dType);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            var desiredMethod = desiredType.GetMethod(dMethod, flags);

            Logger.LogDebug($"{this.GetType().Name} Type: {desiredType?.Name}");
            Logger.LogDebug($"{this.GetType().Name} Method: {desiredMethod?.Name}");

            return desiredMethod;
        }

        [PatchPostfix]
        private static void PatchPostfix(Player player)
        {
            // MapcatMain.HandleNewSpawn(player);
        }
    }
}
