namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Attributes;
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TagHandler))]
    public sealed class CollisionSenses : MonoBehaviour
    {
        public const string CollisionDisabledTag = nameof(CollisionDisabledTag);

        [SiblingComponent]
        private TagHandler _tagHandler;

        private readonly List<Collider2D> _managedColliders = new();

        private void Awake()
        {
            this.AssignRelationalComponents();
        }

        private void OnEnable()
        {
            if (_tagHandler.HasTag(CollisionDisabledTag))
            {
                StartManagingColliders();
            }

            _tagHandler.OnTagAdded += CheckForTagAddition;
            _tagHandler.OnTagRemoved += CheckForTagRemoval;
        }

        private void OnDisable()
        {
            _tagHandler.OnTagAdded -= CheckForTagAddition;
            _tagHandler.OnTagRemoved -= CheckForTagRemoval;
            StopManagingColliders();
        }

        private void CheckForTagAddition(string addedTag)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (string.Equals(addedTag, CollisionDisabledTag, StringComparison.Ordinal))
            {
                StartManagingColliders();
            }
        }

        private void CheckForTagRemoval(string removedTag)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (string.Equals(removedTag, CollisionDisabledTag, StringComparison.Ordinal))
            {
                StopManagingColliders();
            }
        }

        private void StopManagingColliders()
        {
            foreach (Collider2D managedCollider in _managedColliders)
            {
                if (managedCollider != null)
                {
                    managedCollider.enabled = true;
                }
            }

            _managedColliders.Clear();
        }

        private void StartManagingColliders()
        {
            GetComponentsInChildren(_managedColliders);
            _managedColliders.RemoveAll(managed => !managed.enabled);
            foreach (Collider2D managedCollider in _managedColliders)
            {
                managedCollider.enabled = false;
            }
        }
    }
}
