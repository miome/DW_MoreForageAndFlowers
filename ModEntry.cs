using DeepWoodsMod.API;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MoreForageAndFlowers
{
    public class ModEntry : Mod
    {
        private static int SALT { get; } = 35714856;

	private Dictionary<string, int> reverseObjectInfo = new Dictionary<string,int>();

	private Dictionary<string, List<int>> forageTypes;

        private int[] flowerTypes = new int[]{
            427, // 591, // Tulip, spring (30g)
            429, // 597, // BlueJazz, spring (50g)
            455, // 593, // SummerSpangle, summer (90g)
            453, // 376, // Poppy, summer (140g)
            425, // 595, // FairyRose, fall (290g)
        };

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.UpdateTicked += this.GameEvents_UpdateTicked;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

	private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
             LoadReverseObjectInfo();
             LoadForageList();
	}

        private void GameEvents_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.Ticks == 1)
            {
                if (Helper.ModRegistry.IsLoaded("maxvollmer.deepwoodsmod"))
                {
                    IDeepWoodsAPI deepWoodsAPI = Helper.ModRegistry.GetApi<IDeepWoodsAPI>("maxvollmer.deepwoodsmod");
                    if (deepWoodsAPI != null)
                    {
                        CustomizeDeepWoods(deepWoodsAPI);
                    }
                    else
                    {
                        Monitor.Log("DeepWoodsAPI could not be loaded.", LogLevel.Warn);
                    }
                }
                else
                {
                    Monitor.Log("DeepWoodsMod is not loaded.", LogLevel.Warn);
                }
            }
        }

        private void CustomizeDeepWoods(IDeepWoodsAPI deepWoodsAPI)
        {
            deepWoodsAPI.RegisterObject(ShouldAddSeasonableObjectHere, CreateSeasonableObject);
        }

        private Random currentRandom = null;
        private int currentSeed = 0;
        private double currentLuck = 0.0;

        private bool ShouldAddSeasonableObjectHere(IDeepWoodsLocation deepWoodsLocation, Vector2 location)
        {
            if (deepWoodsLocation.IsClearing || deepWoodsLocation.IsCustomMap)
                return false;

            currentLuck = deepWoodsLocation.LuckLevel;
            if (currentSeed != deepWoodsLocation.Seed || currentRandom == null)
            {
                currentRandom = new Random(deepWoodsLocation.Seed ^ SALT);
                currentSeed = deepWoodsLocation.Seed;
            }

            // 20% chance (no luck modifier)
            return currentRandom.NextDouble() < 0.2;
        }

	private void LoadReverseObjectInfo(){
		foreach(KeyValuePair<int,string> kvp in Game1.objectInformation){
			string name=kvp.Value.Split('/')[0];
			reverseObjectInfo[name]=kvp.Key;
		}
	}

	private void LoadForageList()
	{
		Dictionary<string, List<string>> forageNames = new Dictionary<string, List<string>>(){
			{"spring", new List<string>()},
			{"summer", new List<string>()},
			{"fall", new List<string>()},
			{"winter", new List<string>()},
			{"all", new List<string>()}
		};
		forageTypes =  new Dictionary<string, List<int>>(){
			{"spring", new List<int>()},
			{"summer", new List<int>()},
			{"fall", new List<int>()},
			{"winter", new List<int>()},
			{"all", new List<int>()}
		};

		foreach (KeyValuePair<string, string> entry in Game1.objectContextTags){
			if (entry.Value.Contains("forage_item")){
				int ix = entry.Value.IndexOf("season_");
				if (ix != -1){
					string season=entry.Value.Substring(ix+7);
					ix = season.IndexOf(",");
					if (ix != -1){
						season=season.Substring(0,ix);
					}
					forageNames[season].Add(entry.Key);
					forageTypes[season].Add(reverseObjectInfo[entry.Key]);
				}
			
			}
		}
	       //Monitor.Log(printDictionary(Game1.objectContextTags), LogLevel.Warn);
	       //Monitor.Log(printDictionary(forageNames), LogLevel.Warn);
               //Monitor.Log(printDictionary(forageTypes), LogLevel.Warn);

	}
	private string printDictionary(Dictionary<string,List<string>> dictionary){
		string retval="";
		foreach (KeyValuePair<string,List<string>> kvp in dictionary){
			retval+= string.Format("   Key = {0}, Value = {1}", kvp.Key, string.Join(", ", kvp.Value));
		}
		return retval;
	}
        private string printDictionary(Dictionary<string,List<int>> dictionary){
                string retval="";
                foreach (KeyValuePair<string,List<int>> kvp in dictionary){
                        retval+= string.Format("   Key = {0}, Value = {1}", kvp.Key, string.Join(", ", kvp.Value));
                }
                return retval;
        }
        private string printDictionary(System.Collections.Generic.IDictionary<string, string> dictionary){
                string retval="";
                foreach (KeyValuePair<string,string> kvp in dictionary){
                        retval+= string.Format("   Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                }
                return retval;
        }



        private StardewValley.Object CreateSeasonableObject()
        {
            int parentSheetIndex;
            if (Game1.currentSeason == "summer" && Game1.random.NextDouble() < 0.1)
                parentSheetIndex = 259; // Fiddlehead Fern
            else
                parentSheetIndex = GetForagableForCurrentSeason(currentRandom.Next());

            return new StardewValley.Object(parentSheetIndex, 1, false, -1, GetRandomObjectQuality(currentRandom)) { IsSpawnedObject = true };
        }

        private int GetForagableForCurrentSeason(int random)
        {
	    List<int> foragables = forageTypes[Game1.currentSeason];
	    return foragables[currentRandom.Next(foragables.Count)];
        }

        private int GetRandomObjectQuality(Random random)
        {
            double luck = Game1.player.team.sharedDailyLuck.Value + currentLuck;

            if (luck < 0)
                return StardewValley.Object.lowQuality;

            double d = random.NextDouble();
            if (d < (luck * 0.1))
                return StardewValley.Object.bestQuality;
            else if (d < (luck * 0.25))
                return StardewValley.Object.highQuality;
            else if (d < (luck * 0.5))
                return StardewValley.Object.medQuality;
            else
                return StardewValley.Object.lowQuality;
        }


        private TerrainFeature CreateFlower(int flowerType, Vector2 location)
        {
            return (TerrainFeature)ReflectionHelper.GetDeepWoodsType("Flower")
                .GetConstructor(new Type[] { typeof(int), typeof(Vector2) })
                .Invoke(new object[] { flowerType, location });
        }

        private int GetRandomFlowerType(Random random)
        {
            return flowerTypes[random.Next(0, flowerTypes.Length)];
        }

        private bool IsAreaFree(GameLocation gameLocation, Vector2 location, int width, int height)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (!gameLocation.isTileLocationTotallyClearAndPlaceable(new Vector2(location.X + x, location.Y + y)))
                        return false;
            return true;
        }

    }
}
