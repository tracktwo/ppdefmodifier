using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPDefModifier;
using System;
using System.Collections.Generic;
using System.IO;

namespace PPDefModifierTests
{
    using ModnixAction = Dictionary<string, object>;
    using MockRepo  = ApplyModTests.MockRepo;
    using MockLogger= ApplyModTests.MockLogger;
    using TestClass = ApplyModTests.TestClass;
    using PPDefMod  = PPDefModifier.PPDefModifier;

    [TestClass]
    public class ActionModTests
    {
        public static class MockApi
        {
            public static object mockApiPath;

            public static object mockApiVersion;

            public static object Api(string api, object _)
            {
                switch (api?.ToLowerInvariant())
                {
                    case "path":
                        return mockApiPath;
                    case "version":
                        return mockApiVersion;
                    default:
                        return null;
                }
            }
        }

        public ModDefinition JsonToModDef(string json)
        {
            ModDefinition mod = new ModDefinition();
            ModnixAction action = JsonConvert.DeserializeObject<ModnixAction>(json);
            PPDefMod.ConvertActionToMod(action, mod);
            return mod;
        }

        [TestMethod]
        public void ConversionTest()
        {
            ModDefinition mod = JsonToModDef(@"{
                guid: ""g"",
                cls: ""c"",
                field: ""f"",
                value: -43.21,
                comment: ""//"",
                flags: [""foo"",""bar""],
                modletlist : [
                    { ""field"": ""txt"", ""value"": ""mv1"" },
                    { ""field"": ""int"", ""value"": 1234 },
                    { ""field"": ""dec"", ""value"": 12.34 },
                    { ""field"": ""bol"", ""value"": true },
                ],
            }");

            Assert.AreEqual("g", mod.guid, "guid");
            Assert.AreEqual("c", mod.cls, "cls");
            Assert.AreEqual( "f", mod.field, "field" );
            Assert.AreEqual(-43.21, mod.value, "value");
            Assert.AreEqual("//", mod.comment, "comment");
            CollectionAssert.AreEqual(new string[]{ "foo", "bar" }, mod.flags, "flags");
            Assert.AreEqual(4, mod.modletlist?.Count, "modletlist");
            Assert.AreEqual("txt", mod.modletlist[0].field, "modletlist[0].field");
            Assert.AreEqual("mv1", mod.modletlist[0].value, "modletlist[0].value");
            Assert.AreEqual("int", mod.modletlist[1].field, "modletlist[1].field");
            Assert.AreEqual(1234l, mod.modletlist[1].value, "modletlist[1].value");
            Assert.AreEqual("dec", mod.modletlist[2].field, "modletlist[2].field");
            Assert.AreEqual(12.34, mod.modletlist[2].value, "modletlist[2].value");
            Assert.AreEqual("bol", mod.modletlist[3].field, "modletlist[3].field");
            Assert.AreEqual(true , mod.modletlist[3].value, "modletlist[3].value");
        }

        public void SimulateMods<T>(T obj, string json) {
            string tempFile = Path.GetTempFileName();
            try
            {
                MockApi.mockApiPath = tempFile;
                File.WriteAllText(tempFile, json);
                MockRepo repo = new MockRepo();
                repo.AddDef("a", obj);
                new ModFile(tempFile, repo, new MockLogger()).ApplyMods();
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void NonModnixApplyActionFile()
        {
            PPDefMod.api = null;
            TestClass obj = new TestClass();
            SimulateMods(obj, "[{flags:[\"Actions\"],guid:\"a\",field:\"intValue\",value:123}]");
            Assert.AreEqual(123, obj.intValue); // Modified
        }

        [TestMethod]
        public void Modnix2ApplyActionFile()
        {
            MockApi.mockApiVersion = new Version(2,5);
            PPDefMod.api = MockApi.Api;
            TestClass obj = new TestClass();
            SimulateMods(obj, "[{flags:[\"Actions\"],guid:\"a\",field:\"doubleValue\",value:4.5}]");
            Assert.AreEqual(4.5, obj.doubleValue); // Modified
        }

        [TestMethod]
        public void Modnix3ApplyActionFile()
        {
            MockApi.mockApiVersion = new Version(3,1);
            PPDefMod.api = MockApi.Api;
            TestClass obj = new TestClass();
            SimulateMods(obj, "[{flags:[\"Actions\"],guid:\"a\",field:\"boolValue\",value:true}]");
            Assert.AreEqual(false, obj.boolValue); // NOT modified
        }

        [TestMethod]
        public void Modnix3ActionMod()
        {
            MockApi.mockApiVersion = new Version(3,1);
            PPDefMod.api = MockApi.Api;
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass();
            repo.AddDef("a", obj);

            ModDefinition mod = JsonToModDef("{flags:[\"Actions\"],guid:\"a\",field:\"boolValue\",value:true}");
            new ModFile("ActionMod.Simple", repo, new MockLogger()).ApplyMod(mod);
            Assert.AreEqual(true, obj.boolValue); // Modified

            mod = JsonToModDef("{flags:[\"Actions\"],guid:\"a\",modletlist:[" +
               "{field:\"intValue\",value:1234}," +
               "{field:\"doubleValue\",value:12.34}," +
               "{field:\"boolValue\",value:false}," +
               "{field:\"stringValue\",value:\"foobar\"}]}");
            new ModFile("ActionMod.Modlet", repo, new MockLogger()).ApplyMod(mod);
            Assert.AreEqual(1234, obj.intValue);
            Assert.AreEqual(12.34, obj.doubleValue);
            Assert.AreEqual(false, obj.boolValue);
            Assert.AreEqual("foobar", obj.stringValue);
        }
    }
}
