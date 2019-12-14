using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Base.Core;
using Base.Defs;
using System.Reflection;
using Newtonsoft.Json;

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
        public double value;
        public string comment;
    }

    public class PPDefModifier
    {
        public static string fileName = "Mods/PPDefModifier.json";
        public static void Init()
        {
            // Parse the config file into 
            if (File.Exists(fileName))
            {
                try
                {
                    string contents = File.ReadAllText(fileName);
                    List<ModifierDefinition> mods = JsonConvert.DeserializeObject<List<ModifierDefinition>>(contents);
                    if (mods == null || mods.Count() == 0)
                    {
                        Debug.Log("Failed to parse mod file.");
                    }
                    foreach (var mod in mods)
                    {
                        if (ValidateModifier(mod))
                        {
                            ApplyModifier(mod);
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.LogFormat("PPDefModifier: Caught exception during json read or parse: {0}", e.ToString());
                }
            }
            else
            {
                Debug.Log("PPDefModifier: No config file found.");
            }
        }

        // Check that the mod is well-formed: needs exactly one of guid or obj, and must have a field.
        public static bool ValidateModifier(ModifierDefinition mod)
        {
            if (mod.guid == null && mod.cls == null)
            {
                Debug.LogFormat("PPDefModifier: No guid or cls in mod");
                return false;
            }
            if (mod.guid != null && mod.cls != null)
            {
                Debug.LogFormat("PPDefModifier: Both guid and cls in mod");
                return false;
            }
            if (mod.field == null)
            {
                Debug.LogFormat("PPDefModifier: No field in mod for {0}", mod.guid ?? mod.cls);
                return false;
            }

            return true;
        }

        public static void ApplyModifier(ModifierDefinition mod)
        {
            System.Object obj = null;
            Type type = null;

            // Try to find the def if we have a guid
            if (mod.guid != null)
            {
                obj = GameUtl.GameComponent<DefRepository>().GetDef(mod.guid);
                if (obj == null)
                {
                    Debug.LogFormat("Failed to find def {0}", mod.guid);
                    return;
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
                    Debug.LogFormat("Failed to find type for class {0}", mod.cls);
                    return;
                }
            }
            // Try to locate the correct field
            var fields = mod.field.Split('.');

            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo field = type.GetField(fields[i]);
                if (field == null)
                {
                    Debug.LogFormat("PPDefModifier: Could not find field named {0} in type {1}", fields[i], type.Name);
                    return;
                }

                if (i < (fields.Length - 1))
                {
                    obj = field.GetValue(obj);
                    if (obj == null)
                        Debug.LogFormat("PPDefModifier: Could not retrieve object from field {0} in type {1}", fields[i], type.Name);
                }
                else
                {
                    AssignField(obj, field, mod.value);
                }
            }
        }

        public static void AssignField(System.Object obj, FieldInfo field, double value)
        {
            Type fieldType = field.FieldType;
            if (!fieldType.IsPrimitive)
            {
                Debug.LogFormat("PPDefModifier: Field {0} does not have primitive type");
                return;
            }

            // Try to convert the value from the file into the type of the field. This may fail.
            try
            {
                System.Object converted = Convert.ChangeType(value, fieldType);
                field.SetValue(obj, converted);
            }
            catch(Exception)
            {
                Debug.LogFormat("PPDefModifier: Error converting value to type {0} of field {1}", fieldType.Name, field.Name);
            }
        }
    }
}
