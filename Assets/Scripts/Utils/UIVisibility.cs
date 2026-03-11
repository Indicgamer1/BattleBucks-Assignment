using UnityEngine;

namespace BattleBucks.UI
{
    // CanvasGroup visibility — keeps GameObjects active so Awake/OnEnable always fire.
    public static class UIVisibility
    {
        public static void Show(GameObject go)
        {
            CanvasGroup cg    = GetOrAdd(go);
            cg.alpha          = 1f;
            cg.interactable   = true;
            cg.blocksRaycasts = true;
        }

        public static void Hide(GameObject go)
        {
            CanvasGroup cg    = GetOrAdd(go);
            cg.alpha          = 0f;
            cg.interactable   = false;
            cg.blocksRaycasts = false;
        }

        private static CanvasGroup GetOrAdd(GameObject go)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            return cg != null ? cg : go.AddComponent<CanvasGroup>();
        }
    }
}
