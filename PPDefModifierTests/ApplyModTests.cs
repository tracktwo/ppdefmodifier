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

        public class TestClass
        {
            public int intValue;
            public double doubleValue;
            public bool boolValue;

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
            ModFile m = new ModFile("SimpleInt", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "intValue", value = 5 };
            m.ApplyModifier(mod);
            Assert.AreEqual(obj.intValue, 5);
        }

        [TestMethod]
        public void TestSimpleDouble()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { doubleValue = 20.0 };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("SimpleDouble", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "doubleValue", value = 50.0 };
            m.ApplyModifier(mod);
            Assert.AreEqual(obj.doubleValue, 50.0);
        }

        [TestMethod]
        public void TestSimpleBool()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { boolValue = false };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("SimpleBool", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "boolValue", value = 1 };
            m.ApplyModifier(mod);
            Assert.IsTrue(obj.boolValue);
        }

        [TestMethod]
        public void TestNestedInt()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { intValue = 0 } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("NestedInt", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "nested.intValue", value = 10 };
            m.ApplyModifier(mod);
            Assert.AreEqual(obj.nested.intValue, 10);
        }

        [TestMethod]
        public void TestDoubleNestedInt()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("DoubleNestedInt", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "nested.anotherNest.intValue", value = 10 };
            m.ApplyModifier(mod);
            Assert.AreEqual(obj.nested.anotherNest.intValue, 10);
        }

        [TestMethod]
        public void TestWrongGuid()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("WrongGuid", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "b", field = "nested.anotherNest.intValue", value = 10 };
            Assert.ThrowsException<ArgumentException>(() => m.ApplyModifier(mod));
        }

        [TestMethod]
        public void TestBadField()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("BadField", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "wrong", value = 10 };
            Assert.ThrowsException<ModException>(() => m.ApplyModifier(mod));
        }

        [TestMethod]
        public void TestNonPrimitiveField()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { nested = new TestClass.Nested { anotherNest = new TestClass.Nested.AnotherNest { intValue = 0 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("NonPrimitiveField", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "nested", value = 10 };
            Assert.ThrowsException<ModException>(() => m.ApplyModifier(mod));
        }

        [TestMethod]
        public void TestStaticInt()
        {
            MockRepo repo = new MockRepo();
            TestClass obj = new TestClass { };
            TestClass.Nested.staticIntValue = 0;
            repo.AddDef("a", obj);
            ModFile m = new ModFile("StaticInt", repo);
            ModifierDefinition mod = new ModifierDefinition { cls = "PPDefModifierTests.ApplyModTests+TestClass+Nested, PPDefModifierTests", field = "staticIntValue", value = 5 };
            m.ApplyModifier(mod);
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
            ModFile m = new ModFile("FirstArray", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[0].value", value = 10 };
            m.ApplyModifier(mod);
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
            ModFile m = new ModFile("Array", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[1].value", value = 10 };
            m.ApplyModifier(mod);
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
            ModFile m = new ModFile("LastArray", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[2].value", value = 10 };
            m.ApplyModifier(mod);
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
            ModFile m = new ModFile("FirstArrayValue", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "values[0]", value = 10 };
            m.ApplyModifier(mod);
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
            ModFile m = new ModFile("FirstArrayValue", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "values[2]", value = 10 };
            m.ApplyModifier(mod);
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
            ModFile m = new ModFile("FirstArrayValue", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[0].nestedValues[1]", value = 10 };
            m.ApplyModifier(mod);
            Assert.AreEqual(obj.arr[0].nestedValues[1], 10.0);
        }

        [TestMethod]
        public void TestOutOfBound()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("OutOfBound", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[3].value", value = 10 };
            Assert.ThrowsException<IndexOutOfRangeException> (() => m.ApplyModifier(mod));
        }

        [TestMethod]
        public void TestNegativeIndex()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("NegativeIndex", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[-1].value", value = 10 };
            Assert.ThrowsException<ModException>(() => m.ApplyModifier(mod));
        }

        [TestMethod]
        public void TestNonIntIndex()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("NonIntIndex", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[1.0].value", value = 10 };
            Assert.ThrowsException<ModException>(() => m.ApplyModifier(mod));
        }

        [TestMethod]
        public void TestNonNumberIndex()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("NonNumberIndex", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[foo].value", value = 10 };
            Assert.ThrowsException<ModException>(() => m.ApplyModifier(mod));
        }

        [TestMethod]
        public void TestBadBracket()
        {
            MockRepo repo = new MockRepo();
            ArrayTestClass obj = new ArrayTestClass { arr = new ArrayTestClass.Nested[3] { new ArrayTestClass.Nested { value = 7 }, new ArrayTestClass.Nested { value = 8 }, new ArrayTestClass.Nested { value = 9 } } };
            repo.AddDef("a", obj);
            ModFile m = new ModFile("BadBracket", repo);
            ModifierDefinition mod = new ModifierDefinition { guid = "a", field = "arr[.value", value = 10 };
            Assert.ThrowsException<ModException>(() => m.ApplyModifier(mod));
        }

    }
}

