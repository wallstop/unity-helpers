// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Core;

    /// <summary>
    /// Tests for the child height change signaling mechanism used to fix
    /// nested foldout overlap issues in SerializableDictionary and SerializableSet.
    /// </summary>
    /// <remarks>
    /// The SignalChildHeightChanged() methods set a static frame counter that causes
    /// the row render cache to be invalidated when a child property drawer's foldout
    /// state changes (e.g., "Collection Styling (Advanced)" foldout in nested properties).
    /// </remarks>
    [TestFixture]
    public sealed class SerializableCollectionChildHeightSignalTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();
        }

        [TearDown]
        public override void TearDown()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();
            base.TearDown();
        }

        [Test]
        public void DictionarySignalChildHeightChangedMethodExists()
        {
            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            int frameValue =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.GreaterOrEqual(
                frameValue,
                0,
                "SignalChildHeightChanged should set frame to a valid value."
            );
        }

        [Test]
        public void SetSignalChildHeightChangedMethodExists()
        {
            SerializableSetPropertyDrawer.SignalChildHeightChanged();

            int frameValue = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.GreaterOrEqual(
                frameValue,
                0,
                "SignalChildHeightChanged should set frame to a valid value."
            );
        }

        [Test]
        public void DictionarySignalSetsFrameCounterToCurrentFrame()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();
            int expectedFrame = Time.frameCount;

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            int actualFrame =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(
                expectedFrame,
                actualFrame,
                "Signal should set frame counter to current frame."
            );
        }

        [Test]
        public void SetSignalSetsFrameCounterToCurrentFrame()
        {
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();
            int expectedFrame = Time.frameCount;

            SerializableSetPropertyDrawer.SignalChildHeightChanged();

            int actualFrame = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(
                expectedFrame,
                actualFrame,
                "Signal should set frame counter to current frame."
            );
        }

        [Test]
        public void DictionaryResetClearsFrameCounter()
        {
            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();

            int frameValue =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(-1, frameValue, "Reset should set frame counter to -1.");
        }

        [Test]
        public void SetResetClearsFrameCounter()
        {
            SerializableSetPropertyDrawer.SignalChildHeightChanged();

            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();

            int frameValue = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(-1, frameValue, "Reset should set frame counter to -1.");
        }

        [Test]
        public void DictionaryMultipleSignalsInSameFrameDoNotCauseIssues()
        {
            int expectedFrame = Time.frameCount;

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();
            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();
            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            int actualFrame =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(
                expectedFrame,
                actualFrame,
                "Multiple signals in same frame should result in same frame value."
            );
        }

        [Test]
        public void SetMultipleSignalsInSameFrameDoNotCauseIssues()
        {
            int expectedFrame = Time.frameCount;

            SerializableSetPropertyDrawer.SignalChildHeightChanged();
            SerializableSetPropertyDrawer.SignalChildHeightChanged();
            SerializableSetPropertyDrawer.SignalChildHeightChanged();

            int actualFrame = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(
                expectedFrame,
                actualFrame,
                "Multiple signals in same frame should result in same frame value."
            );
        }

        [Test]
        public void DictionaryFrameCounterInitializesToNegativeOne()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();

            int frameValue =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();

            Assert.AreEqual(-1, frameValue, "Frame counter should initialize to -1.");
        }

        [Test]
        public void SetFrameCounterInitializesToNegativeOne()
        {
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();

            int frameValue = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();

            Assert.AreEqual(-1, frameValue, "Frame counter should initialize to -1.");
        }

        [Test]
        public void DictionarySignalIsIdempotentWithinSameFrame()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();
            int firstCallFrame =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();
            int secondCallFrame =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();

            Assert.AreEqual(
                firstCallFrame,
                secondCallFrame,
                "Signal should be idempotent within the same frame."
            );
        }

        [Test]
        public void SetSignalIsIdempotentWithinSameFrame()
        {
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();

            SerializableSetPropertyDrawer.SignalChildHeightChanged();
            int firstCallFrame = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();

            SerializableSetPropertyDrawer.SignalChildHeightChanged();
            int secondCallFrame =
                SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();

            Assert.AreEqual(
                firstCallFrame,
                secondCallFrame,
                "Signal should be idempotent within the same frame."
            );
        }

        [Test]
        public void DictionarySignalValueIsNonNegativeAfterSignaling()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            int frameValue =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.GreaterOrEqual(
                frameValue,
                0,
                "Frame value should be non-negative after signaling."
            );
        }

        [Test]
        public void SetSignalValueIsNonNegativeAfterSignaling()
        {
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();

            SerializableSetPropertyDrawer.SignalChildHeightChanged();

            int frameValue = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.GreaterOrEqual(
                frameValue,
                0,
                "Frame value should be non-negative after signaling."
            );
        }

        [Test]
        public void BothDrawersCanBeSignaledIndependently()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            int dictionaryFrame =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            int setFrame = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();

            Assert.AreNotEqual(
                dictionaryFrame,
                setFrame,
                "Dictionary and Set drawers should have independent frame counters."
            );
            Assert.GreaterOrEqual(dictionaryFrame, 0, "Dictionary frame should be set.");
            Assert.AreEqual(-1, setFrame, "Set frame should remain unset.");
        }

        [Test]
        public void SignalingBothDrawersUpdatesFrameIndependently()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();
            SerializableSetPropertyDrawer.SignalChildHeightChanged();

            int dictionaryFrame =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            int setFrame = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();

            Assert.AreEqual(
                dictionaryFrame,
                setFrame,
                "Both drawers should have same frame when signaled in same frame."
            );
            Assert.AreEqual(
                Time.frameCount,
                dictionaryFrame,
                "Dictionary frame should match current frame."
            );
            Assert.AreEqual(Time.frameCount, setFrame, "Set frame should match current frame.");
        }

        [Test]
        public void DictionaryResetDoesNotAffectSetDrawer()
        {
            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();
            SerializableSetPropertyDrawer.SignalChildHeightChanged();
            int setFrameBeforeReset =
                SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();

            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();

            int setFrameAfterReset =
                SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(
                setFrameBeforeReset,
                setFrameAfterReset,
                "Resetting Dictionary should not affect Set drawer."
            );
        }

        [Test]
        public void SetResetDoesNotAffectDictionaryDrawer()
        {
            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();
            SerializableSetPropertyDrawer.SignalChildHeightChanged();
            int dictionaryFrameBeforeReset =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();

            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();

            int dictionaryFrameAfterReset =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            Assert.AreEqual(
                dictionaryFrameBeforeReset,
                dictionaryFrameAfterReset,
                "Resetting Set should not affect Dictionary drawer."
            );
        }

        [Test]
        public void DictionarySignalMatchesCurrentTimeFrameCount()
        {
            SerializableDictionaryPropertyDrawer.ResetChildHeightChangedFrameForTests();
            int frameBefore = Time.frameCount;

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            int signalFrame =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();
            int frameAfter = Time.frameCount;

            Assert.GreaterOrEqual(
                signalFrame,
                frameBefore,
                "Signal frame should be at least the frame before signaling."
            );
            Assert.LessOrEqual(
                signalFrame,
                frameAfter,
                "Signal frame should be at most the frame after signaling."
            );
        }

        [Test]
        public void SetSignalMatchesCurrentTimeFrameCount()
        {
            SerializableSetPropertyDrawer.ResetChildHeightChangedFrameForTests();
            int frameBefore = Time.frameCount;

            SerializableSetPropertyDrawer.SignalChildHeightChanged();

            int signalFrame = SerializableSetPropertyDrawer.GetChildHeightChangedFrameForTests();
            int frameAfter = Time.frameCount;

            Assert.GreaterOrEqual(
                signalFrame,
                frameBefore,
                "Signal frame should be at least the frame before signaling."
            );
            Assert.LessOrEqual(
                signalFrame,
                frameAfter,
                "Signal frame should be at most the frame after signaling."
            );
        }
    }
}
