// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class Oscillator : MonoBehaviour
    {
        public float speed = 1f;
        public float width = 1f;
        public float height = 1f;

        internal Vector3 _initialLocalPosition;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            float time = Time.time;
            transform.localPosition =
                _initialLocalPosition
                + new Vector3(Mathf.Cos(time * speed) * width, Mathf.Sin(time * speed) * height);
        }
    }
}
