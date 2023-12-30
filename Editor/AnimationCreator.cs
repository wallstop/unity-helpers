namespace Editor.Dev
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Utilities;

    public static class AnimationCreator
    {
        [MenuItem("Assets/Create Animation From Sprites")]
        private static void CreateAnimation()
        {
            List<ObjectReferenceKeyframe> keyFrames = new();
            float currentTime = 0f;
            const float timestep = 0.1f;
            foreach (Texture2D texture in Selection.objects.OfType<Texture2D>())
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(texture));
                if (sprite == null)
                {
                    continue;
                }
                ObjectReferenceKeyframe keyframe = new() {time = currentTime, value = sprite};
                keyFrames.Add(keyframe);

                currentTime += timestep;
            }

            AnimationClip animationClip = new();
            AnimationUtility.SetObjectReferenceCurve(animationClip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keyFrames.ToArray());

            ProjectWindowUtil.CreateAsset(animationClip, AssetDatabase.GetAssetPath(Selection.activeObject) + ".anim");
        }

        [MenuItem("Assets/Create Animation From Sprites", true)]
        private static bool CreateAnimationValidator()
        {
            return Selection.objects.All(obj => obj is Texture2D);
        }
    }
}
