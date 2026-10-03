using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assemblies;

#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * Code referenced from adammyhre from github.
 */
[Serializable]
public class SerializableType : ISerializationCallbackReceiver {
    [SerializeField] string assemblyQualifiedName = string.Empty;

    public Type Type { get; private set; }

    void ISerializationCallbackReceiver.OnBeforeSerialize() {
        assemblyQualifiedName = Type?.AssemblyQualifiedName ?? assemblyQualifiedName;
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() {
        if (!TryGetType(assemblyQualifiedName, out var type)) {
            return;
        }
        Type = type;
    }

    static bool TryGetType(string typeString, out Type type) {
        type = Type.GetType(typeString);
        return type != null || !string.IsNullOrEmpty(typeString);
    }

    // Implicit conversion from SerializableType to Type
    public static implicit operator Type(SerializableType sType) => sType.Type;

    // Implicit conversion from Type to SerializableType
    public static implicit operator SerializableType(Type type) => new() { Type = type };
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SerializableType))]
public class SerializableTypeDrawer : PropertyDrawer {
    TypeFilterAttribute typeFilter;
    string[] typeNames, typeFullNames;

    void Initialize() {
        if (typeFullNames != null) return;

        typeFilter = (TypeFilterAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(TypeFilterAttribute));

        var filteredTypes = CurrentAssemblies.GetLoadedAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => typeFilter == null ? DefaultFilter(t) : typeFilter.Filter(t))
            .ToArray();

        typeNames = filteredTypes.Select(t => t.ReflectedType == null ? t.Name : $"{t.ReflectedType.Name + t.Name}").ToArray();
        typeFullNames = filteredTypes.Select(t => t.AssemblyQualifiedName).ToArray();
    }

    static bool DefaultFilter(Type type) {
        return !type.IsAbstract && !type.IsInterface && !type.IsGenericType;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        Initialize();
        var typeIdProperty = property.FindPropertyRelative("assemblyQualifiedName");

        if (string.IsNullOrEmpty(typeIdProperty.stringValue)) {
            typeIdProperty.stringValue = typeFullNames.First();
            property.serializedObject.ApplyModifiedProperties();
        }

        var currentIndex = Array.IndexOf(typeFullNames, typeIdProperty.stringValue);
        var selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, typeNames);

        if (selectedIndex >= 0 && selectedIndex != currentIndex) {
            typeIdProperty.stringValue = typeFullNames[selectedIndex];
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif // UNITY_EDITOR

public class TypeFilterAttribute : PropertyAttribute {
    public Func<Type, bool> Filter { get; }

    public TypeFilterAttribute(Type filterType) {
        Filter = type => !type.IsAbstract &&
                         !type.IsInterface &&
                         !type.IsGenericType &&
                         type.InheritsOrImplements(filterType);
    }
}

public static class TypeExtensions {
    public static bool InheritsOrImplements(this Type type, Type baseType) {
        type = ResolveGenericType(type);
        baseType = ResolveGenericType(baseType);

        while (type != typeof(object)) {
            if (baseType == type || HasAnyInterfaces(type, baseType)) return true;

            type = ResolveGenericType(type.BaseType);
            if (type == null) return false;
        }

        return false;
    }

    static Type ResolveGenericType(Type type) {
        if (type is not { IsGenericType: true }) return type;

        var genericType = type.GetGenericTypeDefinition();
        return genericType != type ? genericType : type;
    }

    static bool HasAnyInterfaces(Type type, Type interfaceType) {
        return type.GetInterfaces().Any(i => ResolveGenericType(i) == interfaceType);
    }
}
