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
            ModDefinition def = new ModDefinition { guid = null, cls = null, comment = null, value = 0, field = null };
            Assert.ThrowsException<ModException>(() => new Mod("NoGuid", def, null).Validate());
        }

        [TestMethod]
        public void BothGuidAndCls()
        {
            ModDefinition def = new ModDefinition { guid = "a", cls = "b", comment = null, value = 0, field = null };
            Assert.ThrowsException<ModException>(() => new Mod("BothGuidAndClass", def, null).Validate());
        }


        [TestMethod]
        public void BothFieldAndModletlist()
        {
            ModDefinition def = new ModDefinition { guid = null, cls = "c", comment = null, value = 0, field = "foo", modletlist = new List<ModletStep>() };
            Assert.ThrowsException<ModException>(() => new Mod("BothFieldAndModletList", def, null).Validate());
        }


        [TestMethod]
        public void NoField()
        {
            ModDefinition def = new ModDefinition { guid = "a", cls = null, comment = null, value = 0, field = null };
            Assert.ThrowsException<ModException>(() => new Mod("NoField", def, null).Validate());
        }

        [TestMethod]
        public void ValidGuid()
        {
            ModDefinition def = new ModDefinition { guid = "a", cls = null, comment = null, value = 0, field = "foo" };
            new Mod("ValidGuid", def, null).Validate();
        }

        [TestMethod]
        public void ValidCls()
        {
            ModDefinition def = new ModDefinition { guid = null, cls = "c", comment = null, value = 0, field = "foo" };
            new Mod("ValidCls", def, null).Validate();
        }

    }
}
