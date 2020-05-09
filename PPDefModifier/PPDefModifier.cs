using System;
using System.IO;
using UnityEngine;
using Base.Defs;
using System.Collections.Generic;

namespace PPDefModifier
{
    using ModnixCallback = Func<string, object, object>;

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

    public interface ILogger
    {
        void Log(String msg, params object[] args);
        void Error(String msg, params object[] args);
    }

    public class ModnixLogger : ILogger
    { 
        public ModnixLogger(ModnixCallback api)
        {
            this.api = api;
        }

        public void Log(String msg, params object[] args)
        {
            api.Invoke("log info", string.Format(msg, args));
        }

        public void Error(String msg, params object[] args)
        {
            api.Invoke("log error", string.Format(msg, args));
        }

        ModnixCallback api { get; set; }
    }

    public class UnityLogger : ILogger
    {
        public UnityLogger() { }

        public void Log(String msg, params object[] args)
        {
            string str = string.Format(msg, args);
            Debug.LogFormat("PPDefModifier: {0}", str);
        }

        public void Error(String msg, params object[] args)
        {
            string str = string.Format(msg, args);
            Debug.LogErrorFormat("PPDefModifier: {0}", str);
        }
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
        /// Entry point for Modnix
        /// </summary>
        public static void MainMod(ModnixCallback api = null)
        {
            bool foundMod = false;
            // First search the original config file.
            if (File.Exists(fileName))
            {
                foundMod = true;
                ModFile f = new ModFile(fileName, api);
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
                        ModFile m = new ModFile(f, api);
                        m.ApplyMods();
                    }
                }
            }

            if (!foundMod)
            {
                ILogger logger = api != null ? (ILogger)(new ModnixLogger(api)) : new UnityLogger();
                logger.Log("PPDefModifier: No config files found.", api);
            }
        }

        /// <summary>
        /// Entry point for PPML delegates to MainMod.
        /// </summary>
        public static void Init() => MainMod();
    }
}

