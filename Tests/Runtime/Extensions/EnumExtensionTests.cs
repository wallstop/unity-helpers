namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class EnumExtensionTests
    {
        private enum TestEnum
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
            All = First | Second | Third,
        }

        private enum TinyTestEnum : byte
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
            Fourth = 1 << 3,
            Fifth = 1 << 4,
            Sixth = 1 << 5,
            Seventh = 1 << 6,
            Eighth = 1 << 7,
            All = First | Second | Third | Fourth | Fifth | Sixth | Seventh | Eighth,
        }

        private enum SmallTestEnum : short
        {
            None = 0,

            [EnumDisplayName("TestFirst")]
            First = 1 << 0,

            [EnumDisplayName("TestSecond")]
            Second = 1 << 1,

            [EnumDisplayName("TestThird")]
            Third = 1 << 2,
        }

        private enum BigTestEnum : long
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
            VeryLarge = 1L << 62,
        }

        private enum SignedByteEnum : sbyte
        {
            NegativeValue = -1,
            Zero = 0,
            PositiveValue = 1,
            MaxValue = 127,
        }

        private enum UnsignedShortEnum : ushort
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
            Large = 1 << 15,
        }

        private enum UnsignedIntEnum : uint
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
            Large = 1u << 31,
        }

        private enum UnsignedLongEnum : ulong
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
            VeryLarge = 1UL << 63,
        }

        private enum SingleValueEnum
        {
            OnlyValue = 42,
        }

        private enum NonFlagsEnum
        {
            Red = 1,
            Green = 2,
            Blue = 3,
            Yellow = 4,
        }

        private enum ComplexDisplayNameEnum
        {
            [EnumDisplayName("")]
            EmptyDisplayName = 0,

            [EnumDisplayName("Display with spaces")]
            WithSpaces = 1,

            [EnumDisplayName("Display-with-special!@#$%characters")]
            SpecialChars = 2,

            NoAttribute = 3,

            [EnumDisplayName(
                "VeryLongDisplayNameThatCouldPotentiallyCauseIssuesWithBufferSizesOrMemoryAllocation"
            )]
            VeryLongName = 4,
        }

        [Test]
        public void DisplayName()
        {
            Assert.AreEqual("None", SmallTestEnum.None.ToDisplayName());
            Assert.AreEqual("TestFirst", SmallTestEnum.First.ToDisplayName());
            Assert.AreEqual("TestSecond", SmallTestEnum.Second.ToDisplayName());
            Assert.AreEqual("TestThird", SmallTestEnum.Third.ToDisplayName());
        }

        [Test]
        public void CachedName()
        {
            Assert.AreEqual("None", SmallTestEnum.None.ToCachedName());
            Assert.AreEqual("First", SmallTestEnum.First.ToCachedName());
            Assert.AreEqual("Second", SmallTestEnum.Second.ToCachedName());
            Assert.AreEqual("Third", SmallTestEnum.Third.ToCachedName());
        }

        [Test]
        public void HasFlagNoAlloc()
        {
            Assert.IsTrue(TestEnum.First.HasFlagNoAlloc(TestEnum.First));
            Assert.IsFalse(TestEnum.First.HasFlagNoAlloc(TestEnum.Second));
            Assert.IsTrue((TestEnum.First | TestEnum.Second).HasFlagNoAlloc(TestEnum.First));
            Assert.IsTrue((TestEnum.First | TestEnum.Second).HasFlagNoAlloc(TestEnum.Second));
            Assert.IsFalse((TestEnum.First | TestEnum.Second).HasFlagNoAlloc(TestEnum.Third));
            Assert.IsFalse(TestEnum.First.HasFlagNoAlloc((TestEnum.First | TestEnum.Second)));
        }

        [Test]
        public void HasFlagNoAllocTiny()
        {
            Assert.IsTrue(TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.First));
            Assert.IsFalse(TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.Second));
            Assert.IsTrue(
                (TinyTestEnum.First | TinyTestEnum.Second).HasFlagNoAlloc(TinyTestEnum.First)
            );
            Assert.IsTrue(
                (TinyTestEnum.First | TinyTestEnum.Second).HasFlagNoAlloc(TinyTestEnum.Second)
            );
            Assert.IsFalse(
                (TinyTestEnum.First | TinyTestEnum.Second).HasFlagNoAlloc(TinyTestEnum.Third)
            );
            Assert.IsFalse(
                TinyTestEnum.First.HasFlagNoAlloc((TinyTestEnum.First | TinyTestEnum.Second))
            );
        }

        [Test]
        public void HasFlagNoAllocSmall()
        {
            Assert.IsTrue(SmallTestEnum.First.HasFlagNoAlloc(SmallTestEnum.First));
            Assert.IsFalse(SmallTestEnum.First.HasFlagNoAlloc(SmallTestEnum.Second));
            Assert.IsTrue(
                (SmallTestEnum.First | SmallTestEnum.Second).HasFlagNoAlloc(SmallTestEnum.First)
            );
            Assert.IsTrue(
                (SmallTestEnum.First | SmallTestEnum.Second).HasFlagNoAlloc(SmallTestEnum.Second)
            );
            Assert.IsFalse(
                (SmallTestEnum.First | SmallTestEnum.Second).HasFlagNoAlloc(SmallTestEnum.Third)
            );
            Assert.IsFalse(
                SmallTestEnum.First.HasFlagNoAlloc((SmallTestEnum.First | SmallTestEnum.Second))
            );
        }

        [Test]
        public void HasFlagNoAllocBig()
        {
            Assert.IsTrue(BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.First));
            Assert.IsFalse(BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.Second));
            Assert.IsTrue(
                (BigTestEnum.First | BigTestEnum.Second).HasFlagNoAlloc(BigTestEnum.First)
            );
            Assert.IsTrue(
                (BigTestEnum.First | BigTestEnum.Second).HasFlagNoAlloc(BigTestEnum.Second)
            );
            Assert.IsFalse(
                (BigTestEnum.First | BigTestEnum.Second).HasFlagNoAlloc(BigTestEnum.Third)
            );
            Assert.IsFalse(
                BigTestEnum.First.HasFlagNoAlloc((BigTestEnum.First | BigTestEnum.Second))
            );
        }

        [Test]
        public void HasFlagsNoAllocDoesNotAlloc()
        {
            GCAssert.DoesNotAllocate(() =>
            {
                TestEnum.First.HasFlagNoAlloc(TestEnum.First);
                TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.First);
                BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.First);

                TestEnum.First.HasFlagNoAlloc(TestEnum.Second);
                TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.Second);
                BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.Second);
            });
        }

        [Test]
        public void HasFlagNoAllocWithNone()
        {
            Assert.IsTrue(TestEnum.None.HasFlagNoAlloc(TestEnum.None));
            Assert.IsTrue(TestEnum.First.HasFlagNoAlloc(TestEnum.None));
            Assert.IsTrue((TestEnum.First | TestEnum.Second).HasFlagNoAlloc(TestEnum.None));
            Assert.IsFalse(TestEnum.None.HasFlagNoAlloc(TestEnum.First));
        }

        [Test]
        public void HasFlagNoAllocWithAllFlags()
        {
            TestEnum allFlags = TestEnum.All;
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TestEnum.First));
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TestEnum.Second));
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TestEnum.Third));
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TestEnum.First | TestEnum.Second));
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TestEnum.All));
        }

        [Test]
        public void HasFlagNoAllocWithMaxByteValues()
        {
            TinyTestEnum allFlags = TinyTestEnum.All;
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TinyTestEnum.First));
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TinyTestEnum.Eighth));
            Assert.IsTrue(allFlags.HasFlagNoAlloc(TinyTestEnum.First | TinyTestEnum.Eighth));
        }

        [Test]
        public void HasFlagNoAllocWithLargeValues()
        {
            BigTestEnum large = BigTestEnum.VeryLarge;
            Assert.IsTrue(large.HasFlagNoAlloc(BigTestEnum.VeryLarge));
            Assert.IsFalse(large.HasFlagNoAlloc(BigTestEnum.First));

            BigTestEnum combined = BigTestEnum.First | BigTestEnum.VeryLarge;
            Assert.IsTrue(combined.HasFlagNoAlloc(BigTestEnum.First));
            Assert.IsTrue(combined.HasFlagNoAlloc(BigTestEnum.VeryLarge));
        }

        [Test]
        public void HasFlagNoAllocUnsignedShort()
        {
            Assert.IsTrue(UnsignedShortEnum.First.HasFlagNoAlloc(UnsignedShortEnum.First));
            Assert.IsFalse(UnsignedShortEnum.First.HasFlagNoAlloc(UnsignedShortEnum.Second));
            Assert.IsTrue(
                (UnsignedShortEnum.First | UnsignedShortEnum.Second).HasFlagNoAlloc(
                    UnsignedShortEnum.First
                )
            );
            Assert.IsTrue(UnsignedShortEnum.Large.HasFlagNoAlloc(UnsignedShortEnum.Large));
            Assert.IsTrue(
                (UnsignedShortEnum.First | UnsignedShortEnum.Large).HasFlagNoAlloc(
                    UnsignedShortEnum.Large
                )
            );
        }

        [Test]
        public void HasFlagNoAllocUnsignedInt()
        {
            Assert.IsTrue(UnsignedIntEnum.First.HasFlagNoAlloc(UnsignedIntEnum.First));
            Assert.IsFalse(UnsignedIntEnum.First.HasFlagNoAlloc(UnsignedIntEnum.Second));
            Assert.IsTrue(
                (UnsignedIntEnum.First | UnsignedIntEnum.Second).HasFlagNoAlloc(
                    UnsignedIntEnum.First
                )
            );
            Assert.IsTrue(UnsignedIntEnum.Large.HasFlagNoAlloc(UnsignedIntEnum.Large));
            Assert.IsTrue(
                (UnsignedIntEnum.First | UnsignedIntEnum.Large).HasFlagNoAlloc(
                    UnsignedIntEnum.Large
                )
            );
        }

        [Test]
        public void HasFlagNoAllocUnsignedLong()
        {
            Assert.IsTrue(UnsignedLongEnum.First.HasFlagNoAlloc(UnsignedLongEnum.First));
            Assert.IsFalse(UnsignedLongEnum.First.HasFlagNoAlloc(UnsignedLongEnum.Second));
            Assert.IsTrue(
                (UnsignedLongEnum.First | UnsignedLongEnum.Second).HasFlagNoAlloc(
                    UnsignedLongEnum.First
                )
            );
            Assert.IsTrue(UnsignedLongEnum.VeryLarge.HasFlagNoAlloc(UnsignedLongEnum.VeryLarge));
            Assert.IsTrue(
                (UnsignedLongEnum.First | UnsignedLongEnum.VeryLarge).HasFlagNoAlloc(
                    UnsignedLongEnum.VeryLarge
                )
            );
        }

        [Test]
        public void HasFlagNoAllocSignedByte()
        {
            Assert.IsTrue(SignedByteEnum.Zero.HasFlagNoAlloc(SignedByteEnum.Zero));
            Assert.IsTrue(
                SignedByteEnum.PositiveValue.HasFlagNoAlloc(SignedByteEnum.PositiveValue)
            );
            Assert.IsTrue(SignedByteEnum.MaxValue.HasFlagNoAlloc(SignedByteEnum.MaxValue));
        }

        [Test]
        public void DisplayNameWithComplexValues()
        {
            Assert.AreEqual("", ComplexDisplayNameEnum.EmptyDisplayName.ToDisplayName());
            Assert.AreEqual(
                "Display with spaces",
                ComplexDisplayNameEnum.WithSpaces.ToDisplayName()
            );
            Assert.AreEqual(
                "Display-with-special!@#$%characters",
                ComplexDisplayNameEnum.SpecialChars.ToDisplayName()
            );
            Assert.AreEqual("NoAttribute", ComplexDisplayNameEnum.NoAttribute.ToDisplayName());
            Assert.AreEqual(
                "VeryLongDisplayNameThatCouldPotentiallyCauseIssuesWithBufferSizesOrMemoryAllocation",
                ComplexDisplayNameEnum.VeryLongName.ToDisplayName()
            );
        }

        [Test]
        public void DisplayNameWithoutAttribute()
        {
            Assert.AreEqual("None", TestEnum.None.ToDisplayName());
            Assert.AreEqual("First", TestEnum.First.ToDisplayName());
            Assert.AreEqual("All", TestEnum.All.ToDisplayName());
        }

        [Test]
        public void DisplayNameWithDifferentEnumTypes()
        {
            Assert.AreEqual("None", TinyTestEnum.None.ToDisplayName());
            Assert.AreEqual("First", BigTestEnum.First.ToDisplayName());
            Assert.AreEqual("None", UnsignedIntEnum.None.ToDisplayName());
            Assert.AreEqual("Zero", SignedByteEnum.Zero.ToDisplayName());
        }

        [Test]
        public void DisplayNameWithSingleValueEnum()
        {
            Assert.AreEqual("OnlyValue", SingleValueEnum.OnlyValue.ToDisplayName());
        }

        [Test]
        public void DisplayNameCachingConsistency()
        {
            string first1 = SmallTestEnum.First.ToDisplayName();
            string first2 = SmallTestEnum.First.ToDisplayName();
            Assert.AreSame(first1, first2);
        }

        [Test]
        public void DisplayNameWithCombinedFlags()
        {
            SmallTestEnum combined = SmallTestEnum.First | SmallTestEnum.Second;
            string displayName = combined.ToDisplayName();
            Assert.IsNotEmpty(displayName);
            string secondCall = combined.ToDisplayName();
            Assert.AreSame(displayName, secondCall);
        }

        [Test]
        public void DisplayNamesFromCollection()
        {
            List<SmallTestEnum> values = new()
            {
                SmallTestEnum.None,
                SmallTestEnum.First,
                SmallTestEnum.Second,
                SmallTestEnum.Third,
            };

            string[] displayNames = values.ToDisplayNames().ToArray();

            Assert.AreEqual(4, displayNames.Length);
            Assert.AreEqual("None", displayNames[0]);
            Assert.AreEqual("TestFirst", displayNames[1]);
            Assert.AreEqual("TestSecond", displayNames[2]);
            Assert.AreEqual("TestThird", displayNames[3]);
        }

        [Test]
        public void DisplayNamesFromEmptyCollection()
        {
            List<TestEnum> values = new();
            string[] displayNames = values.ToDisplayNames().ToArray();
            Assert.AreEqual(0, displayNames.Length);
        }

        [Test]
        public void DisplayNamesFromSingleItemCollection()
        {
            List<TestEnum> values = new() { TestEnum.First };
            string[] displayNames = values.ToDisplayNames().ToArray();
            Assert.AreEqual(1, displayNames.Length);
            Assert.AreEqual("First", displayNames[0]);
        }

        [Test]
        public void DisplayNamesWithDuplicateValues()
        {
            List<SmallTestEnum> values = new()
            {
                SmallTestEnum.First,
                SmallTestEnum.First,
                SmallTestEnum.Second,
            };

            string[] displayNames = values.ToDisplayNames().ToArray();
            Assert.AreEqual(3, displayNames.Length);
            Assert.AreEqual("TestFirst", displayNames[0]);
            Assert.AreEqual("TestFirst", displayNames[1]);
            Assert.AreEqual("TestSecond", displayNames[2]);
        }

        [Test]
        public void CachedNameWithDifferentEnumTypes()
        {
            Assert.AreEqual("None", TestEnum.None.ToCachedName());
            Assert.AreEqual("First", TinyTestEnum.First.ToCachedName());
            Assert.AreEqual("VeryLarge", BigTestEnum.VeryLarge.ToCachedName());
            Assert.AreEqual("Large", UnsignedIntEnum.Large.ToCachedName());
        }

        [Test]
        public void CachedNameWithCombinedFlags()
        {
            TestEnum combined = TestEnum.First | TestEnum.Second;
            string cachedName = combined.ToCachedName();
            Assert.IsNotEmpty(cachedName);
            string secondCall = combined.ToCachedName();
            Assert.AreSame(cachedName, secondCall);
        }

        [Test]
        public void CachedNameWithAllFlags()
        {
            string cachedName = TestEnum.All.ToCachedName();
            Assert.AreEqual("All", cachedName);
        }

        [Test]
        public void CachedNameCachingConsistency()
        {
            string first1 = TestEnum.First.ToCachedName();
            string first2 = TestEnum.First.ToCachedName();
            Assert.AreSame(first1, first2);
        }

        [Test]
        public void CachedNameWithSingleValueEnum()
        {
            Assert.AreEqual("OnlyValue", SingleValueEnum.OnlyValue.ToCachedName());
        }

        [Test]
        public void CachedNameWithNonFlagsEnum()
        {
            Assert.AreEqual("Red", NonFlagsEnum.Red.ToCachedName());
            Assert.AreEqual("Green", NonFlagsEnum.Green.ToCachedName());
            Assert.AreEqual("Blue", NonFlagsEnum.Blue.ToCachedName());
        }

        [Test]
        public void CachedNamesFromCollection()
        {
            List<TestEnum> values = new()
            {
                TestEnum.None,
                TestEnum.First,
                TestEnum.Second,
                TestEnum.Third,
            };

            string[] cachedNames = values.ToCachedNames().ToArray();

            Assert.AreEqual(4, cachedNames.Length);
            Assert.AreEqual("None", cachedNames[0]);
            Assert.AreEqual("First", cachedNames[1]);
            Assert.AreEqual("Second", cachedNames[2]);
            Assert.AreEqual("Third", cachedNames[3]);
        }

        [Test]
        public void CachedNamesFromEmptyCollection()
        {
            List<TestEnum> values = new();
            string[] cachedNames = values.ToCachedNames().ToArray();
            Assert.AreEqual(0, cachedNames.Length);
        }

        [Test]
        public void CachedNamesFromSingleItemCollection()
        {
            List<BigTestEnum> values = new() { BigTestEnum.VeryLarge };
            string[] cachedNames = values.ToCachedNames().ToArray();
            Assert.AreEqual(1, cachedNames.Length);
            Assert.AreEqual("VeryLarge", cachedNames[0]);
        }

        [Test]
        public void CachedNamesWithDuplicateValues()
        {
            List<TestEnum> values = new() { TestEnum.First, TestEnum.First, TestEnum.Second };

            string[] cachedNames = values.ToCachedNames().ToArray();
            Assert.AreEqual(3, cachedNames.Length);
            Assert.AreEqual("First", cachedNames[0]);
            Assert.AreEqual("First", cachedNames[1]);
            Assert.AreEqual("Second", cachedNames[2]);
        }

        [Test]
        public void CachedNamesWithMixedEnumTypes()
        {
            List<TinyTestEnum> values = new()
            {
                TinyTestEnum.None,
                TinyTestEnum.First,
                TinyTestEnum.Eighth,
            };

            string[] cachedNames = values.ToCachedNames().ToArray();
            Assert.AreEqual(3, cachedNames.Length);
            Assert.AreEqual("None", cachedNames[0]);
            Assert.AreEqual("First", cachedNames[1]);
            Assert.AreEqual("Eighth", cachedNames[2]);
        }

        [Test]
        public void HasFlagNoAllocWithTripleCombination()
        {
            TestEnum triple = TestEnum.First | TestEnum.Second | TestEnum.Third;
            Assert.IsTrue(triple.HasFlagNoAlloc(TestEnum.First));
            Assert.IsTrue(triple.HasFlagNoAlloc(TestEnum.Second));
            Assert.IsTrue(triple.HasFlagNoAlloc(TestEnum.Third));
            Assert.IsTrue(triple.HasFlagNoAlloc(TestEnum.First | TestEnum.Second));
            Assert.IsTrue(triple.HasFlagNoAlloc(TestEnum.Second | TestEnum.Third));
            Assert.IsTrue(triple.HasFlagNoAlloc(TestEnum.First | TestEnum.Third));
        }

        [Test]
        public void HasFlagNoAllocMultipleFlagsCheckedIndependently()
        {
            TinyTestEnum flags = TinyTestEnum.First | TinyTestEnum.Fourth | TinyTestEnum.Seventh;
            Assert.IsTrue(flags.HasFlagNoAlloc(TinyTestEnum.First));
            Assert.IsFalse(flags.HasFlagNoAlloc(TinyTestEnum.Second));
            Assert.IsFalse(flags.HasFlagNoAlloc(TinyTestEnum.Third));
            Assert.IsTrue(flags.HasFlagNoAlloc(TinyTestEnum.Fourth));
            Assert.IsFalse(flags.HasFlagNoAlloc(TinyTestEnum.Fifth));
            Assert.IsFalse(flags.HasFlagNoAlloc(TinyTestEnum.Sixth));
            Assert.IsTrue(flags.HasFlagNoAlloc(TinyTestEnum.Seventh));
            Assert.IsFalse(flags.HasFlagNoAlloc(TinyTestEnum.Eighth));
        }

        [Test]
        public void HasFlagNoAllocNonFlagsEnum()
        {
            Assert.IsTrue(NonFlagsEnum.Red.HasFlagNoAlloc(NonFlagsEnum.Red));
            Assert.IsFalse(NonFlagsEnum.Red.HasFlagNoAlloc(NonFlagsEnum.Green));
            Assert.IsFalse(NonFlagsEnum.Blue.HasFlagNoAlloc(NonFlagsEnum.Yellow));
        }

        [Test]
        public void CachedNamePerformanceWithRepeatedCalls()
        {
            for (int i = 0; i < 1000; i++)
            {
                _ = TestEnum.First.ToCachedName();
                _ = TestEnum.Second.ToCachedName();
                _ = TestEnum.Third.ToCachedName();
            }
        }

        [Test]
        public void DisplayNamePerformanceWithRepeatedCalls()
        {
            for (int i = 0; i < 1000; i++)
            {
                _ = SmallTestEnum.First.ToDisplayName();
                _ = SmallTestEnum.Second.ToDisplayName();
                _ = SmallTestEnum.Third.ToDisplayName();
            }
        }

        [Test]
        public void HasFlagNoAllocPerformanceWithRepeatedCalls()
        {
            TestEnum value = TestEnum.First | TestEnum.Second;
            for (int i = 0; i < 10000; i++)
            {
                _ = value.HasFlagNoAlloc(TestEnum.First);
                _ = value.HasFlagNoAlloc(TestEnum.Second);
                _ = value.HasFlagNoAlloc(TestEnum.Third);
            }
        }

        [Test]
        public void DisplayNamesPreservesOrder()
        {
            List<SmallTestEnum> values = new()
            {
                SmallTestEnum.Third,
                SmallTestEnum.First,
                SmallTestEnum.Second,
                SmallTestEnum.None,
            };

            string[] displayNames = values.ToDisplayNames().ToArray();
            Assert.AreEqual("TestThird", displayNames[0]);
            Assert.AreEqual("TestFirst", displayNames[1]);
            Assert.AreEqual("TestSecond", displayNames[2]);
            Assert.AreEqual("None", displayNames[3]);
        }

        [Test]
        public void CachedNamesPreservesOrder()
        {
            List<TestEnum> values = new()
            {
                TestEnum.Third,
                TestEnum.First,
                TestEnum.Second,
                TestEnum.None,
            };

            string[] cachedNames = values.ToCachedNames().ToArray();
            Assert.AreEqual("Third", cachedNames[0]);
            Assert.AreEqual("First", cachedNames[1]);
            Assert.AreEqual("Second", cachedNames[2]);
            Assert.AreEqual("None", cachedNames[3]);
        }

        [Test]
        public void HasFlagNoAllocAllUnsignedTypesDoNotAlloc()
        {
            _ = UnsignedShortEnum.First.HasFlagNoAlloc(UnsignedShortEnum.First);
            _ = UnsignedIntEnum.First.HasFlagNoAlloc(UnsignedIntEnum.First);
            _ = UnsignedLongEnum.First.HasFlagNoAlloc(UnsignedLongEnum.First);
            _ = SignedByteEnum.Zero.HasFlagNoAlloc(SignedByteEnum.Zero);
            GCAssert.DoesNotAllocate(() =>
            {
                _ = UnsignedShortEnum.First.HasFlagNoAlloc(UnsignedShortEnum.First);
                _ = UnsignedIntEnum.First.HasFlagNoAlloc(UnsignedIntEnum.First);
                _ = UnsignedLongEnum.First.HasFlagNoAlloc(UnsignedLongEnum.First);
                _ = SignedByteEnum.Zero.HasFlagNoAlloc(SignedByteEnum.Zero);
            });
        }

        [Test]
        public void CachedNameDoesNotAllocate()
        {
            // Pre-warm
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(TestEnum.First.ToString("G"), TestEnum.First.ToCachedName());
                Assert.AreEqual(
                    TinyTestEnum.First.ToString("G"),
                    TinyTestEnum.First.ToCachedName()
                );
                Assert.AreEqual(
                    SmallTestEnum.First.ToString("G"),
                    SmallTestEnum.First.ToCachedName()
                );
                Assert.AreEqual(BigTestEnum.First.ToString("G"), BigTestEnum.First.ToCachedName());
            }

            GCAssert.DoesNotAllocate(() =>
            {
                _ = TestEnum.First.ToCachedName();
            });
            GCAssert.DoesNotAllocate(() =>
            {
                _ = TinyTestEnum.First.ToCachedName();
            });
            GCAssert.DoesNotAllocate(() =>
            {
                _ = SmallTestEnum.First.ToCachedName();
            });
            GCAssert.DoesNotAllocate(() =>
            {
                _ = BigTestEnum.First.ToCachedName();
            });
        }

        [Test]
        public void DisplayNameDoesNotAllocate()
        {
            // Pre-warm
            for (int i = 0; i < 100; i++)
            {
                _ = TestEnum.First.ToDisplayName();
                _ = TinyTestEnum.First.ToDisplayName();
                _ = SmallTestEnum.First.ToDisplayName();
                _ = BigTestEnum.First.ToDisplayName();
            }

            GCAssert.DoesNotAllocate(() =>
            {
                _ = TestEnum.First.ToDisplayName();
            });
            GCAssert.DoesNotAllocate(() =>
            {
                _ = TinyTestEnum.First.ToDisplayName();
            });
            GCAssert.DoesNotAllocate(() =>
            {
                _ = SmallTestEnum.First.ToDisplayName();
            });
            GCAssert.DoesNotAllocate(() =>
            {
                _ = BigTestEnum.First.ToDisplayName();
            });
        }

        [Test]
        public void HasFlagNoAllocEdgeCaseZeroFlag()
        {
            Assert.IsTrue(TestEnum.None.HasFlagNoAlloc(TestEnum.None));
            Assert.IsTrue(TestEnum.First.HasFlagNoAlloc(TestEnum.None));
            Assert.IsTrue(
                (TestEnum.First | TestEnum.Second | TestEnum.Third).HasFlagNoAlloc(TestEnum.None)
            );
        }

        [Test]
        public void DisplayNameWithInvalidEnumValue()
        {
            TestEnum invalidValue = (TestEnum)999;
            string displayName = invalidValue.ToDisplayName();
            Assert.IsNotEmpty(displayName);
        }

        [Test]
        public void CachedNameWithInvalidEnumValue()
        {
            TestEnum invalidValue = (TestEnum)999;
            string cachedName = invalidValue.ToCachedName();
            Assert.IsNotEmpty(cachedName);
        }

        [Test]
        public void HasFlagNoAllocWithInvalidEnumValue()
        {
            TestEnum invalidValue = (TestEnum)999;
            Assert.IsTrue(invalidValue.HasFlagNoAlloc(invalidValue));
            Assert.IsFalse(TestEnum.First.HasFlagNoAlloc(invalidValue));
        }

        [Test]
        public void DisplayNamesWithLargeCollection()
        {
            List<TestEnum> values = new();
            for (int i = 0; i < 1000; i++)
            {
                values.Add(TestEnum.First);
                values.Add(TestEnum.Second);
                values.Add(TestEnum.Third);
            }

            string[] displayNames = values.ToDisplayNames().ToArray();
            Assert.AreEqual(3000, displayNames.Length);
        }

        [Test]
        public void CachedNamesWithLargeCollection()
        {
            List<TestEnum> values = new();
            for (int i = 0; i < 1000; i++)
            {
                values.Add(TestEnum.First);
                values.Add(TestEnum.Second);
                values.Add(TestEnum.Third);
            }

            string[] cachedNames = values.ToCachedNames().ToArray();
            Assert.AreEqual(3000, cachedNames.Length);
        }
    }
}
