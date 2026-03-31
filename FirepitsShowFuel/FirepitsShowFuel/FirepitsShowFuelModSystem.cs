using Vintagestory.API.Common;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FirepitsShowFuel
{
    public class FirepitsShowFuelModSystem : ModSystem
    {

        private ICoreServerAPI api;
        public override void Start(ICoreAPI api)
        {
            var harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchAll();
        }

    }
}
