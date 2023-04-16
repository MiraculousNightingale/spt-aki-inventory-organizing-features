using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            }).FirstOrDefault();
        }

        public static MethodInfo FindMethodByArgTypes(object instance, Type[] methodArgTypes)
        {
            return FindMethodByArgTypes(instance.GetType(), methodArgTypes);
        }

        public static MethodInfo FindMethodByArgTypes(Type type, Type[] methodArgTypes)
        {
            //type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(method => method.)
            return null;
        }

        public static T InvokeMethod<T>(object targetObj, string methodName, object[] args, Type[] methodArgTypes = null)
        {
            return (T)AccessTools.Method(targetObj.GetType(), methodName, methodArgTypes).Invoke(targetObj, args);
        }
    }
}
