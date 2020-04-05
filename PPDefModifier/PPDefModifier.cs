using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Base.Core;
using Base.Defs;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;

namespace PPDefModifier
{
    // A mod definition. Each mod must have one of 'guid' or 'obj', and must have 'field' and 'value'.
    // Field is a dot-separated path to the field containing the value to set.
    // Comment is ignored but can be used to add a human-readable comment to each entry since Json does not
    // support comments.
    [System.Serializable]
    public class ModifierDefinition
    {
        public string guid;
        public string cls;
        public string field;
        public object value;
        public string comment;
        /// <summary>
        /// A list of field-value pairs to apply to the guid or cls.
        /// </summary>
        public List<ModletStep> modletlist;
    }

    /// <summary>
    /// Modlet step is used to apply multiple changes to a single master definition.
    /// </summary>
    [System.Serializable]
    public class ModletStep
    {
        public string field;
        public object value;
    }

    public class PPDefModifier
    {
        public static string fileName = "Mods/PPDefModifier.json";
        public static string directoryName = "Mods/PPDefModifier";
        public static void Init()
        {
            bool foundMod = false;

            // First search the original config file.
            if (File.Exists(fileName))
            {
                foundMod = true;
                Debug.LogFormat("PPDefModifier: Applying mods in {0}", fileName);
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
                        Debug.LogFormat("PPDefModifier: Applying mods in {0}", f);
                        ModFile m = new ModFile(f);
                        m.ApplyMods();
                    }
                }
            }

            if (!foundMod)
            {
                Debug.Log("PPDefModifier: No config file found.");
            }
        }
    }

    public class ModFile
    {
        public ModFile(string fileName)
        {
            this.fileName_ = fileName;
            this.repo_ = new PPDefRepository(GameUtl.GameComponent<DefRepository>());
        }

        internal ModFile(string fileName, IDefRepository repo)
        {
            this.fileName_ = fileName;
            this.repo_ = repo;
        }

        public void ApplyMods()
        {
            try
            {
                string contents = File.ReadAllText(fileName_);
                List<ModifierDefinition> mods = JsonConvert.DeserializeObject<List<ModifierDefinition>>(contents);
                if (mods == null || mods.Count() == 0)
                {
                    BadMod("Failed to parse mod file");
                }
                foreach (var mod in mods)
                {
                    ValidateModifier(mod);
                    ApplyModifier(mod);
                }
            }
            catch (Exception e)
            {
                Debug.LogFormat("PPDefModifier: Caught exception during mod application: {0}", e.ToString());
            }
        }



        // Check that the mod is well-formed: needs exactly one of guid or obj, and must have a field.
        public void ValidateModifier(ModifierDefinition mod)
        {
            if (mod.guid == null && mod.cls == null)
            {
                BadMod("No guid or cls in mod");
            }
            if (mod.guid != null && mod.cls != null)
            {
                BadMod("Both guid and cls in mod");
            }

            if (mod.field == null && (mod.modletlist == null || mod.modletlist.Count() == 0))
            {
                BadMod("No field or modletlist in mod for {0}", mod.guid ?? mod.cls);
            }

            if (mod.field != null && mod.modletlist != null)
            {
                BadMod("Both field and modletlist in mod for {0}", mod.guid ?? mod.cls);
            }
        }

        public void ApplyModifier(ModifierDefinition mod)
        {
            System.Object obj = null;
            System.Object parent = null;
            int parentArrayIndex = -1;
            Type type = null;

            // If modletlist are present then skip to generating new single modifiers, so that these tests
            // don't get run one more time than needed
            if (mod.modletlist != null)
            {
                // Make a new ModifierDefinition for each modlet in the modlet list and apply it.
                foreach (ModletStep modlet in mod.modletlist)
                {
                    if (modlet.field == null || modlet.value == null)
                    {
                        // Skip any modlets that are malformed. Do this gracefully so we don't break a sequence.
                        //Debug.LogFormat("PPDefModifer: Modlet patch for {0} was missing a field or a class!", mod.guid ?? mod.cls);
                        continue;
                    }
                    //Debug.LogFormat("PPDefModifier: Applying modlet patch to {0}.{1}", mod.guid ?? mod.cls, modlet.field);
                    ApplyModifier(new ModifierDefinition
                    {
                        guid = mod.guid,
                        cls = mod.cls,
                        field = modlet.field,
                        value = modlet.value
                    });
                }

                // Once we're done exit from the function.
                return;
            }

            // Try to find the def if we have a guid
            if (mod.guid != null)
            {
                obj = repo_.GetDef(mod.guid);
                if (obj == null)
                {
                    BadMod("Failed to find def {0}", mod.guid);
                }

                type = obj.GetType();
            }
            else
            {
                string className = mod.cls;
                // Qualify the obj name with the assembly name if one is not provided.
                if (!className.Contains(','))
                {
                    className = className + ", Assembly-CSharp";
                }

                // Find the type of this object. The field to modify will have to be static.
                type = Type.GetType(className);
                if (type == null)
                {
                    BadMod("Failed to find type for class {0}", mod.cls);
                }
            }
            // Try to locate the correct field
            var fields = mod.field.Split('.');

            for (int i = 0; i < fields.Length; ++i)
            {
                string fieldString = fields[i];
                int arrayIndex = -1;

                fieldString = DecomposeArray(fieldString, out arrayIndex);

                FieldInfo field = type.GetField(fieldString);
                if (field == null)
                {
                    BadMod("Could not find field named {0} in type {1}", fieldString, type.Name);
                }

                if (i < (fields.Length - 1))
                {
                    if (arrayIndex >= 0)
                    {
                        IList elems = field.GetValue(obj) as IList;
                        parent = elems;
                        parentArrayIndex = arrayIndex;
                        obj = elems[arrayIndex];
                    }
                    else
                    {
                        parent = obj;
                        obj = field.GetValue(obj);
                        parentArrayIndex = -1;
                    }

                    if (obj == null)
                        BadMod("Could not retrieve object from field {0} in type {1}", fieldString, type.Name);
                    type = obj.GetType();

                    // If the element we are looking at in the field list is a value type then 'obj' is a boxed copy of the value in the repo. We can change it,
                    // but any changes need to be pushed back into the containing object. Record this in a stack of pending copies to make after we're done. These
                    // will be applied after we have finished changing the value and record the value type object (obj), its field info, it's parent object, and
                    // the array index within that parent if it was an array.
                    if (type.IsValueType)
                    {
                        valueTypeStack.Insert(0, new ValueTypeElement { field = field, obj = parent, value = obj, arrayIndex = parentArrayIndex });
                    }
                }
                else
                {
                    if (arrayIndex >= 0)
                        AssignArrayElement(obj, field, arrayIndex, mod.value);
                    else
                        AssignField(obj, field, mod.value);
                }
            }

            // Copy all pending value type objects back into their parents.
            foreach (var v in valueTypeStack)
            {
                if (v.arrayIndex >= 0)
                {
                    IList elems = v.obj as IList;
                    elems[v.arrayIndex] = v.value;
                }
                else
                {
                    v.field.SetValue(v.obj, v.value);
                }
            }
        }

        public string DecomposeArray(string fieldString, out int index)
        {
            int start = fieldString.IndexOf('[');
            if (start < 0)
            {
                index = -1;
                return fieldString;
            }

            int end = fieldString.IndexOf(']');
            if (end < 0)
            {
                BadMod("Bad array element specifier: missing ']'");
            }

            if (!Int32.TryParse(fieldString.Substring(start + 1, end - start - 1), out index))
            {
                BadMod("Bad array index: not an integer");
            }

            if (index < 0)
            {
                BadMod("Bad array index: must be a positive integer");
            }
            return fieldString.Substring(0, start);
        }

        public void AssignField(System.Object obj, FieldInfo field, object value)
        {
            Type fieldType = field.FieldType;

            // Try to convert the value from the file into the type of the field. This may fail.
            try
            {
                System.Object converted = Convert.ChangeType(value, fieldType);
                field.SetValue(obj, converted);
            }
            catch (Exception)
            {
                BadMod("Error converting value to type {0} of field {1}", fieldType.Name, field.Name);
            }
        }

        public void AssignArrayElement(System.Object obj, FieldInfo field, int arrayIndex, object value)
        {
            Type fieldType = field.FieldType;

            if (!fieldType.IsArray)
            {
                BadMod("Field {0} does not have array type", field.Name);
            }

            fieldType = fieldType.GetElementType();

            try
            {
                Array elems = field.GetValue(obj) as Array;
                System.Object converted = Convert.ChangeType(value, fieldType);
                elems.SetValue(converted, arrayIndex);
                field.SetValue(obj, elems);
            }
            catch (Exception)
            {
                BadMod("Error converting value to type {0} of field {1}", fieldType.Name, field.Name);
            }
        }

        private void BadMod(string s, params object[] args)
        {
            string prefix = string.Format("PPDefModifier: {0}: ", fileName_);
            string rest = string.Format(s, args);
            throw new ModException(prefix + rest);
        }

        private string fileName_ { get; set; }
        private IDefRepository repo_ { get; set; }

        private List<ValueTypeElement> valueTypeStack = new List<ValueTypeElement>();

        class ValueTypeElement
        {
            public FieldInfo field;
            public object obj;
            public object value;
            public int arrayIndex = -1;
        }

    }

    internal interface IDefRepository
    {
        object GetDef(string guid);
    }

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

}
