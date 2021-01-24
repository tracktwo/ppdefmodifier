using System;
using System.IO;
using Base.Defs;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PPDefModifier
{
    using ModnixCallback = Func<string, object, object>;
    using ModnixAction = Dictionary<string, object>;

    /// <summary>
    /// An exception class to represent a failure to apply a mod.
    /// </summary>
    public class ModException : Exception
    {
        public ModException(string fmt, params object[] args)
        {
            str = String.Format(fmt, args);
        }

        public override string ToString()
        {
            return str;
        }

        private string str;
    }

    /// <summary>
    /// A mod definition. Each mod must have one of 'guid' or 'obj', and must have 'field' and 'value'.
    /// Field is a dot-separated path to the field containing the value to set.
    /// Comment is ignored but can be used to add a human-readable comment to each entry since Json does not
    /// support comments (although some implementations do).
    /// </summary>
    [System.Serializable]
    public class ModDefinition
    {
        /// <summary>
        /// The def GUID this mod applies to, for def mods.
        /// </summary>
        public string guid;
        /// <summary>
        /// The class name this mod applies to, for static field mods.
        /// </summary>
        public string cls;
        /// <summary>
        /// The field within the def or class to change.
        /// </summary>
        public string field;
        /// <summary>
        /// The new value for this field
        /// </summary>
        public object value;
        /// <summary>
        /// A comment describing the mod
        /// </summary>
        public string comment;
        /// <summary>
        /// A list of field-value pairs to apply to the guid or cls.
        /// </summary>
        public List<ModletStep> modletlist;

        public String GetModName()
        {
            if (comment != null)
            {
                return comment;
            }
            else if (guid != null)
            {
                return string.Format("guid {0}", guid);
            }
            else if (cls != null)
            {
                return string.Format("class {0}", cls);
            }

            return "<unknown>";
        }
    }

    /// <summary>
    /// Interface abstracting the def repository for the game to allow
    /// unit testing.
    /// </summary>
    public interface IDefRepository
    {
        object GetDef(string guid);
    }

    /// <summary>
    /// An implementation wrapping the game's def repository.
    /// </summary>
    internal class PPDefRepository : IDefRepository
    {
        public PPDefRepository(DefRepository repo)
        {
            this.repo = repo;
        }
        public object GetDef(string guid)
        {
            return repo.GetDef(guid);
        }

        DefRepository repo { get; set; }
    }

    /// <summary>
    ///  Main entry point to PPDefModifier.
    /// </summary>
    public class PPDefModifier
    {
        /// <summary>
        /// The legacy main config file name.
        /// </summary>
        public static string fileName = "Mods/PPDefModifier.json";

        /// <summary>
        /// The directory to search for mod configurations.
        /// </summary>
        public static string directoryName = "Mods/PPDefModifier";

        /// <summary>
        /// Entry point for Modnix.
        /// </summary>
        public static void MainMod(ModnixCallback api = null)
        {
            PPDefModifier.api = api;
            PPDefLogger.SetLogger(api);

            bool foundMod = false;
            // First search the original config file.
            if (File.Exists(fileName))
            {
                foundMod = true;
                ModFile f = new ModFile(fileName);
                f.ApplyMods();
            }

            // If there is a config directory, process all json files within it.
            if (Directory.Exists(directoryName))
            {
                var files = Directory.GetFiles(directoryName, "*.json", SearchOption.AllDirectories);
                if (files != null)
                {
                    foundMod = true;
                    foreach (var f in files)
                    {
                        ModFile m = new ModFile(f);
                        m.ApplyMods();
                    }
                }
            }

            if (!foundMod)
            {
                logger.Log("PPDefModifier: No config files found.");
            }
        }

        /// <summary>
        /// Entry point for PPML delegates to MainMod.
        /// </summary>
        public static void Init() => MainMod();

        internal static ModnixCallback api;

        private static ILogger logger => PPDefLogger.logger;

        /// <summary>
        /// Map of mod id to ModFile.
        /// </summary>
        private static readonly Dictionary<string, ModFile> actionfiles = new Dictionary<string, ModFile>();

        /// <summary>
        /// Mod id of last action. Used to detect mod switching.
        /// </summary>
        private static string lastactionmod;

        /// <summary>
        /// Entry point for Modnix 3 actions.
        /// Action syntax is the same as traditional PPDefModifier mods.
        /// </summary>
        public static bool ActionMod(string modid, ModnixAction action)
        {
            if (action == null || (!action.ContainsKey("cls") && !action.ContainsKey("guid")))
            {
                return false;
            }
            if (!action.ContainsKey("value") && !action.ContainsKey("modletlist"))
            {
                return false;
            }

            ModFile modfile = GetModFile(modid);
            if (modid != lastactionmod)
            {
                logger.Log("Applying mod actions in {0}", modfile );
                lastactionmod = modid;
            }

            ModDefinition def = new ModDefinition();
            try
            {
                ConvertActionToMod(action, def);
                modfile.ApplyMod(def);
                return true;
            }
            catch (Exception e)
            {
                logger.Error("Error applying mod action {0}: {1}", def.GetModName(), e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Create or fetch a ModFile by modid.
        /// The ModFile will have its filename set to the acting mod's path.
        /// </summary>
        private static ModFile GetModFile(string modid)
        {
            if (actionfiles.TryGetValue( modid, out ModFile modfile ))
            {
                return modfile;
            }
            string path = api?.Invoke( "path", modid )?.ToString() ?? modid;
            return actionfiles[modid] = new ModFile(path);
        }

        /// <summary>
        /// Convert and copy fields from a ModnixAction to a ModDefinition.
        /// </summary>
        private static void ConvertActionToMod(ModnixAction action, ModDefinition def)
        {
            def.guid = action.GetText("guid");
            def.cls = action.GetText("cls");
            def.field = action.GetText("field");
            def.comment = action.GetText("comment");
            action.TryGetValue("value", out def.value);
            if (action.TryGetValue("modletlist", out object mlist) && mlist is JArray list)
            {
                def.modletlist = new List<ModletStep>();
                foreach ( var item in list )
                {
                    if (item is JObject modlet)
                    {
                        var step = new ModletStep
                        {
                           field = modlet.GetText("field"),
                           value = modlet.GetText("value"),
                        };
                        def.modletlist.Add(step);
                    }
                }
            }
        }
    }

    internal static class Tools {
        internal static string GetText<T>(this IDictionary<string,T> action, string key)
        {
            if (action.TryGetValue(key, out T value))
            {
                return value?.ToString();
            }
            return null;
        }
    }
}
