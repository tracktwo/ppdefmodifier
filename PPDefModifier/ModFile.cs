using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Base.Core;
using Base.Defs;
using Newtonsoft.Json;

namespace PPDefModifier
{
    /// <summary>
    /// An object representing a mod config file. The config file is a list of one or more mods.
    /// </summary>
    internal class ModFile
    {
        /// <summary>
        /// Construct a new mod file object from the given filename.
        /// </summary>
        /// <param name="fileName">Path to the mod config file</param>
        /// <param name="api">A (possibly null) callback object for Modnix.</param>
        public ModFile(string fileName)
        {
            this.fileName = fileName;
            this.repo = new PPDefRepository(GameUtl.GameComponent<DefRepository>());
            this.logger = PPDefLogger.logger;
        }

        /// <summary>
        /// Unit test constructor: accepts a repo and logger object to use instead of creating
        /// ones appropriate for the game.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="repo"></param>
        /// <param name="logger"></param>
        internal ModFile(string filename, IDefRepository repo, ILogger logger)
        {
            this.fileName = filename;
            this.repo = repo;
            this.logger = logger;
        }

        /// <summary>
        ///  Apply one mod
        /// </summary>
        /// <param name="def">The mod def to apply</param>
        public void ApplyMod(ModDefinition def)
        {
            Mod mod = new Mod(fileName, def, repo);
            mod.Validate();

            // If the mod contains a modletlist expand it out into individual mods and apply each in turn.
            if (def.modletlist != null)
            {
                // The index
                int modletIndex = 1;
                // Make a new ModifierDefinition for each modlet in the modlet list and apply it.
                foreach (ModletStep modlet in def.modletlist)
                {
                    if (modlet.field == null || modlet.value == null)
                    {
                        // Skip any modlets that are malformed. Do this gracefully so we don't break a sequence.
                        logger.Error("Modlet entry {0} is missing field or value in {1}", modletIndex, def.GetModName());
                        continue;
                    }
                    ModDefinition tmpDef = new ModDefinition
                    {
                        guid = def.guid,
                        cls = def.cls,
                        field = modlet.field,
                        value = modlet.value
                    };

                    Mod tmpMod = new Mod(fileName, tmpDef, repo);

                    try
                    {
                        tmpMod.Apply();
                    }
                    catch (Exception e)
                    {
                        logger.Error("Error applying modlet field {0} in {1}: {2}", modlet.field, def.GetModName(), e.ToString());
                    }

                    logger.Log("Successfully applied modlet field {0} in {1}", modlet.field, def.GetModName());
                    ++modletIndex;
                }

                // Once we're done exit from the function.
                return;
            }
            else
            {
                mod.Apply();
                logger.Log("Successfully applied {0}", def.GetModName());
            }
        }

        /// <summary>
        /// Reads the configuration file and applies all mods found within it.
        /// </summary>
        public void ApplyMods()
        {
            string contents = File.ReadAllText(fileName);
            List<ModDefinition> mods = null;
            try
            {
                mods = JsonConvert.DeserializeObject<List<ModDefinition>>(contents);
            }
            catch(Exception e)
            {
                logger.Error("Caught exception reading file {0}: {1}", fileName, e.ToString());
                return;
            }

            if (mods == null || mods.Count() == 0)
            {
                logger.Error("Failed to parse mod file {0}", fileName);
                return;
            }

            if (mods[0].HasFlag("actions") && PPDefModifier.api?.Invoke("version","modnix") is Version ver && ver >= new Version(3,0,2021,0125))
            {
                logger.Log("Deferring {0} to Modnix Actions: Found 'Actions' flag'.", fileName);
                return;
            }

            logger.Log("Applying mods in {0}", fileName);
            foreach (var def in mods)
            {
                try
                {
                    ApplyMod(def);
                }
                catch (Exception e)
                {
                    logger.Error("Error applying mod {0}: {1}", def.GetModName(), e.ToString());
                }
            }
        }

        /// <summary>
        /// The mod filename
        /// </summary>
        private string fileName { get; set; }

        /// <summary>
        /// The def repo to modify.
        /// </summary>
        private IDefRepository repo { get; set; }

        /// <summary>
        /// The logger to use.
        /// </summary>
        private ILogger logger { get; set; }

    }
}

