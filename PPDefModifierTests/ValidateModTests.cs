using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PPDefModifier;
using System.Collections.Generic;

namespace PPDefModifierTests
{
    [TestClass]
    public class ValidateModTests
    {
        [TestMethod]
        public void NoGuid()
        {
            ModFile m = new ModFile("NoGuid", null);
            ModifierDefinition def = new ModifierDefinition { guid = null, cls = null, comment = null, value = 0, field = null };
            Assert.ThrowsException<ModException>(() => m.ValidateModifier(def));
        }

        [TestMethod]
        public void BothGuidAndCls()
        {
            ModFile m = new ModFile("BothGuidAndClass", null);
            ModifierDefinition def = new ModifierDefinition { guid = "a", cls = "b", comment = null, value = 0, field = null };
            Assert.ThrowsException<ModException>(() => m.ValidateModifier(def));
        }


        [TestMethod]
        public void BothFieldAndModletlist()
        {
            ModFile m = new ModFile("BothFieldAndModletlist", null);
            ModifierDefinition def = new ModifierDefinition { guid = null, cls = "c", comment = null, value = 0, field = "foo", modletlist = new List<ModletStep>() };
            Assert.ThrowsException<ModException>(() => m.ValidateModifier(def));
        }


        [TestMethod]
        public void NoField()
        {
            ModFile m = new ModFile("NoField", null);
            ModifierDefinition def = new ModifierDefinition { guid = "a", cls = null, comment = null, value = 0, field = null };
            Assert.ThrowsException<ModException>(() => m.ValidateModifier(def));
        }

        [TestMethod]
        public void ValidGuid()
        {
            ModFile m = new ModFile("ValidGuid", null);
            ModifierDefinition def = new ModifierDefinition { guid = "a", cls = null, comment = null, value = 0, field = "foo" };
            m.ValidateModifier(def);
        }

        [TestMethod]
        public void ValidCls()
        {
            ModFile m = new ModFile("ValidGuid", null);
            ModifierDefinition def = new ModifierDefinition { guid = null, cls = "c", comment = null, value = 0, field = "foo" };
            m.ValidateModifier(def);
        }

    }
}
