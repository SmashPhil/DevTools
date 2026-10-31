using UnityEngine;
using Verse;

namespace DevTools;

[StaticConstructorOnStartup]
internal class CoroutineObject : MonoBehaviour
{
  static CoroutineObject()
  {
    InjectToScene();
  }

  internal static CoroutineObject Instance { get; private set; }

  private static void InjectToScene()
  {
    LongEventHandler.ExecuteWhenFinished(delegate()
    {
      GameObject gameObject = new GameObject("CoroutineManager");
      CoroutineObject manager = gameObject.AddComponent<CoroutineObject>();
      DontDestroyOnLoad(gameObject);
      Instance = manager;
    });
  }
}