using Assets.Scripts.Objects.Pooling;
using HarmonyLib;
using Il2CppInterop.Runtime;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MegabonkTogether.Helpers
{
    /// <summary>
    /// A projectile done has to return to a pool that should have been created by the game.
    /// We are ensuring here to create the pool if it does not exist yet to prevent some exceptions.
    /// </summary>
    public static class PoolHelper
    {
        private static ConstructorInfo objectPoolCtor;

        private static bool EnsureObjectPoolConstructor()
        {
            if (objectPoolCtor != null) return true;

            try
            {
                var objectPoolType = typeof(UnityEngine.Pool.ObjectPool<GameObject>);

                objectPoolCtor = AccessTools.GetDeclaredConstructors(objectPoolType)
                    .FirstOrDefault(c =>
                    {
                        var parameters = c.GetParameters();
                        if (parameters.Length != 7) return false;

                        return parameters[0].ParameterType.FullName.Contains("Il2CppSystem.Func") &&
                               parameters[1].ParameterType.FullName.Contains("Il2CppSystem.Action") &&
                               parameters[2].ParameterType.FullName.Contains("Il2CppSystem.Action") &&
                               parameters[3].ParameterType.FullName.Contains("Il2CppSystem.Action") &&
                               parameters[4].ParameterType == typeof(bool) &&
                               parameters[5].ParameterType == typeof(int) &&
                               parameters[6].ParameterType == typeof(int);
                    });

                if (objectPoolCtor == null)
                {
                    Plugin.Log.LogError("Could not find ObjectPool<GameObject> constructor via reflection");

                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in EnsureObjectPoolConstructor: {ex}");
                return false;
            }
        }

        public static void EnsureWeaponPoolExists(EWeapon weaponType)
        {
            if (!PoolManager.Instance.weaponAttackPools.ContainsKey(weaponType))
            {
                if (!EnsureObjectPoolConstructor())
                {
                    Plugin.Log.LogWarning($"Could not create pool for {weaponType}, adding null entry");
                    PoolManager.Instance.weaponAttackPools.TryAdd(weaponType, null);
                    return;
                }

                try
                {
                    System.Func<GameObject> createFuncNet = () => null;
                    System.Action<GameObject> actionOnGetNet = (obj) => { };
                    System.Action<GameObject> actionOnReleaseNet = (obj) => { };
                    System.Action<GameObject> actionOnDestroyNet = (obj) => { };

                    var createFunc = DelegateSupport.ConvertDelegate<Il2CppSystem.Func<GameObject>>(createFuncNet);
                    var actionOnGet = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<GameObject>>(actionOnGetNet);
                    var actionOnRelease = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<GameObject>>(actionOnReleaseNet);
                    var actionOnDestroy = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<GameObject>>(actionOnDestroyNet);

                    var pool = objectPoolCtor.Invoke(
                    [
                        createFunc,
                        actionOnGet,
                        actionOnRelease,
                        actionOnDestroy,
                        true,
                        10,
                        1000
                    ]);

                    PoolManager.Instance.weaponAttackPools.TryAdd(weaponType, (UnityEngine.Pool.ObjectPool<GameObject>)pool);
                    PoolManager.Instance.projectilePools.TryAdd(weaponType, (UnityEngine.Pool.ObjectPool<GameObject>)pool);
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogError($"Error creating pool for {weaponType}: {ex}");
                    PoolManager.Instance.weaponAttackPools.TryAdd(weaponType, null);
                    PoolManager.Instance.projectilePools.TryAdd(weaponType, null);
                }
            }
        }
    }
}
