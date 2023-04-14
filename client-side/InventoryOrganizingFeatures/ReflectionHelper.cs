using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryOrganizingFeatures
{
    internal static class ReflectionHelper
    {
        public static Type FindClassTypeByMethodNames(string[] methodNames)
        {
            return AccessTools.AllTypes().Where(type =>
            {
                if (type.IsClass)
                {
                    var methods = AccessTools.GetMethodNames(type);
                    return methodNames.All(searchedMethodName => methods.Contains(searchedMethodName));
                }
                return false;
            }).First();
        }

        public static T InvokeMethod<T>(object targetObj, string methodName, object[] args)
        {
            return (T)AccessTools.Method(targetObj.GetType(), methodName).Invoke(targetObj, args);
        }
    }
}
