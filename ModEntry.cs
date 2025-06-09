using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace AutoTouchStatue
{
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string> formatValue = null, string fieldId = null);
    }

    public class ModConfig
    {
        public int distance { get; set; } = 5;
    }
    public class ModEntry : Mod
    {
        private HashSet<Vector2> touchedStatuesToday = new();
        private HashSet<String> ignoredFurnitures = new();
        private ModConfig modConfig;
        private bool isGMCMRegistered = false;

        public override void Entry(IModHelper helper)
        {
            this.modConfig = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.DayStarted += (_, _) => touchedStatuesToday.Clear();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (!isGMCMRegistered)
            {
                RegisterGMCM();
                isGMCMRegistered = true;
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // 每 10 帧处理一次（约 6 次/秒）
            if (!Context.IsWorldReady || !e.IsMultipleOf(10))
                return;

            GameLocation location = Game1.currentLocation;
            Farmer player = Game1.player;

            // 处理地上放置的雕像（如农场上的）
            foreach (var pair in location.objects.Pairs)
            {
                var tile = pair.Key;
                var obj = pair.Value;
                var name = obj.Name?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (ignoredFurnitures.Contains(name)) continue;
                var isStatue = IsBuffStatue(name);
                if (!isStatue)
                {
                    ignoredFurnitures.Add(name);
                    continue;
                }
                if (IsPlayerNear(player.Tile, tile))
                {   
                    if (touchedStatuesToday.Add(tile))
                    {
                        //obj.performObjectDropInAction(null, false, player);
                        obj.checkForAction(player, false);
                        Monitor.Log($"靠近物品雕像自动触发：{obj.Name}", LogLevel.Trace);
                    }
                }
            }
        }

        private bool IsBuffStatue(String name)
        {
            return name.Contains("statue");
        }

        private bool IsPlayerNear(Vector2 playerTile, Vector2 targetTile)
        {
            return Vector2.Distance(playerTile, targetTile) <= 3f;
        }

        private void RegisterGMCM()
        {
            var gmcmApi = Helper.ModRegistry
                .GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi == null)
                return;

            gmcmApi.Register(
                mod: ModManifest,
                reset: () => modConfig = new ModConfig(),
                save: () => Helper.WriteConfig(modConfig)
            );

            gmcmApi.AddNumberOption(
                mod: ModManifest,
                name: () => "distance",
                tooltip: () => "When the player is within this distance from the statue, automatically touch the statue",
                getValue: () => modConfig.distance,
                setValue: value => modConfig.distance = value,
                min: 1,
                max: 100
            );

        }
    }
}
