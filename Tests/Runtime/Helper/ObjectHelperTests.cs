namespace UnityHelpers.Tests.Tests.Runtime.Helper
{
    using System.Collections;
    using Core.Helper;
    using JetBrains.Annotations;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    [UsedImplicitly]
    public sealed class ObjectHelperComponent : MonoBehaviour { }

    public sealed class ObjectHelperTests
    {
        [UnityTest]
        public IEnumerator HasComponent()
        {
            GameObject go = new("Test SpriteRenderer", typeof(SpriteRenderer));
            SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();

            Assert.IsTrue(go.HasComponent(typeof(SpriteRenderer)));
            Assert.IsTrue(go.HasComponent<SpriteRenderer>());
            Assert.IsTrue(spriteRenderer.HasComponent<SpriteRenderer>());
            Assert.IsTrue(spriteRenderer.HasComponent(typeof(SpriteRenderer)));

            Assert.IsFalse(go.HasComponent<LineRenderer>());
            Assert.IsFalse(go.HasComponent(typeof(LineRenderer)));
            Assert.IsFalse(spriteRenderer.HasComponent<LineRenderer>());
            Assert.IsFalse(spriteRenderer.HasComponent(typeof(LineRenderer)));

            Object obj = go;
            Assert.IsTrue(obj.HasComponent<SpriteRenderer>());
            Assert.IsTrue(obj.HasComponent(typeof(SpriteRenderer)));
            Assert.IsFalse(obj.HasComponent<LineRenderer>());
            Assert.IsFalse(obj.HasComponent(typeof(LineRenderer)));

            yield break;
        }

        [UnityTest]
        public IEnumerator EnableRendererRecursively()
        {
            GameObject one = New("1");
            GameObject two = New("2");
            two.transform.SetParent(one.transform);
            GameObject three = New("3");
            three.transform.SetParent(two.transform);
            GameObject four = New("4");
            four.transform.SetParent(three.transform);

            // Act
            two.transform.EnableRendererRecursively<SpriteRenderer>(false);
            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            three.transform.EnableRendererRecursively<SpriteRenderer>(true);

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            one.transform.EnableRendererRecursively<SpriteRenderer>(true);

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            two.transform.EnableRendererRecursively<SpriteRenderer>(
                false,
                renderer => renderer.gameObject == three
            );

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            one.transform.EnableRendererRecursively<SpriteRenderer>(
                true,
                renderer => renderer.gameObject == four
            );

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(four.GetComponent<CircleCollider2D>().enabled);

            yield break;

            GameObject New(string name)
            {
                return new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D));
            }
        }

        [UnityTest]
        public IEnumerator EnableRecursively()
        {
            GameObject one = New("1");
            GameObject two = New("2");
            two.transform.SetParent(one.transform);
            GameObject three = New("3");
            three.transform.SetParent(two.transform);
            GameObject four = New("4");
            four.transform.SetParent(three.transform);

            // Act
            two.transform.EnableRecursively<CircleCollider2D>(false);
            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsFalse(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsFalse(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsFalse(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            three.transform.EnableRecursively<CircleCollider2D>(true);

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsFalse(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            one.transform.EnableRecursively<CircleCollider2D>(true);

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            two.transform.EnableRecursively<CircleCollider2D>(
                false,
                collider => collider.gameObject == three
            );

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsFalse(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsFalse(four.GetComponent<CircleCollider2D>().enabled);

            // Act
            one.transform.EnableRecursively<CircleCollider2D>(
                true,
                collider => collider.gameObject == four
            );

            Assert.IsTrue(one.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(three.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(four.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(one.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(two.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(three.GetComponent<CircleCollider2D>().enabled);
            Assert.IsFalse(four.GetComponent<CircleCollider2D>().enabled);

            yield break;

            GameObject New(string name)
            {
                return new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D));
            }
        }

        [UnityTest]
        public IEnumerator DestroyAllChildGameObjects()
        {
            GameObject one = new("1");
            GameObject two = new("2");
            two.transform.SetParent(one.transform);
            GameObject three = new("3");
            three.transform.SetParent(two.transform);
            GameObject four = new("4");
            four.transform.SetParent(three.transform);

            // Act
            two.DestroyAllChildrenGameObjects();
            yield return null;

            Assert.IsTrue(one != null);
            Assert.IsTrue(two != null);
            Assert.IsTrue(three == null);
            Assert.IsTrue(four == null);

            three = new GameObject("3");
            three.transform.SetParent(two.transform);
            four = new GameObject("4");
            four.transform.SetParent(three.transform);

            // Act
            one.DestroyAllChildrenGameObjects();
            yield return null;

            Assert.IsTrue(one != null);
            Assert.IsTrue(two == null);
            Assert.IsTrue(three == null);
            Assert.IsTrue(four == null);
        }

        [UnityTest]
        public IEnumerator DestroyAllComponentsOfType()
        {
            GameObject one = New("1");
            Assert.AreEqual(4, one.GetComponents<ObjectHelperComponent>().Length);

            GameObject two = New("2");
            two.transform.SetParent(one.transform);

            one.DestroyAllComponentsOfType<ObjectHelperComponent>();
            yield return null;
            Assert.AreEqual(0, one.GetComponents<ObjectHelperComponent>().Length);
            Assert.IsTrue(one.GetComponent<SpriteRenderer>() != null);
            Assert.AreEqual(4, two.GetComponents<ObjectHelperComponent>().Length);

            two.DestroyAllComponentsOfType<ObjectHelperComponent>();
            yield return null;
            Assert.AreEqual(0, one.GetComponents<ObjectHelperComponent>().Length);
            Assert.IsTrue(one.GetComponent<SpriteRenderer>() != null);
            Assert.AreEqual(0, two.GetComponents<ObjectHelperComponent>().Length);
            Assert.IsTrue(two.GetComponent<SpriteRenderer>() != null);

            GameObject New(string name)
            {
                return new GameObject(
                    name,
                    typeof(SpriteRenderer),
                    typeof(ObjectHelperComponent),
                    typeof(ObjectHelperComponent),
                    typeof(ObjectHelperComponent),
                    typeof(ObjectHelperComponent)
                );
            }
        }

        [UnityTest]
        public IEnumerator SmartDestroy()
        {
            GameObject one = new("1");

            one.SmartDestroy();
            yield return null;
            Assert.IsTrue(one == null);

            GameObject two = new("2");
            two.SmartDestroy(1.5f);
            yield return null;
            Assert.IsTrue(two != null);
            yield return new WaitForSeconds(1.6f);

            Assert.IsTrue(two == null);
        }

        [UnityTest]
        public IEnumerator DestroyAllChildrenGameObjectsImmediatelyConditionally()
        {
            GameObject one = new("1");
            GameObject two = new("2");
            two.transform.SetParent(one.transform);
            GameObject three = new("3");
            three.transform.SetParent(two.transform);
            GameObject four = new("4");
            four.transform.SetParent(two.transform);

            two.DestroyAllChildrenGameObjectsImmediatelyConditionally(go => go == four);
            Assert.IsTrue(one != null);
            Assert.IsTrue(two != null);
            Assert.IsTrue(three != null);
            Assert.IsTrue(four == null);

            one.DestroyAllChildrenGameObjectsImmediatelyConditionally(go => go != two);
            Assert.IsTrue(one != null);
            Assert.IsTrue(two != null);
            Assert.IsTrue(three != null);
            Assert.IsTrue(four == null);

            one.DestroyAllChildrenGameObjectsImmediatelyConditionally(go => go == two);
            Assert.IsTrue(one != null);
            Assert.IsTrue(two == null);
            Assert.IsTrue(three == null);
            Assert.IsTrue(four == null);

            yield break;
        }

        [UnityTest]
        public IEnumerator DestroyAllChildGameObjectsConditionally()
        {
            GameObject one = new("1");
            GameObject two = new("2");
            two.transform.SetParent(one.transform);
            GameObject three = new("3");
            three.transform.SetParent(two.transform);
            GameObject four = new("4");
            four.transform.SetParent(two.transform);

            two.DestroyAllChildGameObjectsConditionally(go => go == four);
            yield return null;
            Assert.IsTrue(one != null);
            Assert.IsTrue(two != null);
            Assert.IsTrue(three != null);
            Assert.IsTrue(four == null);

            one.DestroyAllChildGameObjectsConditionally(go => go != two);
            yield return null;
            Assert.IsTrue(one != null);
            Assert.IsTrue(two != null);
            Assert.IsTrue(three != null);
            Assert.IsTrue(four == null);

            one.DestroyAllChildGameObjectsConditionally(go => go == two);
            yield return null;
            Assert.IsTrue(one != null);
            Assert.IsTrue(two == null);
            Assert.IsTrue(three == null);
            Assert.IsTrue(four == null);
        }

        [UnityTest]
        public IEnumerator DestroyAllChildrenGameObjectsImmediately()
        {
            GameObject one = new("1");
            GameObject two = new("2");
            two.transform.SetParent(one.transform);
            GameObject three = new("3");
            three.transform.SetParent(two.transform);
            GameObject four = new("4");
            four.transform.SetParent(two.transform);

            two.DestroyAllChildrenGameObjectsImmediately();
            Assert.IsTrue(one != null);
            Assert.IsTrue(two != null);
            Assert.IsTrue(three == null);
            Assert.IsTrue(four == null);

            three = new GameObject("3");
            three.transform.SetParent(two.transform);
            four = new("4");
            four.transform.SetParent(two.transform);

            one.DestroyAllChildrenGameObjectsImmediately();
            Assert.IsTrue(one != null);
            Assert.IsTrue(two == null);
            Assert.IsTrue(three == null);
            Assert.IsTrue(four == null);

            yield break;
        }
    }
}
