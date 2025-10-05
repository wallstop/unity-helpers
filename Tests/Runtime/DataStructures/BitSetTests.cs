namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class BitSetTests
    {
        #region Construction & Initialization

        [Test]
        public void Constructor_WithPositiveCapacity_InitializesCorrectly()
        {
            BitSet bits = new(64);
            Assert.AreEqual(64, bits.Capacity);
            Assert.AreEqual(0, bits.CountSetBits());
            Assert.IsTrue(bits.None());
        }

        [Test]
        public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BitSet(-1));
        }

        [Test]
        public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BitSet(0));
        }

        [TestCase(1)]
        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        [TestCase(127)]
        [TestCase(128)]
        [TestCase(129)]
        [TestCase(1000)]
        public void Constructor_WithVariousCapacities_InitializesCorrectly(int capacity)
        {
            BitSet bits = new(capacity);
            Assert.AreEqual(capacity, bits.Capacity);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        #endregion

        #region TrySet Tests

        [Test]
        public void TrySetSetsBit()
        {
            BitSet bits = new(64);

            Assert.IsTrue(bits.TrySet(5));
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TrySet_WithNegativeIndex_ReturnsFalse()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits.TrySet(-1));
            Assert.IsFalse(bits.TrySet(-100));
        }

        [Test]
        public void TrySet_BeyondCapacity_ExpandsCapacity()
        {
            BitSet bits = new(10);
            Assert.IsTrue(bits.TrySet(50));
            Assert.IsTrue(bits.Capacity >= 51);
            Assert.IsTrue(bits.TryGet(50, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TrySet_AtBoundary_Index63_Works()
        {
            BitSet bits = new(64);
            Assert.IsTrue(bits.TrySet(63));
            Assert.IsTrue(bits.TryGet(63, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TrySet_AtBoundary_Index64_ExpandsAndWorks()
        {
            BitSet bits = new(64);
            Assert.IsTrue(bits.TrySet(64));
            Assert.IsTrue(bits.Capacity > 64);
            Assert.IsTrue(bits.TryGet(64, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TrySet_MultipleBitsInSameWord_Works()
        {
            BitSet bits = new(64);
            for (int i = 0; i < 64; i++)
            {
                bits.TrySet(i);
            }
            Assert.AreEqual(64, bits.CountSetBits());
        }

        [Test]
        public void TrySet_MultipleBitsAcrossWords_Works()
        {
            BitSet bits = new(200);
            bits.TrySet(0);
            bits.TrySet(63);
            bits.TrySet(64);
            bits.TrySet(127);
            bits.TrySet(128);
            bits.TrySet(199);
            Assert.AreEqual(6, bits.CountSetBits());
        }

        [Test]
        public void TrySet_AlreadySetBit_RemainsSet()
        {
            BitSet bits = new(64);
            bits.TrySet(5);
            bits.TrySet(5);
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsTrue(value);
            Assert.AreEqual(1, bits.CountSetBits());
        }

        #endregion

        #region TryClear Tests

        [Test]
        public void TryClearClearsBit()
        {
            BitSet bits = new(64);
            bits.TrySet(5);

            Assert.IsTrue(bits.TryClear(5));
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsFalse(value);
        }

        [Test]
        public void TryClear_WithNegativeIndex_ReturnsFalse()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits.TryClear(-1));
            Assert.IsFalse(bits.TryClear(-100));
        }

        [Test]
        public void TryClear_BeyondCapacity_ReturnsFalse()
        {
            BitSet bits = new(10);
            Assert.IsFalse(bits.TryClear(50));
            Assert.AreEqual(10, bits.Capacity);
        }

        [Test]
        public void TryClear_AlreadyClearedBit_RemainsClear()
        {
            BitSet bits = new(64);
            Assert.IsTrue(bits.TryClear(5));
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsFalse(value);
        }

        [Test]
        public void TryClear_AtBoundaries_Works()
        {
            BitSet bits = new(128);
            bits.SetAll();
            Assert.IsTrue(bits.TryClear(0));
            Assert.IsTrue(bits.TryClear(63));
            Assert.IsTrue(bits.TryClear(64));
            Assert.IsTrue(bits.TryClear(127));
            Assert.AreEqual(124, bits.CountSetBits());
        }

        #endregion

        #region TryFlip Tests

        [Test]
        public void TryFlipTogglesBit()
        {
            BitSet bits = new(64);

            bits.TryFlip(5);
            Assert.IsTrue(bits.TryGet(5, out bool value1));
            Assert.IsTrue(value1);

            bits.TryFlip(5);
            Assert.IsTrue(bits.TryGet(5, out bool value2));
            Assert.IsFalse(value2);
        }

        [Test]
        public void TryFlip_WithNegativeIndex_ReturnsFalse()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits.TryFlip(-1));
            Assert.IsFalse(bits.TryFlip(-100));
        }

        [Test]
        public void TryFlip_BeyondCapacity_ExpandsCapacity()
        {
            BitSet bits = new(10);
            Assert.IsTrue(bits.TryFlip(50));
            Assert.IsTrue(bits.Capacity >= 51);
            Assert.IsTrue(bits.TryGet(50, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TryFlip_MultipleTimes_TogglesCorrectly()
        {
            BitSet bits = new(64);
            for (int i = 0; i < 10; i++)
            {
                bits.TryFlip(5);
                bool expected = i % 2 == 0;
                Assert.IsTrue(bits.TryGet(5, out bool value));
                Assert.AreEqual(expected, value);
            }
        }

        #endregion

        #region TryGet Tests

        [Test]
        public void TryGet_WithNegativeIndex_ReturnsFalseAndFalseValue()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits.TryGet(-1, out bool value));
            Assert.IsFalse(value);
        }

        [Test]
        public void TryGet_BeyondCapacity_ReturnsFalseAndFalseValue()
        {
            BitSet bits = new(10);
            Assert.IsFalse(bits.TryGet(50, out bool value));
            Assert.IsFalse(value);
        }

        [Test]
        public void TryGet_UnsetBit_ReturnsTrueAndFalseValue()
        {
            BitSet bits = new(64);
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsFalse(value);
        }

        [Test]
        public void TryGet_SetBit_ReturnsTrueAndTrueValue()
        {
            BitSet bits = new(64);
            bits.TrySet(5);
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsTrue(value);
        }

        #endregion

        #region Indexer Tests

        [Test]
        public void Indexer_Get_UnsetBit_ReturnsFalse()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits[5]);
        }

        [Test]
        public void Indexer_Get_SetBit_ReturnsTrue()
        {
            BitSet bits = new(64);
            bits.TrySet(5);
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void Indexer_Get_NegativeIndex_ReturnsFalse()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits[-1]);
        }

        [Test]
        public void Indexer_Get_BeyondCapacity_ReturnsFalse()
        {
            BitSet bits = new(10);
            Assert.IsFalse(bits[50]);
        }

        [Test]
        public void Indexer_Set_True_SetsBit()
        {
            BitSet bits = new(64) { [5] = true };
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void Indexer_Set_False_ClearsBit()
        {
            BitSet bits = new(64) { [5] = true, [5] = false };
            Assert.IsFalse(bits[5]);
        }

        [Test]
        public void Indexer_Set_BeyondCapacity_ExpandsAndSets()
        {
            BitSet bits = new(10) { [50] = true };
            Assert.IsTrue(bits.Capacity >= 51);
            Assert.IsTrue(bits[50]);
        }

        #endregion

        #region CountSetBits Tests

        [Test]
        public void CountSetBitsWorks()
        {
            BitSet bits = new(64);
            bits.TrySet(0);
            bits.TrySet(1);
            bits.TrySet(10);

            Assert.AreEqual(3, bits.CountSetBits());
        }

        [Test]
        public void CountSetBits_EmptyBitSet_ReturnsZero()
        {
            BitSet bits = new(64);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        [Test]
        public void CountSetBits_FullBitSet_ReturnsCapacity()
        {
            BitSet bits = new(100);
            bits.SetAll();
            Assert.AreEqual(100, bits.CountSetBits());
        }

        [Test]
        public void CountSetBits_AcrossMultipleWords_Works()
        {
            BitSet bits = new(200);
            for (int i = 0; i < 200; i += 2)
            {
                bits.TrySet(i);
            }
            Assert.AreEqual(100, bits.CountSetBits());
        }

        #endregion

        #region SetAll Tests

        [TestCase(10)]
        [TestCase(64)]
        [TestCase(65)]
        public void SetAllSetsAllBits(int capacity)
        {
            BitSet bits = new(capacity);
            bits.SetAll();

            Assert.AreEqual(capacity, bits.CountSetBits());
            Assert.IsTrue(bits.All());
        }

        [TestCase(1)]
        [TestCase(63)]
        [TestCase(127)]
        [TestCase(129)]
        public void SetAll_VariousCapacities_SetsAllBitsCorrectly(int capacity)
        {
            BitSet bits = new(capacity);
            bits.SetAll();
            Assert.AreEqual(capacity, bits.CountSetBits());
            for (int i = 0; i < capacity; i++)
            {
                Assert.IsTrue(bits[i], $"Bit {i} should be set");
            }
        }

        [Test]
        public void SetAll_AfterPartialSet_SetsRemainingBits()
        {
            BitSet bits = new(100);
            for (int i = 0; i < 50; i++)
            {
                bits.TrySet(i);
            }
            bits.SetAll();
            Assert.AreEqual(100, bits.CountSetBits());
        }

        #endregion

        #region ClearAll Tests

        [Test]
        public void ClearAllClearsAllBits()
        {
            BitSet bits = new(64);
            bits.SetAll();
            bits.ClearAll();

            Assert.AreEqual(0, bits.CountSetBits());
            Assert.IsTrue(bits.None());
        }

        [Test]
        public void ClearAll_EmptyBitSet_RemainsEmpty()
        {
            BitSet bits = new(64);
            bits.ClearAll();
            Assert.AreEqual(0, bits.CountSetBits());
        }

        [Test]
        public void ClearAll_AcrossMultipleWords_Works()
        {
            BitSet bits = new(200);
            bits.SetAll();
            bits.ClearAll();
            Assert.AreEqual(0, bits.CountSetBits());
        }

        #endregion

        #region FlipAll Tests

        [Test]
        public void FlipAll_EmptyBitSet_SetsAllBits()
        {
            BitSet bits = new(64);
            bits.FlipAll();
            Assert.AreEqual(64, bits.CountSetBits());
            Assert.IsTrue(bits.All());
        }

        [Test]
        public void FlipAll_FullBitSet_ClearsAllBits()
        {
            BitSet bits = new(64);
            bits.SetAll();
            bits.FlipAll();
            Assert.AreEqual(0, bits.CountSetBits());
            Assert.IsTrue(bits.None());
        }

        [Test]
        public void FlipAll_PartiallySet_FlipsCorrectly()
        {
            BitSet bits = new(100);
            for (int i = 0; i < 50; i++)
            {
                bits.TrySet(i);
            }
            bits.FlipAll();
            Assert.AreEqual(50, bits.CountSetBits());
            for (int i = 0; i < 50; i++)
            {
                Assert.IsFalse(bits[i]);
            }
            for (int i = 50; i < 100; i++)
            {
                Assert.IsTrue(bits[i]);
            }
        }

        [TestCase(65)]
        [TestCase(127)]
        [TestCase(128)]
        [TestCase(129)]
        public void FlipAll_NonMultipleOf64_FlipsCorrectly(int capacity)
        {
            BitSet bits = new(capacity);
            bits.FlipAll();
            Assert.AreEqual(capacity, bits.CountSetBits());
        }

        #endregion

        #region Not Tests

        [Test]
        public void Not_IsAliasForFlipAll()
        {
            BitSet bits = new(64);
            bits.TrySet(5);
            bits.TrySet(10);
            bits.Not();
            Assert.AreEqual(62, bits.CountSetBits());
            Assert.IsFalse(bits[5]);
            Assert.IsFalse(bits[10]);
        }

        #endregion

        #region Any, None, All Tests

        [Test]
        public void Any_EmptyBitSet_ReturnsFalse()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits.Any());
        }

        [Test]
        public void Any_SingleBitSet_ReturnsTrue()
        {
            BitSet bits = new(64);
            bits.TrySet(5);
            Assert.IsTrue(bits.Any());
        }

        [Test]
        public void None_EmptyBitSet_ReturnsTrue()
        {
            BitSet bits = new(64);
            Assert.IsTrue(bits.None());
        }

        [Test]
        public void None_SingleBitSet_ReturnsFalse()
        {
            BitSet bits = new(64);
            bits.TrySet(5);
            Assert.IsFalse(bits.None());
        }

        [Test]
        public void All_EmptyBitSet_ReturnsFalse()
        {
            BitSet bits = new(64);
            Assert.IsFalse(bits.All());
        }

        [Test]
        public void All_FullBitSet_ReturnsTrue()
        {
            BitSet bits = new(64);
            bits.SetAll();
            Assert.IsTrue(bits.All());
        }

        [Test]
        public void All_PartiallySet_ReturnsFalse()
        {
            BitSet bits = new(64);
            for (int i = 0; i < 63; i++)
            {
                bits.TrySet(i);
            }
            Assert.IsFalse(bits.All());
        }

        [TestCase(65)]
        [TestCase(127)]
        [TestCase(128)]
        [TestCase(129)]
        public void All_NonMultipleOf64_Works(int capacity)
        {
            BitSet bits = new(capacity);
            bits.SetAll();
            Assert.IsTrue(bits.All());
            bits.TryClear(capacity - 1);
            Assert.IsFalse(bits.All());
        }

        #endregion

        #region LeftShift Tests

        [Test]
        public void LeftShiftWorks()
        {
            BitSet bits = new(8);
            bits.TrySet(0);
            bits.TrySet(1);

            bits.LeftShift(2);

            Assert.IsTrue(bits.TryGet(2, out bool v1) && v1);
            Assert.IsTrue(bits.TryGet(3, out bool v2) && v2);
            Assert.IsTrue(bits.TryGet(0, out bool v3));
            Assert.IsFalse(v3);
        }

        [Test]
        public void LeftShift_ByZero_NoChange()
        {
            BitSet bits = new(8);
            bits.TrySet(0);
            bits.TrySet(5);
            bits.LeftShift(0);
            Assert.IsTrue(bits[0]);
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void LeftShift_ByNegative_NoChange()
        {
            BitSet bits = new(8);
            bits.TrySet(0);
            bits.TrySet(5);
            bits.LeftShift(-5);
            Assert.IsTrue(bits[0]);
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void LeftShift_BeyondCapacity_ClearsAll()
        {
            BitSet bits = new(8);
            bits.SetAll();
            bits.LeftShift(10);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        [Test]
        public void LeftShift_ExactlyCapacity_ClearsAll()
        {
            BitSet bits = new(8);
            bits.SetAll();
            bits.LeftShift(8);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        [Test]
        public void LeftShift_AcrossWordBoundary_Works()
        {
            BitSet bits = new(128);
            bits.TrySet(60);
            bits.TrySet(61);
            bits.LeftShift(5);
            Assert.IsTrue(bits[65]);
            Assert.IsTrue(bits[66]);
            Assert.IsFalse(bits[60]);
            Assert.IsFalse(bits[61]);
        }

        [Test]
        public void LeftShift_ShiftsOutHighBits()
        {
            BitSet bits = new(8);
            bits.TrySet(6);
            bits.TrySet(7);
            bits.LeftShift(3);
            Assert.IsFalse(bits[6]);
            Assert.IsFalse(bits[7]);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        #endregion

        #region RightShift Tests

        [Test]
        public void RightShiftWorks()
        {
            BitSet bits = new(8);
            bits.TrySet(5);
            bits.TrySet(6);

            bits.RightShift(2);

            Assert.IsTrue(bits.TryGet(3, out bool v1) && v1);
            Assert.IsTrue(bits.TryGet(4, out bool v2) && v2);
            Assert.IsTrue(bits.TryGet(5, out bool v3));
            Assert.IsFalse(v3);
        }

        [Test]
        public void RightShift_ByZero_NoChange()
        {
            BitSet bits = new(8);
            bits.TrySet(3);
            bits.TrySet(5);
            bits.RightShift(0);
            Assert.IsTrue(bits[3]);
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void RightShift_ByNegative_NoChange()
        {
            BitSet bits = new(8);
            bits.TrySet(3);
            bits.TrySet(5);
            bits.RightShift(-5);
            Assert.IsTrue(bits[3]);
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void RightShift_BeyondCapacity_ClearsAll()
        {
            BitSet bits = new(8);
            bits.SetAll();
            bits.RightShift(10);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        [Test]
        public void RightShift_ExactlyCapacity_ClearsAll()
        {
            BitSet bits = new(8);
            bits.SetAll();
            bits.RightShift(8);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        [Test]
        public void RightShift_AcrossWordBoundary_Works()
        {
            BitSet bits = new(128);
            bits.TrySet(66);
            bits.TrySet(67);
            bits.RightShift(5);
            Assert.IsTrue(bits[61]);
            Assert.IsTrue(bits[62]);
            Assert.IsFalse(bits[66]);
            Assert.IsFalse(bits[67]);
        }

        [Test]
        public void RightShift_ShiftsOutLowBits()
        {
            BitSet bits = new(8);
            bits.TrySet(0);
            bits.TrySet(1);
            bits.RightShift(3);
            Assert.IsFalse(bits[0]);
            Assert.IsFalse(bits[1]);
            Assert.AreEqual(0, bits.CountSetBits());
        }

        #endregion

        #region TryAnd Tests

        [Test]
        public void TryAndWorks()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);

            bits1.TrySet(0);
            bits1.TrySet(1);
            bits2.TrySet(1);
            bits2.TrySet(2);

            bits1.TryAnd(bits2);

            Assert.AreEqual(1, bits1.CountSetBits());
            Assert.IsTrue(bits1.TryGet(1, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TryAnd_WithNull_ReturnsFalse()
        {
            BitSet bits = new(8);
            bits.TrySet(1);
            Assert.IsFalse(bits.TryAnd(null));
            Assert.AreEqual(1, bits.CountSetBits());
        }

        [Test]
        public void TryAnd_DifferentSizes_ResizesToMatch()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(16);
            bits1.TrySet(5);
            bits2.TrySet(5);
            bits2.TrySet(10);

            bits1.TryAnd(bits2);

            Assert.AreEqual(16, bits1.Capacity);
            Assert.IsTrue(bits1[5]);
            Assert.IsFalse(bits1[10]);
        }

        [Test]
        public void TryAnd_NoOverlap_ResultsInEmpty()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);
            bits1.TrySet(0);
            bits1.TrySet(1);
            bits2.TrySet(5);
            bits2.TrySet(6);

            bits1.TryAnd(bits2);

            Assert.AreEqual(0, bits1.CountSetBits());
        }

        [Test]
        public void TryAnd_WithEmpty_ResultsInEmpty()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);
            bits1.SetAll();

            bits1.TryAnd(bits2);

            Assert.AreEqual(0, bits1.CountSetBits());
        }

        #endregion

        #region TryOr Tests

        [Test]
        public void TryOrWorks()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);

            bits1.TrySet(0);
            bits2.TrySet(1);

            bits1.TryOr(bits2);

            Assert.AreEqual(2, bits1.CountSetBits());
        }

        [Test]
        public void TryOr_WithNull_ReturnsFalse()
        {
            BitSet bits = new(8);
            bits.TrySet(1);
            Assert.IsFalse(bits.TryOr(null));
            Assert.AreEqual(1, bits.CountSetBits());
        }

        [Test]
        public void TryOr_DifferentSizes_ExpandsIfNeeded()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(16);
            bits1.TrySet(5);
            bits2.TrySet(10);

            bits1.TryOr(bits2);

            Assert.AreEqual(16, bits1.Capacity);
            Assert.IsTrue(bits1[5]);
            Assert.IsTrue(bits1[10]);
        }

        [Test]
        public void TryOr_WithEmpty_RemainsUnchanged()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);
            bits1.TrySet(1);
            bits1.TrySet(5);

            bits1.TryOr(bits2);

            Assert.AreEqual(2, bits1.CountSetBits());
        }

        [Test]
        public void TryOr_WithOverlap_UnionCorrectly()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);
            bits1.TrySet(0);
            bits1.TrySet(1);
            bits1.TrySet(2);
            bits2.TrySet(1);
            bits2.TrySet(2);
            bits2.TrySet(3);

            bits1.TryOr(bits2);

            Assert.AreEqual(4, bits1.CountSetBits());
            Assert.IsTrue(bits1[0]);
            Assert.IsTrue(bits1[1]);
            Assert.IsTrue(bits1[2]);
            Assert.IsTrue(bits1[3]);
        }

        #endregion

        #region TryXor Tests

        [Test]
        public void TryXor_NoOverlap_SetsUnion()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);
            bits1.TrySet(0);
            bits1.TrySet(1);
            bits2.TrySet(5);
            bits2.TrySet(6);

            bits1.TryXor(bits2);

            Assert.AreEqual(4, bits1.CountSetBits());
            Assert.IsTrue(bits1[0]);
            Assert.IsTrue(bits1[1]);
            Assert.IsTrue(bits1[5]);
            Assert.IsTrue(bits1[6]);
        }

        [Test]
        public void TryXor_WithOverlap_TogglesOverlap()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);
            bits1.TrySet(0);
            bits1.TrySet(1);
            bits1.TrySet(2);
            bits2.TrySet(1);
            bits2.TrySet(2);
            bits2.TrySet(3);

            bits1.TryXor(bits2);

            Assert.AreEqual(2, bits1.CountSetBits());
            Assert.IsTrue(bits1[0]);
            Assert.IsFalse(bits1[1]);
            Assert.IsFalse(bits1[2]);
            Assert.IsTrue(bits1[3]);
        }

        [Test]
        public void TryXor_WithNull_ReturnsFalse()
        {
            BitSet bits = new(8);
            bits.TrySet(1);
            Assert.IsFalse(bits.TryXor(null));
            Assert.AreEqual(1, bits.CountSetBits());
        }

        [Test]
        public void TryXor_DifferentSizes_ExpandsIfNeeded()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(16);
            bits1.TrySet(5);
            bits2.TrySet(5);
            bits2.TrySet(10);

            bits1.TryXor(bits2);

            Assert.AreEqual(16, bits1.Capacity);
            Assert.IsFalse(bits1[5]);
            Assert.IsTrue(bits1[10]);
        }

        [Test]
        public void TryXor_WithIdentical_ResultsInEmpty()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);
            bits1.TrySet(0);
            bits1.TrySet(5);
            bits2.TrySet(0);
            bits2.TrySet(5);

            bits1.TryXor(bits2);

            Assert.AreEqual(0, bits1.CountSetBits());
        }

        #endregion

        #region Resize Tests

        [Test]
        public void ResizeWorks()
        {
            BitSet bits = new(10);
            bits.TrySet(5);

            bits.Resize(20);

            Assert.AreEqual(20, bits.Capacity);
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void Resize_ToSameSize_NoChange()
        {
            BitSet bits = new(10);
            bits.TrySet(5);
            bits.Resize(10);
            Assert.AreEqual(10, bits.Capacity);
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void Resize_ToZero_ThrowsArgumentOutOfRangeException()
        {
            BitSet bits = new(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => bits.Resize(0));
        }

        [Test]
        public void Resize_ToNegative_ThrowsArgumentOutOfRangeException()
        {
            BitSet bits = new(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => bits.Resize(-5));
        }

        [Test]
        public void Resize_ShrinkingLosesBits()
        {
            BitSet bits = new(20);
            bits.TrySet(5);
            bits.TrySet(15);

            bits.Resize(10);

            Assert.AreEqual(10, bits.Capacity);
            Assert.IsTrue(bits[5]);
            Assert.IsFalse(bits.TryGet(15, out _));
        }

        [Test]
        public void Resize_ShrinkingClearsExtraBitsInLastWord()
        {
            BitSet bits = new(100);
            bits.SetAll();
            bits.Resize(65);
            Assert.AreEqual(65, bits.CountSetBits());
        }

        [Test]
        public void Resize_ExpandingPreservesExistingBits()
        {
            BitSet bits = new(10);
            for (int i = 0; i < 10; i++)
            {
                bits.TrySet(i);
            }
            bits.Resize(100);
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(bits[i]);
            }
            for (int i = 10; i < 100; i++)
            {
                Assert.IsFalse(bits[i]);
            }
        }

        #endregion

        #region EnsureCapacity Tests

        [Test]
        public void EnsureCapacity_BelowCurrent_NoChange()
        {
            BitSet bits = new(64);
            bits.EnsureCapacity(32);
            Assert.AreEqual(64, bits.Capacity);
        }

        [Test]
        public void EnsureCapacity_EqualToCurrent_NoChange()
        {
            BitSet bits = new(64);
            bits.EnsureCapacity(64);
            Assert.AreEqual(64, bits.Capacity);
        }

        [Test]
        public void EnsureCapacity_AboveCurrent_Expands()
        {
            BitSet bits = new(64);
            bits.EnsureCapacity(100);
            Assert.IsTrue(bits.Capacity >= 100);
        }

        [Test]
        public void EnsureCapacity_PreservesExistingBits()
        {
            BitSet bits = new(10);
            bits.TrySet(5);
            bits.EnsureCapacity(100);
            Assert.IsTrue(bits[5]);
        }

        [Test]
        public void EnsureCapacity_GrowthStrategy_DoublesSmallCapacities()
        {
            BitSet bits = new(4);
            bits.EnsureCapacity(5);
            Assert.AreEqual(8, bits.Capacity);
        }

        #endregion

        #region TrimExcess Tests

        [Test]
        public void TrimExcess_WithNoBits_ShrinksToMinimum()
        {
            BitSet bits = new(1000);
            bits.TrimExcess();
            Assert.AreEqual(64, bits.Capacity);
        }

        [Test]
        public void TrimExcess_WithBits_ShrinksToHighestSetBit()
        {
            BitSet bits = new(1000);
            bits.TrySet(50);
            bits.TrimExcess();
            Assert.IsTrue(bits.Capacity >= 51);
            Assert.IsTrue(bits.Capacity < 1000);
            Assert.IsTrue(bits[50]);
        }

        [Test]
        public void TrimExcess_WithCustomMinimum_RespectsMinimum()
        {
            BitSet bits = new(1000);
            bits.TrySet(5);
            bits.TrimExcess(100);
            Assert.AreEqual(100, bits.Capacity);
        }

        [Test]
        public void TrimExcess_HighestBitBelowMinimum_UsesMinimum()
        {
            BitSet bits = new(1000);
            bits.TrySet(10);
            bits.TrimExcess(50);
            Assert.AreEqual(50, bits.Capacity);
        }

        [Test]
        public void TrimExcess_HighestBitAboveMinimum_UsesHighestBit()
        {
            BitSet bits = new(1000);
            bits.TrySet(100);
            bits.TrimExcess(50);
            Assert.AreEqual(101, bits.Capacity);
        }

        [Test]
        public void TrimExcess_NoExcessCapacity_NoChange()
        {
            BitSet bits = new(10);
            bits.TrySet(9);
            bits.TrimExcess(10);
            Assert.AreEqual(10, bits.Capacity);
        }

        #endregion

        #region GetSetBits Tests

        [Test]
        public void GetSetBits_ReturnsCorrectIndices()
        {
            BitSet bits = new(100);
            bits.TrySet(5);
            bits.TrySet(10);
            bits.TrySet(99);

            List<int> result = new();
            bits.GetSetBits(result);

            Assert.AreEqual(3, result.Count);
            Assert.Contains(5, result);
            Assert.Contains(10, result);
            Assert.Contains(99, result);
        }

        [Test]
        public void GetSetBits_ClearsList()
        {
            BitSet bits = new(10);
            bits.TrySet(5);

            List<int> result = new() { 999, 888 };
            bits.GetSetBits(result);

            Assert.AreEqual(1, result.Count);
            Assert.Contains(5, result);
        }

        [Test]
        public void GetSetBits_WithNullList_ThrowsArgumentNullException()
        {
            BitSet bits = new(10);
            Assert.Throws<ArgumentNullException>(() => bits.GetSetBits(null));
        }

        [Test]
        public void GetSetBits_EmptyBitSet_ReturnsEmptyList()
        {
            BitSet bits = new(64);
            List<int> result = new();
            bits.GetSetBits(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetSetBits_ReturnsInAscendingOrder()
        {
            BitSet bits = new(100);
            bits.TrySet(99);
            bits.TrySet(5);
            bits.TrySet(50);

            List<int> result = new();
            bits.GetSetBits(result);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(5, result[0]);
            Assert.AreEqual(50, result[1]);
            Assert.AreEqual(99, result[2]);
        }

        #endregion

        #region EnumerateSetIndices Tests

        [Test]
        public void EnumerateSetIndices_ReturnsCorrectIndices()
        {
            BitSet bits = new(100);
            bits.TrySet(5);
            bits.TrySet(10);
            bits.TrySet(99);

            List<int> result = new();
            foreach (int index in bits.EnumerateSetIndices())
            {
                result.Add(index);
            }

            Assert.AreEqual(3, result.Count);
            Assert.Contains(5, result);
            Assert.Contains(10, result);
            Assert.Contains(99, result);
        }

        [Test]
        public void EnumerateSetIndices_EmptyBitSet_ReturnsNothing()
        {
            BitSet bits = new(64);
            List<int> result = new();
            foreach (int index in bits.EnumerateSetIndices())
            {
                result.Add(index);
            }
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void EnumerateSetIndices_ReturnsInAscendingOrder()
        {
            BitSet bits = new(100);
            bits.TrySet(99);
            bits.TrySet(5);
            bits.TrySet(50);

            List<int> result = new(bits.EnumerateSetIndices());

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(5, result[0]);
            Assert.AreEqual(50, result[1]);
            Assert.AreEqual(99, result[2]);
        }

        #endregion

        #region Enumeration Tests

        [Test]
        public void EnumerationYieldsAllBits()
        {
            BitSet bits = new(5);
            bits.TrySet(1);
            bits.TrySet(3);

            List<bool> values = new();
            foreach (bool bit in bits)
            {
                values.Add(bit);
            }

            Assert.AreEqual(5, values.Count);
            Assert.IsFalse(values[0]);
            Assert.IsTrue(values[1]);
            Assert.IsFalse(values[2]);
            Assert.IsTrue(values[3]);
            Assert.IsFalse(values[4]);
        }

        [Test]
        public void Enumeration_EmptyBitSet_YieldsFalses()
        {
            BitSet bits = new(10);
            List<bool> values = new(bits);
            Assert.AreEqual(10, values.Count);
            Assert.IsTrue(values.TrueForAll(b => !b));
        }

        [Test]
        public void Enumeration_FullBitSet_YieldsTrues()
        {
            BitSet bits = new(10);
            bits.SetAll();
            List<bool> values = new(bits);
            Assert.AreEqual(10, values.Count);
            Assert.IsTrue(values.TrueForAll(b => b));
        }

        [Test]
        public void Enumeration_MultipleIterations_Works()
        {
            BitSet bits = new(5);
            bits.TrySet(2);

            int iterations = 0;
            foreach (bool _ in bits)
            {
                iterations++;
            }
            Assert.AreEqual(5, iterations);

            iterations = 0;
            foreach (bool _ in bits)
            {
                iterations++;
            }
            Assert.AreEqual(5, iterations);
        }

        [Test]
        public void Enumeration_AcrossWordBoundaries_Works()
        {
            BitSet bits = new(128);
            bits.TrySet(63);
            bits.TrySet(64);

            List<bool> values = new(bits);
            Assert.AreEqual(128, values.Count);
            Assert.IsTrue(values[63]);
            Assert.IsTrue(values[64]);
        }

        #endregion

        #region IReadOnlyList Tests

        [Test]
        public void Count_ReturnsCapacity()
        {
            BitSet bits = new(100);
            Assert.AreEqual(100, bits.Count);
        }

        [Test]
        public void Count_AfterResize_ReturnsNewCapacity()
        {
            BitSet bits = new(10);
            bits.Resize(50);
            Assert.AreEqual(50, bits.Count);
        }

        #endregion

        #region Complex Scenario Tests

        [Test]
        public void ComplexScenario_MultipleOperations()
        {
            BitSet bits = new(64);
            bits.TrySet(10);
            bits.TrySet(20);
            bits.TrySet(30);
            Assert.AreEqual(3, bits.CountSetBits());

            bits.FlipAll();
            Assert.AreEqual(61, bits.CountSetBits());

            bits.LeftShift(5);
            Assert.IsFalse(bits[10]);
            Assert.IsFalse(bits[15]);

            bits.TrimExcess();
            Assert.IsTrue(bits.Capacity >= 64);
        }

        [Test]
        public void ComplexScenario_BitwiseOperationsChain()
        {
            BitSet bits1 = new(16);
            BitSet bits2 = new(16);
            BitSet bits3 = new(16);

            bits1.TrySet(0);
            bits1.TrySet(5);
            bits1.TrySet(10);

            bits2.TrySet(5);
            bits2.TrySet(10);
            bits2.TrySet(15);

            bits3.TrySet(10);
            bits3.TrySet(15);

            bits1.TryAnd(bits2);
            Assert.AreEqual(2, bits1.CountSetBits());
            Assert.IsTrue(bits1[5]);
            Assert.IsTrue(bits1[10]);

            bits1.TryOr(bits3);
            Assert.AreEqual(3, bits1.CountSetBits());
            Assert.IsTrue(bits1[5]);
            Assert.IsTrue(bits1[10]);
            Assert.IsTrue(bits1[15]);
        }

        [Test]
        public void ComplexScenario_AutoExpansion()
        {
            BitSet bits = new(10);
            Assert.AreEqual(10, bits.Capacity);

            bits[50] = true;
            Assert.IsTrue(bits.Capacity >= 51);
            Assert.IsTrue(bits[50]);

            bits.TryFlip(100);
            Assert.IsTrue(bits.Capacity >= 101);
            Assert.IsTrue(bits[100]);
        }

        [Test]
        public void StressTest_ManyBits()
        {
            BitSet bits = new(10000);
            for (int i = 0; i < 10000; i += 2)
            {
                bits.TrySet(i);
            }
            Assert.AreEqual(5000, bits.CountSetBits());

            bits.FlipAll();
            Assert.AreEqual(5000, bits.CountSetBits());

            for (int i = 1; i < 10000; i += 2)
            {
                Assert.IsTrue(bits[i]);
            }
        }

        [Test]
        public void EdgeCase_AllBoundaries()
        {
            int[] boundaries = { 0, 1, 63, 64, 65, 127, 128, 129 };
            foreach (int size in boundaries)
            {
                BitSet bits = new(size);
                bits.SetAll();
                Assert.AreEqual(size, bits.CountSetBits(), $"Failed at size {size}");
                Assert.IsTrue(bits.All(), $"All() failed at size {size}");

                bits.ClearAll();
                Assert.IsTrue(bits.None(), $"None() failed at size {size}");
            }
        }

        #endregion
    }
}
