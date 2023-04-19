using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryOrganizingFeatures.Reflections
{
    internal class ReflectionBase
    {
        public object ReflectedInstance { get; set; }
        public Type ReflectedType { get; set; }
        public T GetFieldValue<T>(string fieldName)
        {
            return (T)AccessTools.Field(ReflectedType, fieldName).GetValue(ReflectedInstance);
        }

        public T GetPropertyValue<T>(string propertyName)
        {
            return (T)AccessTools.Property(ReflectedType, propertyName).GetValue(ReflectedInstance);
        }

        public T InvokeMethod<T>(string methodName, object[] args = null, Type[] methodArgTypes = null)
        {
            return (T)InvokeMethod(methodName, args, methodArgTypes);
        }

        public object InvokeMethod(string methodName, object[] args = null, Type[] methodArgTypes = null)
        {
            return AccessTools.Method(ReflectedType, methodName, methodArgTypes).Invoke(ReflectedInstance, args);
        }
    }
}
