namespace UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityHelpers.Core.Helper;
    using UnityHelpers.Core.Random;

    public struct TestStruct
    {
        public static int staticIntValue;
        public int intValue;
    }

    public sealed class TestClass
    {
        public static int staticIntValue;
        public int intValue;
    }

    public sealed class ReflectionHelperTests
    {
        private const int NumTries = 1_000;

        [Test]
        public void GetFieldGetterClassMemberField()
        {
            TestClass testClass = new();
            Func<object, object> classGetter = ReflectionHelpers.GetFieldGetter(
                typeof(TestClass).GetField(nameof(TestClass.intValue))
            );
            Assert.AreEqual(testClass.intValue, classGetter(testClass));
            for (int i = 0; i < NumTries; ++i)
            {
                testClass.intValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testClass.intValue, classGetter(testClass));
            }
        }

        [Test]
        public void GetFieldGetterClassStaticField()
        {
            TestClass testClass = new();
            Func<object, object> classGetter = ReflectionHelpers.GetFieldGetter(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            Assert.AreEqual(TestClass.staticIntValue, classGetter(testClass));
            for (int i = 0; i < NumTries; ++i)
            {
                TestClass.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestClass.staticIntValue, classGetter(testClass));
            }
        }

        [Test]
        public void GetStaticFieldGetterThrowsOnNonStaticField()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    ReflectionHelpers.GetStaticFieldGetter(
                        typeof(TestClass).GetField(nameof(TestClass.intValue))
                    )
            );
        }

        [Test]
        public void GetStaticFieldGetterClassStaticField()
        {
            Func<object> classGetter = ReflectionHelpers.GetStaticFieldGetter(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            Assert.AreEqual(TestClass.staticIntValue, classGetter());
            for (int i = 0; i < NumTries; ++i)
            {
                TestClass.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestClass.staticIntValue, classGetter());
            }
        }

        [Test]
        public void GetFieldGetterStructMemberField()
        {
            TestStruct testStruct = new();
            Func<object, object> structGetter = ReflectionHelpers.GetFieldGetter(
                typeof(TestStruct).GetField(nameof(TestStruct.intValue))
            );
            Assert.AreEqual(testStruct.intValue, structGetter(testStruct));
            for (int i = 0; i < NumTries; ++i)
            {
                testStruct.intValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testStruct.intValue, structGetter(testStruct));
            }
        }

        [Test]
        public void GetFieldGetterStructStaticField()
        {
            TestStruct testStruct = new();
            Func<object, object> structGetter = ReflectionHelpers.GetFieldGetter(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            Assert.AreEqual(TestStruct.staticIntValue, structGetter(testStruct));
            for (int i = 0; i < NumTries; ++i)
            {
                TestStruct.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestStruct.staticIntValue, structGetter(testStruct));
            }
        }

        [Test]
        public void GetStaticFieldGetterStructStaticField()
        {
            Func<object> structGetter = ReflectionHelpers.GetStaticFieldGetter(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            Assert.AreEqual(TestStruct.staticIntValue, structGetter());
            for (int i = 0; i < NumTries; ++i)
            {
                TestStruct.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestStruct.staticIntValue, structGetter());
            }
        }

        [Test]
        public void GetFieldSetterClassMemberField()
        {
            TestClass testClass = new();
            Action<object, object> structSetter = ReflectionHelpers.GetFieldSetter(
                typeof(TestClass).GetField(nameof(TestClass.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(testClass, expected);
                Assert.AreEqual(expected, testClass.intValue);
            }
        }

        [Test]
        public void GetFieldSetterClassStaticField()
        {
            TestClass testClass = new();
            Action<object, object> structSetter = ReflectionHelpers.GetFieldSetter(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(testClass, expected);
                Assert.AreEqual(expected, TestClass.staticIntValue);
            }
        }

        [Test]
        public void GetStaticFieldSetterThrowsOnNonStaticField()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    ReflectionHelpers.GetStaticFieldSetter(
                        typeof(TestClass).GetField(nameof(TestClass.intValue))
                    )
            );
        }

        [Test]
        public void GetStaticFieldSetterClassStaticField()
        {
            Action<object> structSetter = ReflectionHelpers.GetStaticFieldSetter(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(expected);
                Assert.AreEqual(expected, TestClass.staticIntValue);
            }
        }

        [Test]
        public void GetFieldSetterStructMemberField()
        {
            // Need boxing
            object testStruct = new TestStruct();
            Action<object, object> structSetter = ReflectionHelpers.GetFieldSetter(
                typeof(TestStruct).GetField(nameof(TestStruct.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(testStruct, expected);
                Assert.AreEqual(expected, ((TestStruct)testStruct).intValue);
            }
        }

        [Test]
        public void GetFieldSetterStructStaticField()
        {
            // Need boxing
            object testStruct = new TestStruct();
            Action<object, object> structSetter = ReflectionHelpers.GetFieldSetter(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(testStruct, expected);
                Assert.AreEqual(expected, TestStruct.staticIntValue);
            }
        }

        [Test]
        public void GetStaticFieldSetterStructStaticField()
        {
            Action<object> structSetter = ReflectionHelpers.GetStaticFieldSetter(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(expected);
                Assert.AreEqual(expected, TestStruct.staticIntValue);
            }
        }

        [Test]
        public void GetFieldSetterClassGenericMemberField()
        {
            TestClass testClass = new();
            FieldSetter<TestClass, int> classSetter = ReflectionHelpers.GetFieldSetter<
                TestClass,
                int
            >(typeof(TestClass).GetField(nameof(TestClass.intValue)));
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                classSetter(ref testClass, expected);
                Assert.AreEqual(expected, testClass.intValue);
            }
        }

        [Test]
        public void GetFieldSetterClassGenericStaticField()
        {
            TestClass testClass = new();
            FieldSetter<TestClass, int> classSetter = ReflectionHelpers.GetFieldSetter<
                TestClass,
                int
            >(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                classSetter(ref testClass, expected);
                Assert.AreEqual(expected, TestClass.staticIntValue);
            }
        }

        [Test]
        public void GetStaticFieldSetterGenericThrowsOnNonStaticField()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    ReflectionHelpers.GetStaticFieldSetter<int>(
                        typeof(TestClass).GetField(nameof(TestClass.intValue))
                    )
            );
        }

        [Test]
        public void GetStaticFieldSetterClassGenericStaticField()
        {
            Action<int> classSetter = ReflectionHelpers.GetStaticFieldSetter<int>(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                classSetter(expected);
                Assert.AreEqual(expected, TestClass.staticIntValue);
            }
        }

        [Test]
        public void GetFieldSetterStructGenericMemberField()
        {
            TestStruct testStruct = new();
            FieldSetter<TestStruct, int> structSetter = ReflectionHelpers.GetFieldSetter<
                TestStruct,
                int
            >(typeof(TestStruct).GetField(nameof(TestStruct.intValue)));
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(ref testStruct, expected);
                Assert.AreEqual(expected, testStruct.intValue);
            }
        }

        [Test]
        public void GetFieldSetterStructGenericStaticField()
        {
            TestStruct testStruct = new();
            FieldSetter<TestStruct, int> structSetter = ReflectionHelpers.GetFieldSetter<
                TestStruct,
                int
            >(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(ref testStruct, expected);
                Assert.AreEqual(expected, TestStruct.staticIntValue);
            }
        }

        [Test]
        public void GetStaticFieldSetterStructGenericStaticField()
        {
            Action<int> structSetter = ReflectionHelpers.GetStaticFieldSetter<int>(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                int expected = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                structSetter(expected);
                Assert.AreEqual(expected, TestStruct.staticIntValue);
            }
        }

        [Test]
        public void GetFieldGetterClassGenericMemberField()
        {
            TestClass testClass = new();
            Func<TestClass, int> classGetter = ReflectionHelpers.GetFieldGetter<TestClass, int>(
                typeof(TestClass).GetField(nameof(TestClass.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                testClass.intValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testClass.intValue, classGetter(testClass));
            }
        }

        [Test]
        public void GetFieldGetterClassGenericStaticField()
        {
            TestClass testClass = new();
            Func<TestClass, int> classGetter = ReflectionHelpers.GetFieldGetter<TestClass, int>(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                TestClass.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestClass.staticIntValue, classGetter(testClass));
            }
        }

        [Test]
        public void GetStaticFieldGetterGenericThrowsOnNonStaticField()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    ReflectionHelpers.GetStaticFieldGetter<int>(
                        typeof(TestClass).GetField(nameof(TestClass.intValue))
                    )
            );
        }

        [Test]
        public void GetStaticFieldGetterClassGenericStaticField()
        {
            Func<int> classGetter = ReflectionHelpers.GetStaticFieldGetter<int>(
                typeof(TestClass).GetField(
                    nameof(TestClass.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                TestClass.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestClass.staticIntValue, classGetter());
            }
        }

        [Test]
        public void GetFieldGetterStructGenericMemberField()
        {
            TestStruct testStruct = new();
            Func<TestStruct, int> structSetter = ReflectionHelpers.GetFieldGetter<TestStruct, int>(
                typeof(TestStruct).GetField(nameof(TestStruct.intValue))
            );
            for (int i = 0; i < NumTries; ++i)
            {
                testStruct.intValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(testStruct.intValue, structSetter(testStruct));
            }
        }

        [Test]
        public void GetFieldGetterStructGenericStaticField()
        {
            TestStruct testStruct = new();
            Func<TestStruct, int> structSetter = ReflectionHelpers.GetFieldGetter<TestStruct, int>(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                TestStruct.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestStruct.staticIntValue, structSetter(testStruct));
            }
        }

        [Test]
        public void GetStaticFieldGetterStructGenericStaticField()
        {
            Func<int> structSetter = ReflectionHelpers.GetStaticFieldGetter<int>(
                typeof(TestStruct).GetField(
                    nameof(TestStruct.staticIntValue),
                    BindingFlags.Static | BindingFlags.Public
                )
            );
            for (int i = 0; i < NumTries; ++i)
            {
                TestStruct.staticIntValue = PRNG.Instance.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(TestStruct.staticIntValue, structSetter());
            }
        }

        [Test]
        public void ArrayCreator()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int count = PRNG.Instance.Next(1_000);
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
                int count = PRNG.Instance.Next(50);
                List<int> expected = new();
                for (int j = 0; j < count; ++j)
                {
                    int element = PRNG.Instance.Next();
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
                int capacity = PRNG.Instance.Next(1_000);
                IList created = ReflectionHelpers.CreateList(typeof(int), capacity);
                Assert.AreEqual(0, created.Count);
                Assert.IsTrue(created is List<int>);
                List<int> typedCreated = (List<int>)created;
                Assert.AreEqual(capacity, typedCreated.Capacity);

                int count = PRNG.Instance.Next(50);
                List<int> expected = new();
                for (int j = 0; j < count; ++j)
                {
                    int element = PRNG.Instance.Next();
                    created.Add(element);
                    expected.Add(element);
                    Assert.AreEqual(j + 1, created.Count);
                    Assert.That(expected, Is.EqualTo(typedCreated));
                }
            }
        }
    }
}
