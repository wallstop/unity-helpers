namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CollisionProxy : MonoBehaviour
    {
        public event Action<Collision2D> OnCollisionEnter = _ => { };
        public event Action<Collision2D> OnCollisionStay = _ => { };
        public event Action<Collision2D> OnCollisionExit = _ => { };

        public event Action<Collider2D> OnTriggerEnter = _ => { };
        public event Action<Collider2D> OnTriggerStay = _ => { };
        public event Action<Collider2D> OnTriggerExit = _ => { };

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnTriggerEnter?.Invoke(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            OnTriggerStay?.Invoke(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OnTriggerExit?.Invoke(other);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            OnCollisionEnter?.Invoke(other);
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            OnCollisionStay?.Invoke(other);
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            OnCollisionExit?.Invoke(other);
        }
    }
}
