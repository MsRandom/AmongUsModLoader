using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AmongUs.Api;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnhollowerBaseLib.Runtime;

namespace AmongUs.ModLoader
{
    public class ModLoader : Mod
    {
        public static readonly ManualLogSource Log = Logger.CreateLogSource("ModLoader");
        public static readonly Dictionary<string, Mod> Mods = new Dictionary<string, Mod>();
        public static readonly Harmony Harmony = new Harmony("amongus.modloader");
        public const string ModDirectory = "Mods";
        private static readonly BepInPlugin LoaderInfo = typeof(ModLoaderPlugin).GetCustomAttribute<BepInPlugin>();

        public ModLoader() : base("ModLoader", LoaderInfo.Name, LoaderInfo.Version.ToString()) {}

        public override void Load()
        {
            VersionString.ShowEvent += shower => shower.text.Text += ", ModLoader v" + LoaderInfo.Version;
        }
        
        internal static void AddPatchType(Type type) => type.GetNestedTypes().Do(Harmony.PatchAll);

        internal static void InitializeLoaderEvents()
        {
            UnityVersionHandler.Initialize(2019, 4, 9);
            AddPatchType(typeof(VersionString));
        }
        
        internal static void InitializeModEvents()
        {
            //TODO improve this
            AddPatchType(typeof(Game));
            AddPatchType(typeof(Language));
            AddPatchType(typeof(MainMenu));
            Log.LogDebug("Initialized Events.");
        }

        internal static void AddMod(Mod mod)
        {
            mod.Load();
            Mods[mod.ID] = mod;
        }
        
        internal static async Task LoadModsAsync(string dir)
        {
            foreach (var file in Directory.GetFiles(ModDirectory))
            {
                if (!file.ToLower().EndsWith(".dll")) continue;

                await LoadModAsync(Assembly.LoadFile(dir + file));
            }
        }

        private static async Task LoadModAsync(Assembly assembly)
        {
            using (var entry = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(resource => resource.EndsWith(".ModEntry.txt"))))
            {
                if (entry != null)
                {
                    var entryType = assembly.GetType(await new StreamReader(entry).ReadToEndAsync());
                    if (entryType == null || !typeof(Mod).IsAssignableFrom(entryType) ||
                        !(entryType.GetConstructor(new Type[0])?.Invoke(new object[0]) is Mod mod)) return;
                    
                    AddMod(mod);
                    Log.LogDebug($"{mod.Name}({mod.ID}) has been loaded.");
                }
            }
        }
    }
}
