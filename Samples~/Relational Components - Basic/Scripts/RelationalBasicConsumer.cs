// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.Relational.Basic
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Minimal, container-free example of Relational Component Attributes.
    /// Attach to a child GameObject with a parent and a sibling to see fields auto-assigned.
    /// </summary>
    public sealed class RelationalBasicConsumer : MonoBehaviour
    {
        [SiblingComponent]
        private Transform siblingTransform;

        [ChildComponent]
        private Collider childCollider;

        [ParentComponent(OnlyAncestors = true, MaxDepth = 1)]
        private Transform directParent;

        private void Awake()
        {
            this.AssignRelationalComponents();
        }

        private void Start()
        {
            string parentName = directParent != null ? directParent.name : "<none>";
            string siblingName = siblingTransform != null ? siblingTransform.name : "<none>";
            string childName = childCollider != null ? childCollider.name : "<none>";
            Debug.Log(
                $"Relational assigned → parent={parentName}, sibling={siblingName}, child={childName}",
                this
            );
        }
    }
}
