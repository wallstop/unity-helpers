namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;
    using CategoryAttribute = System.ComponentModel.CategoryAttribute;
    using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

    [AttributeUsage(AttributeTargets.All)]
    public class ReflectionTestAttribute : Attribute
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public ReflectionTestAttribute() { }

        public ReflectionTestAttribute(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

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

    public class TestMethodClass
    {
        public static int StaticMethodCallCount = 0;
        public int InstanceMethodCallCount = 0;

        // Void methods
        public static void StaticVoidMethod()
        {
            StaticMethodCallCount++;
        }

        public void InstanceVoidMethod()
        {
            InstanceMethodCallCount++;
        }

        // Return value methods
        public static int StaticIntMethod()
        {
            StaticMethodCallCount++;
            return 42;
        }

        public static string StaticStringMethod()
        {
            StaticMethodCallCount++;
            return "test";
        }

        public static bool StaticBoolMethod()
        {
            StaticMethodCallCount++;
            return true;
        }

        public int InstanceIntMethod()
        {
            InstanceMethodCallCount++;
            return 100;
        }

        public string InstanceStringMethod()
        {
            InstanceMethodCallCount++;
            return "instance";
        }

        // Methods with parameters
        public static int StaticMethodWithParam(int param)
        {
            StaticMethodCallCount++;
            return param * 2;
        }

        public int InstanceMethodWithParam(string param)
        {
            InstanceMethodCallCount++;
            return param?.Length ?? 0;
        }

        public static void StaticVoidMethodWithParam(int param)
        {
            StaticMethodCallCount = param;
        }

        // Multiple parameter methods
        public static int StaticMethodMultipleParams(int a, string b, bool c)
        {
            StaticMethodCallCount++;
            return a + (b?.Length ?? 0) + (c ? 1 : 0);
        }

        public void Reset()
        {
            StaticMethodCallCount = 0;
            InstanceMethodCallCount = 0;
        }
    }

    public sealed class TestConstructorClass
    {
        public int Value1 { get; }
        public string Value2 { get; }
        public bool Value3 { get; }

        public TestConstructorClass()
        {
            Value1 = 0;
            Value2 = "default";
            Value3 = false;
        }

        public TestConstructorClass(int value1)
        {
            Value1 = value1;
            Value2 = "single";
            Value3 = false;
        }

        public TestConstructorClass(int value1, string value2)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = false;
        }

        public TestConstructorClass(int value1, string value2, bool value3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }
    }

    public readonly struct TestConstructorStruct
    {
        public int Value { get; }

        public TestConstructorStruct(int value)
        {
            Value = value;
        }
    }

    public sealed class TestPropertyClass
    {
        private static int _staticValue = 50;
        private int _instanceValue = 25;

        public static int StaticProperty
        {
            get => _staticValue;
            set => _staticValue = value;
        }

        public int InstanceProperty
        {
            get => _instanceValue;
            set => _instanceValue = value;
        }

        public static string StaticStringProperty { get; set; } = "static";

        public string InstanceStringProperty { get; set; } = "instance";

        // Read-only properties
        public static int StaticReadOnlyProperty => 999;
        public int InstanceReadOnlyProperty => 888;
    }

    [Description("Test class for attribute testing")]
    [ReflectionTestAttribute("ClassLevel", 10)]
    public sealed class TestAttributeClass
    {
        [Description("Static field with description")]
        [ReflectionTestAttribute("StaticField", 15)]
        public static int StaticFieldWithAttribute = 1;

        [Category("TestCategory")]
        [ReflectionTestAttribute("InstanceField", 5)]
        public int InstanceFieldWithAttribute = 2;

        [Description("Static property for testing")]
        [ReflectionTestAttribute("StaticProperty", 20)]
        public static string StaticPropertyWithAttribute { get; set; } = "static";

        [Category("TestCategory")]
        [Description("Instance property")]
        [ReflectionTestAttribute("InstanceProperty", 8)]
        public string InstancePropertyWithAttribute { get; set; } = "instance";

        [Description("Static method for testing")]
        [ReflectionTestAttribute("StaticMethod", 12)]
        public static void StaticMethodWithAttribute() { }

        [Category("TestCategory")]
        [ReflectionTestAttribute("InstanceMethod", 7)]
        public void InstanceMethodWithAttribute() { }

        public void MethodWithAttributedParameter(
            [Description("Parameter with description")] int param
        ) { }
    }

    public sealed class GenericTestClass<T>
    {
        public T Value { get; set; }

        public GenericTestClass() { }

        public GenericTestClass(T value)
        {
            Value = value;
        }
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
            Assert.Throws<ArgumentException>(() =>
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
            Assert.Throws<ArgumentException>(() =>
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
            Assert.Throws<ArgumentException>(() =>
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
            Assert.Throws<ArgumentException>(() =>
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

        [Test]
        public void GetArrayCreator()
        {
            Func<int, Array> arrayCreator = ReflectionHelpers.GetArrayCreator(typeof(int));
            Assert.IsNotNull(arrayCreator);

            for (int i = 0; i < 10; ++i)
            {
                int size = PRNG.Instance.Next(1, 100);
                Array created = arrayCreator(size);
                Assert.IsNotNull(created);
                Assert.AreEqual(size, created.Length);
                Assert.IsTrue(created is int[]);
            }
        }

        [Test]
        public void GetListCreator()
        {
            Func<IList> listCreator = ReflectionHelpers.GetListCreator(typeof(string));
            Assert.IsNotNull(listCreator);

            for (int i = 0; i < 10; ++i)
            {
                IList created = listCreator();
                Assert.IsNotNull(created);
                Assert.AreEqual(0, created.Count);
                Assert.IsTrue(created is List<string>);
            }
        }

        [Test]
        public void GetListWithCapacityCreator()
        {
            Func<int, IList> listCreator = ReflectionHelpers.GetListWithCapacityCreator(
                typeof(bool)
            );
            Assert.IsNotNull(listCreator);

            for (int i = 0; i < 10; ++i)
            {
                int capacity = PRNG.Instance.Next(1, 100);
                IList created = listCreator(capacity);
                Assert.IsNotNull(created);
                Assert.AreEqual(0, created.Count);
                Assert.IsTrue(created is List<bool>);
                Assert.AreEqual(capacity, ((List<bool>)created).Capacity);
            }
        }

        [Test]
        public void InvokeStaticVoidMethod()
        {
            TestMethodClass testObj = new();
            testObj.Reset();

            MethodInfo method = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticVoidMethod)
            );
            Assert.IsNotNull(method);

            int initialCount = TestMethodClass.StaticMethodCallCount;
            object result = ReflectionHelpers.InvokeStaticMethod(method);

            Assert.IsNull(result);
            Assert.AreEqual(initialCount + 1, TestMethodClass.StaticMethodCallCount);
        }

        [Test]
        public void InvokeStaticMethodWithReturnValue()
        {
            TestMethodClass testObj = new();
            testObj.Reset();

            MethodInfo intMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticIntMethod)
            );
            MethodInfo stringMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticStringMethod)
            );
            MethodInfo boolMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticBoolMethod)
            );

            Assert.AreEqual(42, ReflectionHelpers.InvokeStaticMethod(intMethod));
            Assert.AreEqual("test", ReflectionHelpers.InvokeStaticMethod(stringMethod));
            Assert.AreEqual(true, ReflectionHelpers.InvokeStaticMethod(boolMethod));
            Assert.AreEqual(3, TestMethodClass.StaticMethodCallCount);
        }

        [Test]
        public void InvokeStaticMethodWithParameters()
        {
            int directResult = TestMethodClass.StaticMethodWithParam(10);
            Assert.AreEqual(20, directResult);

            TestMethodClass testObj = new();
            testObj.Reset();

            MethodInfo paramMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticMethodWithParam)
            );
            MethodInfo voidParamMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticVoidMethodWithParam)
            );
            MethodInfo multiParamMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticMethodMultipleParams)
            );

            object result1 = ReflectionHelpers.InvokeStaticMethod(paramMethod, 10);
            Assert.AreEqual(20, result1);
            ReflectionHelpers.InvokeStaticMethod(voidParamMethod, 99);
            Assert.AreEqual(99, TestMethodClass.StaticMethodCallCount);
            object result3 = ReflectionHelpers.InvokeStaticMethod(multiParamMethod, 5, "abc", true);
            Assert.AreEqual(9, result3);
        }

        [Test]
        public void InvokeInstanceMethod()
        {
            TestMethodClass testObj = new();
            testObj.Reset();

            MethodInfo voidMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.InstanceVoidMethod)
            );
            MethodInfo intMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.InstanceIntMethod)
            );
            MethodInfo stringMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.InstanceStringMethod)
            );
            MethodInfo paramMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.InstanceMethodWithParam)
            );

            Assert.IsNull(ReflectionHelpers.InvokeMethod(voidMethod, testObj));
            Assert.AreEqual(1, testObj.InstanceMethodCallCount);

            Assert.AreEqual(100, ReflectionHelpers.InvokeMethod(intMethod, testObj));
            Assert.AreEqual("instance", ReflectionHelpers.InvokeMethod(stringMethod, testObj));
            Assert.AreEqual(5, ReflectionHelpers.InvokeMethod(paramMethod, testObj, "hello"));
            Assert.AreEqual(4, testObj.InstanceMethodCallCount);
        }

        [Test]
        public void GetMethodInvoker()
        {
            TestMethodClass testObj = new();
            testObj.Reset();

            MethodInfo voidMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.InstanceVoidMethod)
            );
            MethodInfo intMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.InstanceIntMethod)
            );
            MethodInfo paramMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.InstanceMethodWithParam)
            );

            Func<object, object[], object> voidInvoker = ReflectionHelpers.GetMethodInvoker(
                voidMethod
            );
            Func<object, object[], object> intInvoker = ReflectionHelpers.GetMethodInvoker(
                intMethod
            );
            Func<object, object[], object> paramInvoker = ReflectionHelpers.GetMethodInvoker(
                paramMethod
            );

            Assert.IsNull(voidInvoker(testObj, Array.Empty<object>()));
            Assert.AreEqual(1, testObj.InstanceMethodCallCount);

            Assert.AreEqual(100, intInvoker(testObj, Array.Empty<object>()));
            Assert.AreEqual(2, testObj.InstanceMethodCallCount);

            Assert.AreEqual(5, paramInvoker(testObj, new object[] { "hello" }));
            Assert.AreEqual(3, testObj.InstanceMethodCallCount);
        }

        [Test]
        public void GetStaticMethodInvoker()
        {
            TestMethodClass testObj = new();
            testObj.Reset();

            MethodInfo voidMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticVoidMethod)
            );
            MethodInfo intMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticIntMethod)
            );
            MethodInfo paramMethod = typeof(TestMethodClass).GetMethod(
                nameof(TestMethodClass.StaticMethodWithParam)
            );

            Func<object[], object> voidInvoker = ReflectionHelpers.GetStaticMethodInvoker(
                voidMethod
            );
            Func<object[], object> intInvoker = ReflectionHelpers.GetStaticMethodInvoker(intMethod);
            Func<object[], object> paramInvoker = ReflectionHelpers.GetStaticMethodInvoker(
                paramMethod
            );

            Assert.IsNull(voidInvoker(Array.Empty<object>()));
            Assert.AreEqual(1, TestMethodClass.StaticMethodCallCount);

            Assert.AreEqual(42, intInvoker(Array.Empty<object>()));
            Assert.AreEqual(2, TestMethodClass.StaticMethodCallCount);

            Assert.AreEqual(20, paramInvoker(new object[] { 10 }));
            Assert.AreEqual(3, TestMethodClass.StaticMethodCallCount);
        }

        [Test]
        public void CreateInstanceParameterless()
        {
            TestConstructorClass defaultInstance =
                ReflectionHelpers.CreateInstance<TestConstructorClass>();
            Assert.IsNotNull(defaultInstance);
            Assert.AreEqual(0, defaultInstance.Value1);
            Assert.AreEqual("default", defaultInstance.Value2);
            Assert.AreEqual(false, defaultInstance.Value3);
        }

        [Test]
        public void CreateInstanceWithParameters()
        {
            TestConstructorClass singleParam =
                ReflectionHelpers.CreateInstance<TestConstructorClass>(42);
            Assert.IsNotNull(singleParam);
            Assert.AreEqual(42, singleParam.Value1);
            Assert.AreEqual("single", singleParam.Value2);
            Assert.AreEqual(false, singleParam.Value3);

            TestConstructorClass twoParam = ReflectionHelpers.CreateInstance<TestConstructorClass>(
                10,
                "test"
            );
            Assert.IsNotNull(twoParam);
            Assert.AreEqual(10, twoParam.Value1);
            Assert.AreEqual("test", twoParam.Value2);
            Assert.AreEqual(false, twoParam.Value3);

            TestConstructorClass threeParam =
                ReflectionHelpers.CreateInstance<TestConstructorClass>(5, "hello", true);
            Assert.IsNotNull(threeParam);
            Assert.AreEqual(5, threeParam.Value1);
            Assert.AreEqual("hello", threeParam.Value2);
            Assert.AreEqual(true, threeParam.Value3);
        }

        [Test]
        public void CreateInstanceUsingConstructorInfo()
        {
            ConstructorInfo[] constructors = typeof(TestConstructorClass).GetConstructors();
            ConstructorInfo parameterlessConstructor = constructors.First(c =>
                c.GetParameters().Length == 0
            );
            ConstructorInfo singleParamConstructor = constructors.First(c =>
                c.GetParameters().Length == 1
            );

            Func<object[], object> parameterlessDelegate = ReflectionHelpers.GetConstructor(
                parameterlessConstructor
            );
            Func<object[], object> singleParamDelegate = ReflectionHelpers.GetConstructor(
                singleParamConstructor
            );

            TestConstructorClass instance1 = (TestConstructorClass)parameterlessDelegate(
                Array.Empty<object>()
            );
            Assert.AreEqual(0, instance1.Value1);
            Assert.AreEqual("default", instance1.Value2);

            TestConstructorClass instance2 = (TestConstructorClass)singleParamDelegate(
                new object[] { 99 }
            );
            Assert.AreEqual(99, instance2.Value1);
            Assert.AreEqual("single", instance2.Value2);
        }

        [Test]
        public void GetParameterlessConstructor()
        {
            Func<TestConstructorClass> constructor =
                ReflectionHelpers.GetParameterlessConstructor<TestConstructorClass>();
            Assert.IsNotNull(constructor);

            for (int i = 0; i < 100; i++)
            {
                TestConstructorClass instance = constructor();
                Assert.IsNotNull(instance);
                Assert.AreEqual(0, instance.Value1);
                Assert.AreEqual("default", instance.Value2);
                Assert.AreEqual(false, instance.Value3);
            }
        }

        [Test]
        public void CreateGenericInstance()
        {
            object intGeneric = ReflectionHelpers.CreateGenericInstance<object>(
                typeof(GenericTestClass<>),
                new[] { typeof(int) },
                42
            );

            Assert.IsNotNull(intGeneric);
            Assert.IsInstanceOf<GenericTestClass<int>>(intGeneric);
            Assert.AreEqual(42, ((GenericTestClass<int>)intGeneric).Value);

            object stringGeneric = ReflectionHelpers.CreateGenericInstance<object>(
                typeof(GenericTestClass<>),
                new[] { typeof(string) },
                "hello"
            );

            Assert.IsNotNull(stringGeneric);
            Assert.IsInstanceOf<GenericTestClass<string>>(stringGeneric);
            Assert.AreEqual("hello", ((GenericTestClass<string>)stringGeneric).Value);
        }

        [Test]
        public void GetGenericParameterlessConstructor()
        {
            Func<object> constructor = ReflectionHelpers.GetGenericParameterlessConstructor<object>(
                typeof(GenericTestClass<>),
                typeof(int)
            );

            Assert.IsNotNull(constructor);

            object instance = constructor();
            Assert.IsNotNull(instance);
            Assert.IsInstanceOf<GenericTestClass<int>>(instance);
        }

        [Test]
        public void CreateStructInstance()
        {
            TestConstructorStruct structInstance =
                ReflectionHelpers.CreateInstance<TestConstructorStruct>(123);
            Assert.AreEqual(123, structInstance.Value);
        }

        [Test]
        public void GetPropertyGetterStatic()
        {
            PropertyInfo staticProp = typeof(TestPropertyClass).GetProperty(
                nameof(TestPropertyClass.StaticProperty)
            );
            PropertyInfo staticStringProp = typeof(TestPropertyClass).GetProperty(
                nameof(TestPropertyClass.StaticStringProperty)
            );
            PropertyInfo staticReadOnlyProp = typeof(TestPropertyClass).GetProperty(
                nameof(TestPropertyClass.StaticReadOnlyProperty)
            );

            Func<object, object> staticGetter = ReflectionHelpers.GetPropertyGetter(staticProp);
            Func<object, object> staticStringGetter = ReflectionHelpers.GetPropertyGetter(
                staticStringProp
            );
            Func<object, object> staticReadOnlyGetter = ReflectionHelpers.GetPropertyGetter(
                staticReadOnlyProp
            );

            // Test initial values
            Assert.AreEqual(50, staticGetter(null));
            Assert.AreEqual("static", staticStringGetter(null));
            Assert.AreEqual(999, staticReadOnlyGetter(null));

            // Test after changing values
            TestPropertyClass.StaticProperty = 100;
            TestPropertyClass.StaticStringProperty = "changed";

            Assert.AreEqual(100, staticGetter(null));
            Assert.AreEqual("changed", staticStringGetter(null));
            Assert.AreEqual(999, staticReadOnlyGetter(null)); // Read-only shouldn't change
        }

        [Test]
        public void GetPropertyGetterInstance()
        {
            TestPropertyClass testObj = new();
            PropertyInfo instanceProp = typeof(TestPropertyClass).GetProperty(
                nameof(TestPropertyClass.InstanceProperty)
            );
            PropertyInfo instanceStringProp = typeof(TestPropertyClass).GetProperty(
                nameof(TestPropertyClass.InstanceStringProperty)
            );
            PropertyInfo instanceReadOnlyProp = typeof(TestPropertyClass).GetProperty(
                nameof(TestPropertyClass.InstanceReadOnlyProperty)
            );

            Func<object, object> instanceGetter = ReflectionHelpers.GetPropertyGetter(instanceProp);
            Func<object, object> instanceStringGetter = ReflectionHelpers.GetPropertyGetter(
                instanceStringProp
            );
            Func<object, object> instanceReadOnlyGetter = ReflectionHelpers.GetPropertyGetter(
                instanceReadOnlyProp
            );

            // Test initial values
            Assert.AreEqual(25, instanceGetter(testObj));
            Assert.AreEqual("instance", instanceStringGetter(testObj));
            Assert.AreEqual(888, instanceReadOnlyGetter(testObj));

            // Test after changing values
            testObj.InstanceProperty = 200;
            testObj.InstanceStringProperty = "modified";

            Assert.AreEqual(200, instanceGetter(testObj));
            Assert.AreEqual("modified", instanceStringGetter(testObj));
            Assert.AreEqual(888, instanceReadOnlyGetter(testObj)); // Read-only shouldn't change
        }

        [Test]
        public void GetStaticPropertyGetter()
        {
            Func<int> staticGetter = ReflectionHelpers.GetStaticPropertyGetter<int>(
                typeof(TestPropertyClass).GetProperty(nameof(TestPropertyClass.StaticProperty))
            );

            Func<string> staticStringGetter = ReflectionHelpers.GetStaticPropertyGetter<string>(
                typeof(TestPropertyClass).GetProperty(
                    nameof(TestPropertyClass.StaticStringProperty)
                )
            );

            Assert.AreEqual(TestPropertyClass.StaticProperty, staticGetter());
            Assert.AreEqual(TestPropertyClass.StaticStringProperty, staticStringGetter());

            TestPropertyClass.StaticProperty = 777;
            TestPropertyClass.StaticStringProperty = "new value";

            Assert.AreEqual(777, staticGetter());
            Assert.AreEqual("new value", staticStringGetter());
        }

        [Test]
        public void GetAllLoadedAssemblies()
        {
            Assembly[] assemblies = ReflectionHelpers.GetAllLoadedAssemblies().ToArray();
            Assert.IsNotNull(assemblies);
            Assert.Greater(assemblies.Length, 0);
            Assert.IsTrue(assemblies.All(a => a != null));
            Assert.IsTrue(assemblies.All(a => !a.IsDynamic));
        }

        [Test]
        public void GetAllLoadedTypes()
        {
            Type[] types = ReflectionHelpers.GetAllLoadedTypes().Take(100).ToArray();
            Assert.IsNotNull(types);
            Assert.Greater(types.Length, 0);
            Assert.IsTrue(types.All(t => t != null));
        }

        [Test]
        public void GetTypesFromAssembly()
        {
            Assembly testAssembly = typeof(ReflectionHelperTests).Assembly;
            Type[] types = ReflectionHelpers.GetTypesFromAssembly(testAssembly).ToArray();

            Assert.IsNotNull(types);
            Assert.Greater(types.Length, 0);
            Assert.IsTrue(types.Contains(typeof(ReflectionHelperTests)));
            Assert.IsTrue(types.Contains(typeof(TestAttributeClass)));
            Assert.IsTrue(types.Contains(typeof(TestMethodClass)));
        }

        [Test]
        public void GetTypesFromAssemblyName()
        {
            Type[] types = ReflectionHelpers
                .GetTypesFromAssemblyName("System.Core")
                .Take(10)
                .ToArray();
            Assert.IsNotNull(types);
            // May be empty on some platforms, but should not throw
        }

        [Test]
        public void GetTypesWithAttribute()
        {
            Type[] typesWithDescAttr = ReflectionHelpers
                .GetTypesWithAttribute<DescriptionAttribute>()
                .ToArray();
            Assert.IsNotNull(typesWithDescAttr);
            Assert.IsTrue(typesWithDescAttr.Contains(typeof(TestAttributeClass)));

            Type[] typesByType = ReflectionHelpers
                .GetTypesWithAttribute(typeof(DescriptionAttribute))
                .ToArray();
            Assert.IsNotNull(typesByType);
            Assert.IsTrue(typesByType.Contains(typeof(TestAttributeClass)));
        }

        [Test]
        public void HasAttributeSafe()
        {
            Type testType = typeof(TestAttributeClass);
            FieldInfo staticField = testType.GetField(
                nameof(TestAttributeClass.StaticFieldWithAttribute)
            );
            MethodInfo instanceMethod = testType.GetMethod(
                nameof(TestAttributeClass.InstanceMethodWithAttribute)
            );

            Assert.IsTrue(ReflectionHelpers.HasAttributeSafe<ReflectionTestAttribute>(testType));
            Assert.IsTrue(ReflectionHelpers.HasAttributeSafe<ReflectionTestAttribute>(staticField));
            Assert.IsTrue(
                ReflectionHelpers.HasAttributeSafe<ReflectionTestAttribute>(instanceMethod)
            );

            Assert.IsTrue(
                ReflectionHelpers.HasAttributeSafe(testType, typeof(ReflectionTestAttribute))
            );
            Assert.IsTrue(
                ReflectionHelpers.HasAttributeSafe(staticField, typeof(ReflectionTestAttribute))
            );
            Assert.IsTrue(
                ReflectionHelpers.HasAttributeSafe(instanceMethod, typeof(ReflectionTestAttribute))
            );

            // Test with types that don't have attributes
            Assert.IsFalse(
                ReflectionHelpers.HasAttributeSafe<ReflectionTestAttribute>(typeof(TestMethodClass))
            );
            Assert.IsFalse(
                ReflectionHelpers.HasAttributeSafe(
                    typeof(TestMethodClass),
                    typeof(ReflectionTestAttribute)
                )
            );

            // Test with null (should return false, not throw)
            Assert.IsFalse(ReflectionHelpers.HasAttributeSafe<ReflectionTestAttribute>(null));
            Assert.IsFalse(
                ReflectionHelpers.HasAttributeSafe(null, typeof(ReflectionTestAttribute))
            );
        }

        [Test]
        public void GetAttributeSafe()
        {
            Type testType = typeof(TestAttributeClass);
            FieldInfo instanceField = testType.GetField(
                nameof(TestAttributeClass.InstanceFieldWithAttribute)
            );

            ReflectionTestAttribute classAttr =
                ReflectionHelpers.GetAttributeSafe<ReflectionTestAttribute>(testType);
            Assert.IsNotNull(classAttr);
            Assert.AreEqual("ClassLevel", classAttr.Name);
            Assert.AreEqual(10, classAttr.Value);

            ReflectionTestAttribute fieldAttr =
                ReflectionHelpers.GetAttributeSafe<ReflectionTestAttribute>(instanceField);
            Assert.IsNotNull(fieldAttr);
            Assert.AreEqual("InstanceField", fieldAttr.Name);
            Assert.AreEqual(5, fieldAttr.Value);

            // Test non-generic version
            Attribute classAttrObj = ReflectionHelpers.GetAttributeSafe(
                testType,
                typeof(ReflectionTestAttribute)
            );
            Assert.IsNotNull(classAttrObj);
            Assert.IsInstanceOf<ReflectionTestAttribute>(classAttrObj);

            // Test with null
            Assert.IsNull(ReflectionHelpers.GetAttributeSafe<ReflectionTestAttribute>(null));
            Assert.IsNull(
                ReflectionHelpers.GetAttributeSafe(null, typeof(ReflectionTestAttribute))
            );
        }

        [Test]
        public void GetAllAttributesSafe()
        {
            Type testType = typeof(TestAttributeClass);
            PropertyInfo instanceProperty = testType.GetProperty(
                nameof(TestAttributeClass.InstancePropertyWithAttribute)
            );

            ReflectionTestAttribute[] typeAttrs = ReflectionHelpers
                .GetAllAttributesSafe<ReflectionTestAttribute>(testType)
                .ToArray();
            Assert.IsNotNull(typeAttrs);
            Assert.AreEqual(1, typeAttrs.Length);
            Assert.AreEqual("ClassLevel", typeAttrs[0].Name);

            ReflectionTestAttribute[] propAttrs = ReflectionHelpers
                .GetAllAttributesSafe<ReflectionTestAttribute>(instanceProperty)
                .ToArray();
            Assert.IsNotNull(propAttrs);
            Assert.AreEqual(1, propAttrs.Length);
            Assert.AreEqual("InstanceProperty", propAttrs[0].Name);

            // Test non-generic version
            Attribute[] allTypeAttrs = ReflectionHelpers.GetAllAttributesSafe(testType).ToArray();
            Assert.IsNotNull(allTypeAttrs);
            Assert.Greater(allTypeAttrs.Length, 0);

            Attribute[] allPropAttrs = ReflectionHelpers
                .GetAllAttributesSafe(instanceProperty, typeof(ReflectionTestAttribute))
                .ToArray();
            Assert.IsNotNull(allPropAttrs);
            Assert.AreEqual(1, allPropAttrs.Length);

            // Test with null
            Assert.AreEqual(
                0,
                ReflectionHelpers.GetAllAttributesSafe<ReflectionTestAttribute>(null).Length
            );
            Assert.AreEqual(0, ReflectionHelpers.GetAllAttributesSafe(null).Length);
        }

        [Test]
        public void GetAllAttributeValuesSafe()
        {
            Type testType = typeof(TestAttributeClass);
            Dictionary<string, object> attrValues = ReflectionHelpers.GetAllAttributeValuesSafe(
                testType
            );

            Assert.IsNotNull(attrValues);
            Assert.IsTrue(attrValues.ContainsKey("ReflectionTest"));
            Assert.IsInstanceOf<ReflectionTestAttribute>(attrValues["ReflectionTest"]);

            // Test with null
            Dictionary<string, object> nullResult = ReflectionHelpers.GetAllAttributeValuesSafe(
                null
            );
            Assert.IsNotNull(nullResult);
            Assert.AreEqual(0, nullResult.Count);
        }

        [Test]
        public void GetMembersWithAttributeSafe()
        {
            Type testType = typeof(TestAttributeClass);

            MethodInfo[] methods = ReflectionHelpers
                .GetMethodsWithAttributeSafe<ReflectionTestAttribute>(testType)
                .ToArray();
            Assert.IsNotNull(methods);
            Assert.Greater(methods.Length, 0);
            Assert.IsTrue(
                methods.Any(m => m.Name == nameof(TestAttributeClass.StaticMethodWithAttribute))
            );
            Assert.IsTrue(
                methods.Any(m => m.Name == nameof(TestAttributeClass.InstanceMethodWithAttribute))
            );

            PropertyInfo[] properties = ReflectionHelpers
                .GetPropertiesWithAttributeSafe<ReflectionTestAttribute>(testType)
                .ToArray();
            Assert.IsNotNull(properties);
            Assert.Greater(properties.Length, 0);
            Assert.IsTrue(
                properties.Any(p =>
                    p.Name == nameof(TestAttributeClass.StaticPropertyWithAttribute)
                )
            );

            FieldInfo[] fields = ReflectionHelpers
                .GetFieldsWithAttributeSafe<ReflectionTestAttribute>(testType)
                .ToArray();
            Assert.IsNotNull(fields);
            Assert.Greater(fields.Length, 0);
            Assert.IsTrue(
                fields.Any(f => f.Name == nameof(TestAttributeClass.StaticFieldWithAttribute))
            );

            // Test with null
            Assert.AreEqual(
                0,
                ReflectionHelpers.GetMethodsWithAttributeSafe<ReflectionTestAttribute>(null).Length
            );
            Assert.AreEqual(
                0,
                ReflectionHelpers
                    .GetPropertiesWithAttributeSafe<ReflectionTestAttribute>(null)
                    .Length
            );
            Assert.AreEqual(
                0,
                ReflectionHelpers.GetFieldsWithAttributeSafe<ReflectionTestAttribute>(null).Length
            );
        }

        [Test]
        public void LoadStaticPropertiesForType()
        {
            Dictionary<string, PropertyInfo> staticProperties =
                ReflectionHelpers.LoadStaticPropertiesForType<TestPropertyClass>();
            Assert.IsNotNull(staticProperties);

            // Note: This method looks for static properties that return the same type as the class
            // Our TestPropertyClass doesn't have static properties that return TestPropertyClass,
            // so the result should be empty, but the method should not throw
            Assert.IsNotNull(staticProperties);
        }

        [Test]
        public void LoadStaticFieldsForType()
        {
            Dictionary<string, FieldInfo> staticFields =
                ReflectionHelpers.LoadStaticFieldsForType<TestPropertyClass>();
            Assert.IsNotNull(staticFields);

            // Note: This method looks for static fields that are of the same type as the class
            // Our TestPropertyClass doesn't have such fields, so the result should be empty,
            // but the method should not throw
            Assert.IsNotNull(staticFields);
        }

        [Test]
        public void IsAttributeDefined()
        {
            Type testType = typeof(TestAttributeClass);
            FieldInfo testField = testType.GetField(
                nameof(TestAttributeClass.InstanceFieldWithAttribute)
            );

            // Test the extension method version
            Assert.IsTrue(testType.IsAttributeDefined(out ReflectionTestAttribute typeAttr));
            Assert.IsNotNull(typeAttr);
            Assert.AreEqual("ClassLevel", typeAttr.Name);
            Assert.AreEqual(10, typeAttr.Value);

            Assert.IsTrue(testField.IsAttributeDefined(out ReflectionTestAttribute fieldAttr));
            Assert.IsNotNull(fieldAttr);
            Assert.AreEqual("InstanceField", fieldAttr.Name);
            Assert.AreEqual(5, fieldAttr.Value);

            // Test with type that doesn't have the attribute
            Type methodType = typeof(TestMethodClass);
            Assert.IsFalse(methodType.IsAttributeDefined(out ReflectionTestAttribute noAttr));
            Assert.IsNull(noAttr);
        }
    }
}
