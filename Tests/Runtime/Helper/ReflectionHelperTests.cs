namespace UnityHelpers.Tests.Tests.Runtime.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Core.Helper;
    using NUnit.Framework;

    public struct TestStruct
    {
        public int intValue;
    }

    public sealed class TestClass
    {
        public int intValue;
    }

    public sealed class ReflectionHelperTests
    {
        internal const int NumTries = 1_000;

        private readonly Random _random = new();

        // TODO: Test on static fields
        [Test]
        public void GetFieldGetterClass()
        {
            TestClass testClass = new();
            Func<object, object> classGetter = ReflectionHelpers.GetFieldGetter(
                typeof(TestClass).GetField(nameof(TestClass.intValue))
            );
            Assert.AreEqual(testClass.intValue, classGetter(testClass));
            for (int i = 0; i < NumTries; ++i)
            {
                testClass.intValue = _random.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testClass.intValue, classGetter(testClass));
            }
        }

        // TODO: Test on static fields
        [Test]
        public void GetFieldGetterStruct()
        {
            TestStruct testStruct = new();
            Func<object, object> structGetter = ReflectionHelpers.GetFieldGetter(
                typeof(TestStruct).GetField(nameof(TestStruct.intValue))
            );
            Assert.AreEqual(testStruct.intValue, structGetter(testStruct));
            for (int i = 0; i < NumTries; ++i)
            {
                testStruct.intValue = _random.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testStruct.intValue, structGetter(testStruct));
            }
        }

        // TODO: Test on static fields
        [Test]
        public void GetFieldSetterClass()
        {
            TestClass testClass = new();
            Action<object, object> structSetter = ReflectionHelpers.GetFieldSetter(
                typeof(TestClass).GetField(nameof(TestClass.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = _random.Next(int.MinValue, int.MaxValue);
                structSetter(testClass, expected);
                Assert.AreEqual(expected, testClass.intValue);
            }
        }

        // TODO: Test on static fields
        [Test]
        public void GetFieldSetterStruct()
        {
            // Need boxing
            object testStruct = new TestStruct();
            Action<object, object> structSetter = ReflectionHelpers.GetFieldSetter(
                typeof(TestStruct).GetField(nameof(TestStruct.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = _random.Next(int.MinValue, int.MaxValue);
                structSetter(testStruct, expected);
                Assert.AreEqual(expected, ((TestStruct)testStruct).intValue);
            }
        }

        // TODO: Test on static fields
        [Test]
        public void GetFieldSetterClassGeneric()
        {
            TestClass testClass = new();
            FieldSetter<TestClass, int> classSetter = ReflectionHelpers.GetFieldSetter<
                TestClass,
                int
            >(typeof(TestClass).GetField(nameof(TestClass.intValue)));
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = _random.Next(int.MinValue, int.MaxValue);
                classSetter(ref testClass, expected);
                Assert.AreEqual(expected, testClass.intValue);
            }
        }

        // TODO: Test on static fields
        [Test]
        public void GetFieldSetterStructGeneric()
        {
            TestStruct testStruct = new();
            FieldSetter<TestStruct, int> structSetter = ReflectionHelpers.GetFieldSetter<
                TestStruct,
                int
            >(typeof(TestStruct).GetField(nameof(TestStruct.intValue)));
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = _random.Next(int.MinValue, int.MaxValue);
                structSetter(ref testStruct, expected);
                Assert.AreEqual(expected, testStruct.intValue);
            }
        }

        // TODO: Test on static fields
        [Test]
        public void GetFieldGetterClassGeneric()
        {
            TestClass testClass = new();
            Func<TestClass, int> classGetter = ReflectionHelpers.GetFieldGetter<TestClass, int>(
                typeof(TestClass).GetField(nameof(TestClass.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                testClass.intValue = _random.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testClass.intValue, classGetter(testClass));
            }
        }

        // TODO: Test on static fields
        [Test]
        public void GetFieldGetterStructGeneric()
        {
            TestStruct testStruct = new();
            Func<TestStruct, int> structSetter = ReflectionHelpers.GetFieldGetter<TestStruct, int>(
                typeof(TestStruct).GetField(nameof(TestStruct.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                testStruct.intValue = _random.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testStruct.intValue, structSetter(testStruct));
            }
        }

        [Test]
        public void ArrayCreator()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int count = _random.Next(1_000);
                Array created = ReflectionHelpers.CreateArray(typeof(int), count);
                Assert.AreEqual(count, created.Length);
                Assert.IsTrue(created is int[]);
                int[] typed = (int[])created;
                Assert.AreEqual(count, typed.Length);
            }
        }

        [Test]
        public void ListCreator()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                IList created = ReflectionHelpers.CreateList(typeof(int));
                Assert.AreEqual(0, created.Count);
                Assert.IsTrue(created is List<int>);
                List<int> typedCreated = (List<int>)created;
                int count = _random.Next(50);
                List<int> expected = new();
                for (int j = 0; j < count; ++j)
                {
                    int element = _random.Next();
                    created.Add(element);
                    expected.Add(element);
                    Assert.AreEqual(j + 1, created.Count);
                    Assert.That(expected, Is.EqualTo(typedCreated));
                }
            }
        }

        [Test]
        public void ListWithSizeCreator()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = _random.Next(1_000);
                IList created = ReflectionHelpers.CreateList(typeof(int), capacity);
                Assert.AreEqual(0, created.Count);
                Assert.IsTrue(created is List<int>);
                List<int> typedCreated = (List<int>)created;
                Assert.AreEqual(capacity, typedCreated.Capacity);

                int count = _random.Next(50);
                List<int> expected = new();
                for (int j = 0; j < count; ++j)
                {
                    int element = _random.Next();
                    created.Add(element);
                    expected.Add(element);
                    Assert.AreEqual(j + 1, created.Count);
                    Assert.That(expected, Is.EqualTo(typedCreated));
                }
            }
        }
    }
}
