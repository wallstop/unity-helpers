namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class CircleLineRendererTests
    {
        [UnityTest]
        public IEnumerator UpdateSyncsEnabledWithCollider()
        {
            GameObject go = new(
                "Circle",
                typeof(LineRenderer),
                typeof(CircleCollider2D),
                typeof(CircleLineRenderer)
            );
            LineRenderer lr = go.GetComponent<LineRenderer>();
            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            CircleLineRenderer clr = go.GetComponent<CircleLineRenderer>();

            clr.SendMessage("Awake");

            col.enabled = true;
            clr.SendMessage("Update");
            yield return null;
            Assert.IsTrue(lr.enabled);

            col.enabled = false;
            clr.SendMessage("Update");
            yield return null;
            Assert.IsFalse(lr.enabled);
        }

        [UnityTest]
        public IEnumerator OnValidateWarnsOnInvalidValues()
        {
            GameObject go = new(
                "Circle",
                typeof(LineRenderer),
                typeof(CircleCollider2D),
                typeof(CircleLineRenderer)
            );
            CircleLineRenderer clr = go.GetComponent<CircleLineRenderer>();

            clr.SendMessage("Awake");
            clr.numSegments = 2;
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(".*Invalid number of segments.*")
            );
            clr.SendMessage("OnValidate");
            yield return null;

            clr.updateRateSeconds = 0;
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(".*Invalid update rate.*")
            );
            clr.SendMessage("OnValidate");
            yield return null;

            clr.minLineWidth = 1f;
            clr.maxLineWidth = 0.5f;
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(".*MaxLineWidth.*MinLineWidth.*")
            );
            clr.SendMessage("OnValidate");
            yield return null;
        }
    }
}
