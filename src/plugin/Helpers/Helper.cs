using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MegabonkTogether.Helpers
{
    public static class Il2CppFindHelper
    {
        private static MethodInfo findObjectsByType;
        private static MethodInfo getCurrentAnimatorClipInfo;
        private static MethodBase setSharedMaterials;
        private static MethodInfo getSharedMaterials;
        private static MethodInfo getSharedMaterial;
        private static MethodInfo sphereCastAll;
        private static MethodInfo getComponentsInChildren;
        private static MemberInfo getComponents;

        private static Type sharedMaterialsParamType;
        private static PropertyInfo sharedMaterialsiL2cppArrayIndexer;
        private static ConstructorInfo sharedMaterialsiI2cppArrayCtor;


        public static GameObject[] FindAllGameObjects()
        {
            if (findObjectsByType == null)
            {
                findObjectsByType = AccessTools.GetDeclaredMethods(typeof(UnityEngine.Object))
                    .FirstOrDefault(m => m.Name == "FindObjectsByType" && m.IsGenericMethodDefinition);

                if (findObjectsByType == null)
                {
                    Plugin.Log.LogError("Could not find FindObjectsByType<T>");
                    return Array.Empty<GameObject>();
                }
            }

            var closedMethod = findObjectsByType.MakeGenericMethod(typeof(GameObject));
            var result = closedMethod.Invoke(null, [FindObjectsSortMode.None]);

            if (result is Il2CppArrayBase<GameObject> array)
                return [.. array];

            return Array.Empty<GameObject>();
        }

        public static AnimatorClipInfo[] RuntimeGetCurrentAnimatorClipInfo(this Animator animator, int layer)
        {
            if (getCurrentAnimatorClipInfo == null)
            {
                getCurrentAnimatorClipInfo = AccessTools.GetDeclaredMethods(typeof(Animator))
                    .FirstOrDefault(m => m.Name == "GetCurrentAnimatorClipInfo" && m.GetParameters().Length == 1);
                if (getCurrentAnimatorClipInfo == null)
                {
                    Plugin.Log.LogError("Could not find Animator.GetCurrentAnimatorClipInfo");
                    return Array.Empty<AnimatorClipInfo>();
                }
            }

            var result = getCurrentAnimatorClipInfo.Invoke(animator, [layer]);
            if (result is Il2CppArrayBase<AnimatorClipInfo> array)
                return [.. array];

            return Array.Empty<AnimatorClipInfo>();
        }

        public static RaycastHit[] RuntimeSphereCastAll(Ray ray, float radius, float maxDistance, int layerMask)
        {
            if (sphereCastAll == null)
            {
                sphereCastAll = AccessTools.GetDeclaredMethods(typeof(Physics))
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "SphereCastAll") return false;
                        var parameters = m.GetParameters();
                        return parameters.Length == 4 &&
                               parameters[0].ParameterType == typeof(Ray) &&
                               parameters[1].ParameterType == typeof(float) &&
                               parameters[2].ParameterType == typeof(float) &&
                               parameters[3].ParameterType == typeof(int);
                    });

                if (sphereCastAll == null)
                {
                    Plugin.Log.LogError("Could not find Physics.SphereCastAll(Ray, float, float, int)");
                    return Array.Empty<RaycastHit>();
                }
            }

            var result = sphereCastAll.Invoke(null, new object[] { ray, radius, maxDistance, layerMask });

            if (result is Il2CppArrayBase<RaycastHit> array)
                return [.. array];

            return Array.Empty<RaycastHit>();
        }


        private static bool EnsureSetSharedMaterials()
        {
            if (setSharedMaterials != null) return true;

            setSharedMaterials = AccessTools.PropertySetter(typeof(Renderer), "sharedMaterials");

            if (setSharedMaterials == null)
                return false;

            sharedMaterialsParamType = setSharedMaterials.GetParameters()[0].ParameterType;

            if (!sharedMaterialsParamType.IsArray)
            {
                sharedMaterialsiL2cppArrayIndexer = AccessTools.Property(sharedMaterialsParamType, "Item");
                sharedMaterialsiI2cppArrayCtor = AccessTools.Constructor(sharedMaterialsParamType, [typeof(int)]);
            }

            return true;
        }

        public static void RuntimeSetSharedMaterials(this Renderer renderer, Material[] materials)
        {
            if (!EnsureSetSharedMaterials())
            {
                Plugin.Log.LogError("[RuntimeSetSharedMaterials] set_sharedMaterials not found via reflection.");
                return;
            }

            if (sharedMaterialsiI2cppArrayCtor == null || sharedMaterialsiL2cppArrayIndexer == null)
            {
                Plugin.Log.LogError("[RuntimeSetSharedMaterials] Unexpected Il2Cpp array param type and no ctor/indexer found: " + sharedMaterialsParamType);
                return;
            }

            object il2cppArr = sharedMaterialsiI2cppArrayCtor.Invoke([materials.Length]);

            for (int i = 0; i < materials.Length; i++)
            {
                sharedMaterialsiL2cppArrayIndexer.SetValue(il2cppArr, materials[i], [i]);
            }

            object arrayArg = il2cppArr;

            try
            {
                setSharedMaterials.Invoke(renderer, [arrayArg]);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[RuntimeSetSharedMaterials] Invocation exception: " + ex);
            }
        }

        private static bool EnsureGetSharedMaterials()
        {
            if (getSharedMaterials != null) return true;
            getSharedMaterials = AccessTools.PropertyGetter(typeof(Renderer), "sharedMaterials");
            if (getSharedMaterials == null)
                return false;
            sharedMaterialsParamType = getSharedMaterials.ReturnType;
            if (!sharedMaterialsParamType.IsArray)
            {
                sharedMaterialsiL2cppArrayIndexer = AccessTools.Property(sharedMaterialsParamType, "Item");
                sharedMaterialsiI2cppArrayCtor = AccessTools.Constructor(sharedMaterialsParamType, [typeof(int)]);
            }
            return true;
        }

        public static Material[] RuntimeGetSharedMaterials(this Renderer renderer)
        {
            if (!EnsureGetSharedMaterials())
            {
                Plugin.Log.LogError("[RuntimeGetSharedMaterials] get_sharedMaterials not found via reflection.");
                return Array.Empty<Material>();
            }
            object result = null;
            try
            {
                result = getSharedMaterials.Invoke(renderer, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[RuntimeGetSharedMaterials] Invocation exception: " + ex);
                return Array.Empty<Material>();
            }
            if (result is Il2CppArrayBase<Material> array)
                return [.. array];
            return Array.Empty<Material>();
        }

        private static bool EnsureGetSharedMaterial()
        {
            if (getSharedMaterial != null) return true;
            getSharedMaterial = AccessTools.PropertyGetter(typeof(Renderer), "sharedMaterial");
            return getSharedMaterial != null;
        }

        public static Material RuntimeGetSharedMaterial(this Renderer renderer)
        {
            if (!EnsureGetSharedMaterial())
            {
                Plugin.Log.LogError("[RuntimeGetSharedMaterial] get_sharedMaterial not found via reflection.");
                return null;
            }
            object result = null;
            try
            {
                result = getSharedMaterial.Invoke(renderer, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[RuntimeGetSharedMaterial] Invocation exception: " + ex);
                return null;
            }
            return result as Material;
        }

        public static T[] RuntimeGetComponentsInChildren<T>(this GameObject gameObject, bool includeInactive = false) where T : UnityEngine.Object
        {
            if (getComponentsInChildren == null)
            {
                getComponentsInChildren = AccessTools.GetDeclaredMethods(typeof(GameObject))
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "GetComponentsInChildren" || !m.IsGenericMethodDefinition) return false;
                        var parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
                    });

                if (getComponentsInChildren == null)
                {
                    Plugin.Log.LogError("Could not find GameObject.GetComponentsInChildren<T>(bool)");
                    return Array.Empty<T>();
                }
            }

            var closedMethod = getComponentsInChildren.MakeGenericMethod(typeof(T));
            var result = closedMethod.Invoke(gameObject, [includeInactive]);

            if (result is Il2CppArrayBase<T> array)
                return [.. array];

            return Array.Empty<T>();
        }

        public static T[] RuntimeGetComponents<T>(this GameObject gameObject) where T : UnityEngine.Object
        {
            if (getComponents == null)
            {
                getComponents = AccessTools.GetDeclaredMethods(typeof(GameObject))
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "GetComponents" || !m.IsGenericMethodDefinition) return false;
                        var parameters = m.GetParameters();
                        return parameters.Length == 0;
                    });
                if (getComponents == null)
                {
                    Plugin.Log.LogError("Could not find GameObject.GetComponents<T>()");
                    return Array.Empty<T>();
                }
            }
            var closedMethod = ((MethodInfo)getComponents).MakeGenericMethod(typeof(T));
            var result = closedMethod.Invoke(gameObject, Array.Empty<object>());
            if (result is Il2CppArrayBase<T> array)
                return [.. array];
            return Array.Empty<T>();
        }

        public static T[] RuntimeGetComponents<T>(this Component component) where T : UnityEngine.Object
        {
            return component.gameObject.RuntimeGetComponents<T>();
        }

        public static T[] RuntimeGetComponentsInChildren<T>(this Component component, bool includeInactive = false) where T : UnityEngine.Object
        {
            return component.gameObject.RuntimeGetComponentsInChildren<T>(includeInactive);
        }
    }
}
