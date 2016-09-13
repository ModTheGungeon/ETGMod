#pragma warning disable 0626
#pragma warning disable 0649

using MonoMod;
using UnityEngine;

internal static class patch_BraveTime {

    private static System.Collections.Generic.Dictionary<GameObject, bool> m_post = new System.Collections.Generic.Dictionary<GameObject, bool>();
    [MonoModIgnore] private static System.Collections.Generic.List<GameObject> m_sources;
    [MonoModIgnore] private static System.Collections.Generic.List<float> m_multipliers;

    public static void SetTimeScaleModifierIsPost(bool isPost, GameObject source) {
        if (!m_sources.Contains(source)) {
            m_sources.Add(source);
            m_multipliers.Add(1f);
        }

        if (!m_post.ContainsKey(source)) {
            m_post.Add(source, false);
        }

        int index = m_sources.IndexOf(source);
        m_post[source] = isPost;
        UpdateTimeScale();
    }

    private static void UpdateTimeScale() {
        float scale = 1f;

        bool isPost;
        for (int i = 0; i < m_multipliers.Count; i++) {
            if (!m_post.TryGetValue(m_sources[i], out isPost) || !isPost) {
                scale = m_multipliers[i] * scale;
            }
        }

        scale = Mathf.Clamp(scale, 0f, 1f);

        for (int i = 0; i < m_multipliers.Count; i++) {
            if (m_post.TryGetValue(m_sources[i], out isPost) && isPost) {
                scale = m_multipliers[i] * scale;
            }
        }

        if (float.IsNaN(scale)) {
            Debug.LogError("TIMESCALE WAS MY NAN ALL ALONG");
            scale = 1f;
        }

        scale = Mathf.Clamp(scale, 0f, 100f);
        Time.timeScale = scale;
    }

}
