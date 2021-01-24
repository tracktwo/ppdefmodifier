using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace PPDefModifier
{
    // Represents a single mod to apply.
    public class Mod
    {
        /// <summary>
        /// Construct a new mod object
        /// </summary>
        /// <param name="filename">The name of the containing configuration file, for error and log messages.</param>
        /// <param name="def">The ModDefinition deserialized from the configuration file.</param>
        /// <param name="repo">The def repo into which to apply the change</param>
        public Mod(String filename, ModDefinition def, IDefRepository repo)
        {
            this.filename = filename;
            this.def = def;
            this.repo = repo;
        }

        /// <summary>
        /// Validate that the mod is well-formed. Returns on success or throws a ModException if the mod fails to
        /// validate, containing the reason for the failure.
        /// </summary>
        public void Validate()
        {
            // Check that the mod is well-formed: needs exactly one of guid or obj, and must have a field.
            // Throws a ModException with details of the mod error if it is not valid.
            if (def.guid == null && def.cls == null)
            {
                BadMod("No guid or cls in mod");
            }
            if (def.guid != null && def.cls != null)
            {
                BadMod("Both guid and cls in mod");
            }

            if (def.field == null && (def.modletlist == null || def.modletlist.Count == 0))
            {
                BadMod("No field or modletlist in mod for {0}", def.guid ?? def.cls);
            }

            if (def.field != null && def.modletlist != null)
            {
                BadMod("Both field and modletlist in mod for {0}", def.guid ?? def.cls);
            }
        }

        /// <summary>
        /// Apply this mod. Should not be called for mods involving a modletlist: these should be unpacked into distinct
        /// mods before applying for better error recovery.
        /// </summary>
        public void Apply()
        {
            System.Object obj = null;
            System.Object parent = null;
            int parentArrayIndex = -1;
            Type type = null;

            if (def.modletlist != null)
            {
                // In order to gracefully recover from bad entries in the list the caller should have exploded the modletlist
                // into distinct mods before calling apply.
                throw new Exception("Unexpanded modletlist passed to Apply()");
            }

            // Try to find the def if we have a guid
            if (def.guid != null)
            {
                obj = repo.GetDef(def.guid);
                if (obj == null)
                {
                    BadMod("Failed to find def {0}", def.guid);
                }

                type = obj.GetType();
            }
            else
            {
                string className = def.cls;
                // Qualify the obj name with the assembly name if one is not provided.
                if (!className.Contains(","))
                {
                    className += ", Assembly-CSharp";
                }

                // Find the type of this object. The field to modify will have to be static.
                type = Type.GetType(className);
                if (type == null)
                {
                    BadMod("Failed to find type for class {0}", def.cls);
                }
            }
            // Try to locate the correct field
            var fields = def.field.Split('.');

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
                    {
                        BadMod("Could not retrieve object from field {0} in type {1}", fieldString, type.Name);
                    }
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
                        AssignArrayElement(obj, field, arrayIndex, def.value);
                    else
                        AssignField(obj, field, def.value);
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

        /// <summary>
        /// Given a field name that potentially includes an optional array index decompose the
        /// field into the name and the index within the array bounds.
        ///
        /// e.g. given "someField" returns "someField" and sets index to -1.
        ///      given "indexedField[2]" returns "indexedField" and sets index to 2.
        /// </summary>
        /// <param name="fieldString">The field name, optionally including an array index.</param>
        /// <param name="index">Out param containing the array index of the field or -1 if it does not have one.</param>
        /// <returns>The name of the field with the array portion stripped.</returns>
        private string DecomposeArray(string fieldString, out int index)
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

        /// <summary>
        /// Assign 'value' to 'obj.field'. Will attempt conversion on the given value to the appropriate
        /// type of the field.
        /// </summary>
        /// <param name="obj">The object to modify</param>
        /// <param name="field">The field to assign within the object obj</param>
        /// <param name="value">The value to assign to this field, possibly after conversion</param>
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

        /// <summary>
        /// Assign 'value' to the element 'arayIndex' in 'obj.field'. Similar to AssignField except
        /// for arrays.
        /// </summary>
        /// <param name="obj">The object to modify</param>
        /// <param name="field">The field to assign within the object obj. Must be an array.</param>
        /// <param name="arrayIndex">The array index to modify</param>
        /// <param name="value">The value to assign to this field element.</param>
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

        /// <summary>
        /// Throw an exception indicating this mod appliation has failed.
        /// </summary>
        /// <param name="s">A string indicating the reason for failure</param>
        /// <param name="args">Format arguments for s</param>
        private void BadMod(string s, params object[] args)
        {
            throw new ModException(s, args);
        }

        // A stack of value type objects we have encountered while processing a mod field name.
        // These need to be re-assigned into their parents after modification.
        private List<ValueTypeElement> valueTypeStack = new List<ValueTypeElement>();
        class ValueTypeElement
        {
            public FieldInfo field;
            public object obj;
            public object value;
            public int arrayIndex = -1;
        }

        // The mod we are applying
        private ModDefinition def { get; set; }

        // The name of the config file containing this mod
        private string filename { get; set; }

        // The def repository we're modifying
        private IDefRepository repo { get; set; }

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


}


