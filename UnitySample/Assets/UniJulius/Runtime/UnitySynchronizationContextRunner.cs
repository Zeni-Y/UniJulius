using UnityEngine;

namespace UniJulius.Runtime
{
    class UnitySynchronizationContextRunner : MonoBehaviour
    {
        static UnitySynchronizationContextRunner instance;
        UnitySynchronizationContext context;

        public static void Begin(UnitySynchronizationContext context)
        {
            if (instance != null) return;
            var go = new GameObject("SynchronizationContextRunner");
            instance = go.AddComponent<UnitySynchronizationContextRunner>();
            DontDestroyOnLoad(go);
            instance.context = context;
        }

        void Update()
        {
            if (context == null) return;
            context.Update();
        }
    }
}