using Microsoft.VisualStudio.TestTools.UnitTesting;
using PPDefModifier;
using System;
using System.Collections.Generic;

namespace PPDefModifierTests
{
    [TestClass]
    public class ApplyModTests
    {
        public class MockRepo : IDefRepository
        {
            public object GetDef(string guid)
            {
                object obj;
                if (dict.TryGetValue(guid, out obj))
                {
                    return obj;
                }
                throw new ArgumentException();
            }

            public void AddDef(string guid, object value)
            {
                dict.Add(guid, value);
            }

            private Dictionary<String, Object> dict = new Dictionary<String,Object>();
        }

        public class MockLogger : ILogger
        {
            public void Error(string msg, params object[] args)
            {
            }

            public void Log(string msg, params object[] args)
            {
            }
        }


        public class TestClass
        {
            public int intValue;
            public double doubleValue;
            public bool boolValue;
            public string stringValue;

            public class Nested
            {
                public int intValue;

                public static int staticIntValue;

                public class AnotherNest
                {
                    public int intValue;
                }

                public AnotherNest anotherNest;

            }

            public Nested nested;
        }


        [TestMethod]
        public void TestSimpleInt()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { intValue = 10 };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "intValue", value = 5 };
            new Mod("SimpleInt", mod, repo).Apply();
            Assert.AreEqual(obj.intValue, 5);
        }

        [TestMethod]
        public void TestSimpleDouble()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { doubleValue = 20.0 };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "doubleValue", value = 50.0 };
            new Mod("SimpleDouble", mod, repo).Apply();
            Assert.AreEqual(obj.doubleValue, 50.0);
        }

        [TestMethod]
        public void TestSimpleBool()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { boolValue = false };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "boolValue", value = 1 };
            new Mod("SimpleBool", mod, repo).Apply();
            Assert.IsTrue(obj.boolValue);
        }

        [TestMethod]
        public void TestSimpleString()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { stringValue = "foo" };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "stringValue", value = "bar" };
            new Mod("SimpleString", mod, repo).Apply();
            Assert.AreEqual(obj.stringValue, "bar");
        }

        [TestMethod]
        public void TestNestedInt()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { intValue = 0 } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "nested.intValue", value = 10 };
            new Mod("NestedInt", mod, repo).Apply();
            Assert.AreEqual(obj.nested.intValue, 10);
        }

        [TestMethod]
        public void TestDoubleNestedInt()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "nested.anotherNest.intValue", value = 10 };
            new Mod("DoubleNestedInt", mod, repo).Apply();
            Assert.AreEqual(obj.nested.anotherNest.intValue, 10);
        }

        [TestMethod]
        public void TestWrongGuid()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "b", field = "nested.anotherNest.intValue", value = 10 };
            Assert.ThrowsException<ArgumentException>(() => new Mod("WrongGuid", mod, repo).Apply());
        }

        [TestMethod]
        public void TestBadField()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "wrong", value = 10 };
            Assert.ThrowsException<ModException>(() => new Mod("BadField", mod, repo).Apply());
        }

        [TestMethod]
        public void TestNonPrimitiveField()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "nested", value = 10 };
            Assert.ThrowsException<ModException>(() => new Mod("NonPrimitiveField", mod, repo).Apply());
        }

        [TestMethod]
        public void TestStaticInt()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { };
            TestClass.Nested.staticIntValue = 0;
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { cls = "PPDefModifierTests.ApplyModTests+TestClass+Nested, PPDefModifierTests", field = "staticIntValue", value = 5 };
            new Mod("StaticInt", mod, repo).Apply();
            Assert.AreEqual(TestClass.Nested.staticIntValue, 5);
        }

        public class ArrayTestClass
        {
            public class Nested
            {
                public int value;
                public double[] nestedValues;
            }

            public Nested[] arr;

            public double[] values;
        }

        [TestMethod]
        public void TestFirstArrayObject()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[0].value", value = 10 };
            new Mod("FirstArray", mod, repo).Apply();
            Assert.AreEqual(obj.arr[0].value, 10);
            Assert.AreEqual(obj.arr[1].value, 8);
            Assert.AreEqual(obj.arr[2].value, 9);
        }

        [TestMethod]
        public void TestArrayObject()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 } , new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[1].value", value = 10 };
            new Mod("Array", mod, repo).Apply();
            Assert.AreEqual(obj.arr[0].value, 7);
            Assert.AreEqual(obj.arr[1].value, 10);
            Assert.AreEqual(obj.arr[2].value, 9);
        }

        [TestMethod]
        public void TestLastArrayObject()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[2].value", value = 10 };
            new Mod("LastArray", mod, repo).Apply();
            Assert.AreEqual(obj.arr[0].value, 7);
            Assert.AreEqual(obj.arr[1].value, 8);
            Assert.AreEqual(obj.arr[2].value, 10);
        }

        [TestMethod]
        public void TestFirstArrayValue()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { values = new double[3] { 7.0, 8.0, 9.0 } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "values[0]", value = 10 };
            new Mod("FirstArrayValue", mod, repo).Apply();
            Assert.AreEqual(obj.values[0], 10.0);
            Assert.AreEqual(obj.values[1], 8.0);
            Assert.AreEqual(obj.values[2], 9.0);
        }

        [TestMethod]
        public void TestLastArrayValue()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { values = new double[3] { 7.0, 8.0, 9.0 } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "values[2]", value = 10 };
            new Mod("LastArrayValue", mod, repo).Apply();
            Assert.AreEqual(obj.values[0], 7.0);
            Assert.AreEqual(obj.values[1], 8.0);
            Assert.AreEqual(obj.values[2], 10.0);
        }

        [TestMethod]
        public void TestMultiArrayValues()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { 
                new ArrayTestClass.Nested { nestedValues = new double[2] { 7.0, 8.0 } }, 
                new ArrayTestClass.Nested { nestedValues = new double[3] { 17.0, 18.0, 19.0 } },
                new ArrayTestClass.Nested { nestedValues = new double[4] { 27.0, 28.0, 29.0, 30.0 } } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[0].nestedValues[1]", value = 10 };
            new Mod("MultiArrayValues", mod, repo).Apply();
            Assert.AreEqual(obj.arr[0].nestedValues[1], 10.0);
        }

        [TestMethod]
        public void TestOutOfBound()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[3].value", value = 10 };
            Assert.ThrowsException<IndexOutOfRangeException> (() => new Mod("OutOfBound", mod, repo).Apply());
        }

        [TestMethod]
        public void TestNegativeIndex()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[-1].value", value = 10 };
            Assert.ThrowsException<ModException>(() => new Mod("NegativeIndex", mod, repo).Apply());
        }

        [TestMethod]
        public void TestNonIntIndex()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[1.0].value", value = 10 };
            Assert.ThrowsException<ModException>(() => new Mod("NonIntIndex", mod, repo).Apply());
        }

        [TestMethod]
        public void TestNonNumberIndex()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[foo].value", value = 10 };
            Assert.ThrowsException<ModException>(() => new Mod("NonNumberIndex", mod, repo).Apply());
        }

        [TestMethod]
        public void TestBadBracket()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "arr[.value", value = 10 };
            Assert.ThrowsException<ModException>(() => new Mod("BadBracket", mod, repo).Apply());
        }

        class NestedStruct
        {
            public struct Nested
            {
                public int Value;

                public struct Further
                {
                    public int Value2;
                }

                public Further further;
            }

            public Nested nested;
            public Nested[] nestedArray;
            public List<Nested> nestedList;
        }

        [TestMethod]
        public void TestStructMember()
        {
            MockRepo repo = new MockRepo();
            NestedStruct obj = new NestedStruct { nested = new NestedStruct.Nested { Value = 0 } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "nested.Value", value = 10 };
            new Mod("NestedStruct", mod, repo).Apply();
            Assert.AreEqual(10, obj.nested.Value);
        }

        [TestMethod]
        public void TestStructInStruct()
        {
            MockRepo repo = new MockRepo();
            NestedStruct obj = new NestedStruct { nested = new NestedStruct.Nested { further = new NestedStruct.Nested.Further { Value2 = 7 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "nested.further.Value2", value = 10 };
            new Mod("StructInStruct", mod, repo).Apply();
            Assert.AreEqual(10, obj.nested.further.Value2);
        }

        [TestMethod]
        public void TestStructArray()
        {
            MockRepo repo = new MockRepo();
            NestedStruct obj = new NestedStruct { nestedArray = new NestedStruct.Nested[2] { new NestedStruct.Nested { Value = 7 }, new NestedStruct.Nested { Value = 8 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "nestedArray[1].Value", value = 10 };
            new Mod("NestedStructArray", mod, repo).Apply();
            Assert.AreEqual(7, obj.nestedArray[0].Value);
            Assert.AreEqual(10, obj.nestedArray[1].Value);
        }

        [TestMethod]
        public void TestStructList()
        {
            MockRepo repo = new MockRepo();
            NestedStruct obj = new NestedStruct { nestedList = new List<NestedStruct.Nested> { new NestedStruct.Nested { Value = 7 }, new NestedStruct.Nested { Value = 8 } } };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition { guid = "a", field = "nestedList[1].Value", value = 10 };
            new Mod("NestedStructList", mod, repo).Apply();
            Assert.AreEqual(7, obj.nestedList[0].Value);
            Assert.AreEqual(10, obj.nestedList[1].Value);
        }

        [TestMethod]
        public void TestModletList()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { intValue = 10, boolValue = false };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition {
                guid = "a",
                modletlist = new List<ModletStep> {
                    new ModletStep { field = "intValue", value = 20 },
                    new ModletStep { field = "boolValue", value = true}
                }
            };
            ModFile f = new ModFile("TestModletList", repo, new MockLogger());
            f.ApplyMod(mod);
            Assert.AreEqual(obj.intValue, 20);
            Assert.IsTrue(obj.boolValue);
        }

        [TestMethod]
        public void TestModletListWithMalformedSteps()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { intValue = 10, boolValue = false };
            repo.AddDef("a", obj);
            ModDefinition mod = new ModDefinition
            {
                guid = "a",
                modletlist = new List<ModletStep> {
                    new ModletStep { field = "intValue", value = 20 },
                    new ModletStep { field = null, value = 20 },
                    new ModletStep { field = "NoValue", value = null },
                    new ModletStep { field = null, value = null },
                    new ModletStep { field = "boolValue", value = true}
                }
            };
            ModFile f = new ModFile("ModlietListWithMalformedSteps", repo, new MockLogger());
            f.ApplyMod(mod);
            Assert.AreEqual(obj.intValue, 20);
            Assert.IsTrue(obj.boolValue);
        }

    }
}

