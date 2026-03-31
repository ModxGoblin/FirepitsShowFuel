using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

using API = Vintagestory.API;

namespace FirepitsShowFuel.HarmonyPatches
{
    public class FancyFirepitAttributes
    {
        public string model = "firewood";
        public string domain = "game";
        public string fuelname = "firewood";
        public Dictionary<string, string[]> burnTextures = new();
        //
        public FancyFirepitAttributes(string newModel, string newFuelname, Dictionary<string, string[]> newBurnTextures)
        {
            model = newModel;
            fuelname = newFuelname;
            burnTextures = newBurnTextures;
        }

        public override string ToString()
        {
            return "model:" + model + "\n" +
                   "domain:" + domain + "\n" +
                   "fuelname:" + fuelname + "\n" +
                   "burnTextures:" + burnTextures.Count + "\n";
        }
    }

    [HarmonyPatch]
    public static class firepit_patch
    {
        [HarmonyPatch(typeof(BlockEntityFirepit), "OnTesselation")]
        [HarmonyPrefix]
        public static bool OnTesselation(BlockEntityFirepit __instance, ITerrainMeshPool mesher, ITesselatorAPI tesselator, ref bool __result)
        {
            if (__instance.Block == null || __instance.Block.Code.Path.Contains("construct"))
            {
                __result = false; 
                return false;
            }

            ItemStack contentStack = __instance.inputStack == null ? __instance.outputStack : __instance.inputStack;
            MeshData contentmesh = Call_getContentMesh(__instance, contentStack, tesselator);
            if (contentmesh != null)
            {
                mesher.AddMeshData(contentmesh);
            }

            string fuelState = __instance.fuelStack == null ? "" : __instance.fuelStack.Collectible.Code.Path;
            string fuelDomain = __instance.fuelStack == null ? "" : __instance.fuelStack.Collectible.Code.Domain;
            string burnState = __instance.Block.Variant["burnstate"];

            FancyFirepitAttributes fancyFirepitAttributes = GetData(__instance.fuelStack, __instance.Block);
            //Console.WriteLine("[Firepit] attr [" + fancyFirepitAttributes.ToString() + "]");


            string contentState = __instance.CurrentModel.ToString().ToLowerInvariant();
            if (burnState == "cold" && __instance.fuelSlot.Empty) burnState = "extinct";
            if (burnState == null)
            {
                __result = true;
                return false;
            }

            mesher.AddMeshData(getOrCreateMesh_n(__instance, fancyFirepitAttributes, burnState, contentState, fuelState, fuelDomain));

            __result = true;
            return false;
        }

        static FancyFirepitAttributes GetData(ItemStack fuelItemStack, Block firepit)
        {
            string fuelState = fuelItemStack == null ? "" : fuelItemStack.Collectible.Code.Path;
            string fuelDomain = fuelItemStack == null ? "" : fuelItemStack.Collectible.Code.Domain;
            string burnState = firepit.Variant["burnstate"];

            JsonObject attr = fuelItemStack?.Collectible.Attributes;

            if (fuelState == "" && burnState == "lit")
            {
                return new FancyFirepitAttributes(
                    "cinder",
                    "game",
                    new Dictionary<string, string[]> { { "cinder", new[] { "block/coal/cinder" } }, }
                );
            }
            if (attr != null && attr["fancyfirepit"].Exists)
            {
                return attr["fancyfirepit"].AsObject<FancyFirepitAttributes>();
            }

            return new FancyFirepitAttributes(
                "firewood",
                "firewood",
                new Dictionary<string, string[]> {
                    { "birch", new[] { "block/wood/debarked/birch" } },
                    { "walnut-h", new[] { "block/wood/bark/walnut-h" } },
                }
            );
        }

        static string GetMeshAndTextureKeys(string fuelState, string burnState, string fuelDomain, out Dictionary<string, string[]> replacementTextures)
        {
            replacementTextures = new Dictionary<string, string[]>();
            string fuelname = "firewood"; 
            string modelType = "firewood";
            string logType = "oak";



            if (fuelState.StartsWith("log")){

                string[] str = fuelState.Split("-");
                logType = str[2];

                fuelState = "log";

            }
            if (fuelState.StartsWith("debarkedlog"))
            {
                string[] str = fuelState.Split("-");
                logType = str[1];

                fuelState = "debarkedlog";
            }

            if (fuelState.StartsWith("logquad"))
            {
                string[] str = fuelState.Split("-");
                logType = str[2];

                fuelState = "log";
            }

            //Console.WriteLine("[Firepit] logType is [" + logType + "]");
            //Console.WriteLine("[Firepit] fuelDomain is [" + fuelDomain + "]");


            switch (fuelState)
            {
                case "ore-bituminouscoal":
                    fuelname = "bituminous";

                    replacementTextures.Add("coal", ["block/coal/bituminous"]);
                    //replacementKeys.Add("coal", "bituminous");
                    modelType = "coal";
                    break;

                case "ore-lignite":
                    fuelname = "lignite";
                    //replacementKeys.Add("coal", "lignite");

                    replacementTextures.Add("coal", ["block/coal/lignite"]);
                    modelType = "coal";

                    break;

                case "ore-anthracite":
                    fuelname = "anthracite";
                    replacementTextures.Add("coal", ["block/coal/anthracite"]);

                    modelType = "coal";

                    break;

                case "charcoal":
                    fuelname = "charcoal";
                    replacementTextures.Add("coal", ["block/coal/charcoal"]);

                    modelType = "coal";

                    break;

                case "coke":
                    fuelname = "charcoal";
                    replacementTextures.Add("coal", ["block/coal/coke"]);

                    modelType = "coal";
                    break;

                case "peatbrick":
                    fuelname = "peat";
                    replacementTextures.Add("peat", ["block/soil/peat"]);

                    modelType = "peat";
                    break;

                case "driedpeat":
                    fuelname = "driedpeat";
                    replacementTextures.Add("peat", ["block/soil/peat"]);

                    modelType = "peat";
                    break;

                case "refinedpeat":
                    fuelname = "refinedpeat";
                    replacementTextures.Add("peat", ["block/soil/peat"]);

                    modelType = "peat";
                    break;

                case "dryablepeatbrick":
                    fuelname = "dryablepeatbrick";
                    replacementTextures.Add("peat", ["block/soil/peat"]);

                    modelType = "peat";
                    break;

                case "dryablerefinedpeatbrick":
                    fuelname = "dryablerefinedpeatbrick";
                    replacementTextures.Add("peat", ["block/soil/peat"]);

                    modelType = "peat";
                    break;

                case "fpreparedfirewood":
                    fuelname = "preparedfirewood";
                    replacementTextures.Add("birch", ["fseasonedfirewood:block/wood/firewood/curable/side"]);
                    replacementTextures.Add("walnut-h", ["fseasonedfirewood:block/wood/firewood/curable/bark"]);
                    modelType = "firewood";
                    break;

                case "fseasonedfirewood":
                    fuelname = "seasonedfirewood";
                    replacementTextures.Add("birch", ["fseasonedfirewood:block/wood/firewood/seasoned/side"]);
                    replacementTextures.Add("walnut-h", ["fseasonedfirewood:block/wood/firewood/seasoned/bark"]);
                    modelType = "firewood";
                    break;

                case "agedfirewood":
                    fuelname = "agedfirewood";
                    replacementTextures.Add("birch", ["block/wood/debarked/aged"]);
                    replacementTextures.Add("walnut-h", ["block/wood/bark/aged-h"]);
                    modelType = "firewood";
                    break;

                case "log":
                    if (fuelDomain == "wildcrafttree")
                    {
                        //Console.WriteLine("[Firepit] wildrcraftt [" + logType + "]");

                        replacementTextures.Add("baldcypress", ["wildcrafttree:block/wood/treetrunk/" + logType]);
                        replacementTextures.Add("walnut-h", ["wildcrafttree:block/wood/bark/" + logType + "-h"]);
                    } 
                    else
                    {
                        replacementTextures.Add("baldcypress", ["block/wood/treetrunk/" + logType]);
                        replacementTextures.Add("walnut-h", ["block/wood/bark/" + logType + "-h"]);
                    }
                    fuelname = "log";
                    modelType = "log";
                    break;

                case "debarkedlog":
                    if (fuelDomain == "wildcrafttree")
                    {
                        replacementTextures.Add("baldcypress", ["wildcrafttree:block/wood/treetrunk/" + logType]);
                        replacementTextures.Add("walnut-h", ["block/wood/debarked/" + logType]);
                    }
                    else
                    {
                        replacementTextures.Add("baldcypress", ["block/wood/treetrunk/" + logType]);
                        replacementTextures.Add("walnut-h", ["block/wood/debarked/" + logType]);
                    }
                    fuelname = "debarkedlog";
                    modelType = "log";
                    break;

                case "stick":
                    fuelname = "stick";
                    replacementTextures.Add("birch", ["block/wood/debarked/birch"]);
                    modelType = "stick";
                    break;

                case "drygrass":
                    fuelname = "drygrass";
                    replacementTextures.Add("foliage", ["block/hay/bundle"]);
                    modelType = "foliage";
                    break;

                case "briquette-sawdust":
                    replacementTextures.Add("brick", ["fuelbriquettes:block/sawdust-brick1", "block/coal/hot_briquette_wood"]);
                    //Console.WriteLine("[Firepit] saw dusty");
                    modelType = "briquette";
                    break;
                case "briquette-peat":
                    replacementTextures.Add("brick", ["fuelbriquettes:block/peat-brick1", "block/coal/hot_briquette_wood"]);
                    modelType = "briquette";
                    break;
                case "briquette-sawdust-soaked":
                    replacementTextures.Add("brick", ["fuelbriquettes:block/sawdust-soaked-brick1", "block/coal/hot_briquette_wood"]);
                    modelType = "briquette";
                    break;
                case "briquette-peat-soaked":
                    replacementTextures.Add("brick", ["fuelbriquettes:block/peat-soaked-brick1", "block/coal/hot_briquette_wood"]);
                    modelType = "briquette";
                    break;
                case "briquette-charcoal":
                    replacementTextures.Add("brick", ["fuelbriquettes:block/charcoal-brick1", "block/coal/hot_briquette_coal"]);
                    modelType = "briquette";
                    break;

                case "":
                    if (burnState == "lit")
                    {
                        fuelname = "cinder";
                        replacementTextures.Add("cinder", ["block/coal/cinder"]);
                        modelType = "cinder";
                    }
                    break;

                default:
                    fuelname = "firewood";
                    replacementTextures.Add("birch", ["block/wood/debarked/birch"]);
                    replacementTextures.Add("walnut-h", ["block/wood/bark/walnut-h"]);
                    modelType = "firewood";
                    break;

            }

            return modelType;
        }

        static MeshData getOrCreateMesh_n(BlockEntityFirepit __instance, FancyFirepitAttributes fancyFirepitAttributes, string burnstate, string contentstate, string fuelState, string fuelDomain)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(__instance.Api, "firepit-meshes", () => new Dictionary<string, MeshData>());

            Dictionary<string, string[]> replacementTextures = new Dictionary<string, string[]>();

            foreach(var a in fancyFirepitAttributes.burnTextures)
            {
                //Console.WriteLine("[Firepit]  [" + a.Key + ":" + a.Value[0] + "]");
                replacementTextures.Add(a.Key, a.Value);
            }

            string key = burnstate + "-" + contentstate;

            if (!Meshes.TryGetValue(key, out MeshData meshdata))
            {
                Block block = __instance.Api.World.BlockAccessor.GetBlock(__instance.Pos);
                if (block.BlockId == 0) return null;

                MeshData[] meshes = new MeshData[17];
                ITesselatorAPI mesher = ((ICoreClientAPI)__instance.Api).Tesselator;
                ITextureAtlasAPI ba = ((ICoreClientAPI)__instance.Api).BlockTextureAtlas;
                ICoreClientAPI cAPI = ((ICoreClientAPI)__instance.Api);

                string meshPath = "shapes/block/wood/firepit/" + fancyFirepitAttributes.model + "/" + key + "-" + fancyFirepitAttributes.model + ".json";
                if (burnstate == "extinct"){
                    meshPath = "shapes/block/wood/firepit/" + key + ".json";
                }


                Shape shape = API.Common.Shape.TryGet(__instance.Api, meshPath);

                
                Block b = block.Clone();


                foreach (var k in b.Textures.Keys)
                {
                    foreach (string texturekey in replacementTextures.Keys)
                    {
                        if (k == texturekey)
                        {
                            AssetLocation a = new AssetLocation(replacementTextures[texturekey][0]);
                            
                            b.Textures[k].Base.Path = a.Path;
                            b.Textures[k].Base.Domain = a.Domain;

                            if (replacementTextures[texturekey].Length > 1 && burnstate == "lit")
                            {
                                AssetLocation c = new AssetLocation(replacementTextures[texturekey][1]);
                                b.Textures[k].BlendedOverlays[0].Base = c.Path;
                            }
                        }
                    }
                }

                ShapeTextureSource tex = new ShapeTextureSource(
                    ((ICoreClientAPI)__instance.Api),
                    shape,
                    "firefixer",
                    b.Textures,
                    (p) => p
                );

                mesher.TesselateShape("ding", shape, out meshdata, tex);
            }

            return meshdata;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BlockEntityFirepit), "getContentMesh")]
        static MeshData Call_getContentMesh(BlockEntityFirepit __instance, ItemStack contentStack, ITesselatorAPI tesselator)
        {
            throw new NotImplementedException();
        }
    }
}
