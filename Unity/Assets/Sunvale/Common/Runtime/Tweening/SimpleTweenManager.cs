using System;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.Tweening
{
    public static class SimpleTweenManager
    {
        public static TweenClientList<ITweenClient> tweenArray;

        private struct CustomTweenPlayerLoop { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            tweenArray = new TweenClientList<ITweenClient>(10);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallPlayerLoop()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            if (ContainsSystem(loop, typeof(CustomTweenPlayerLoop)))
                return;

            InsertAfter<PreLateUpdate.ScriptRunBehaviourLateUpdate>(
                ref loop,
                new PlayerLoopSystem
                {
                    type = typeof(CustomTweenPlayerLoop),
                    updateDelegate = UpdateTweens
                });

            PlayerLoop.SetPlayerLoop(loop);
        }

        private static void UpdateTweens()
        {
            tweenArray.UpdateDeltaTime(Time.unscaledDeltaTime);
        }

        public static void RegisterTween(ITweenClient tweenClient)
        {
            if (tweenClient.GetIndexNumber() != 0)
                return;

            tweenArray.Add(tweenClient);
        }

        public static void UnregisterTween(ITweenClient tweenClient)
        {
            tweenArray.Remove(tweenClient);
        }

        private static bool InsertAfter<TTarget>(ref PlayerLoopSystem root, PlayerLoopSystem insert)
        {
            var list = root.subSystemList;
            if (list == null)
                return false;

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].type == typeof(TTarget))
                {
                    var newList = new PlayerLoopSystem[list.Length + 1];

                    Array.Copy(list, 0, newList, 0, i + 1);
                    newList[i + 1] = insert;
                    Array.Copy(list, i + 1, newList, i + 2, list.Length - i - 1);

                    root.subSystemList = newList;
                    return true;
                }

                if (InsertAfter<TTarget>(ref list[i], insert))
                {
                    root.subSystemList = list;
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSystem(PlayerLoopSystem root, Type type)
        {
            if (root.type == type)
                return true;

            var list = root.subSystemList;
            if (list == null)
                return false;

            for (int i = 0; i < list.Length; i++)
            {
                if (ContainsSystem(list[i], type))
                    return true;
            }

            return false;
        }
    }
}
