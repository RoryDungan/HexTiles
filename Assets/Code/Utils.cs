using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Utils
{
    /// <summary>
    /// Get a specified type of component from the specified object.
    /// Throws an exception if the component doesn't exist or if there are multiple and it is ambiguous which to choose.
    /// </summary>
    public static ComponentT ExpectComponentInChildren<ComponentT>(this GameObject parent) where ComponentT : Component
    {
        var components = parent.GetComponentsInChildren<ComponentT>();
        if (components.Length < 1)
        {
            throw new ApplicationException(string.Format("Object {0} expected a {1} in children but none was found.", parent.name, typeof(ComponentT).Name));
        }
        else if (components.Length > 1)
        {
            throw new ApplicationException(string.Format("Object {0} expected a single {1} in children but multiple were found.", parent.name, typeof(ComponentT).Name));
        }
        return components[0];
    }

    /// <summary>
    /// Get a named componet on a child of the specified object.
    /// Throws an exception if the component doesn't exist or if there are multiple and it is ambiguous which to choose.
    /// </summary>
    public static ComponentT ExpectComponentInChildren<ComponentT>(this GameObject parent, string name) where ComponentT : Component
    {
        var components = parent.GetComponentsInChildren<ComponentT>()
            .Where(component => component.gameObject.name == name)
            .ToArray();

        if (components.Length < 1)
        {
            throw new ApplicationException(string.Format("Object {0} expected a {1} in children but none was found.", parent.name, typeof(ComponentT).Name));
        }
        else if (components.Length > 1)
        {
            throw new ApplicationException(string.Format("Object {0} expected a single {1} in children but multiple were found.", parent.name, typeof(ComponentT).Name));
        }
        return components[0];
    }

    /// <summary>
    /// Get a named object from the children of the specified object.
    /// Throws an exception if the GameObject doesn't exist or if there are multiple and it is ambiguous which to choose.
    /// </summary>
    public static GameObject ExpectObjectInChildren(this GameObject parent, string name)
    {
        var components = parent.GetComponentsInChildren<Transform>()
            .Where(trans => trans.gameObject.name == name)
            .ToArray();

        if (components.Length < 1)
        {
            throw new ApplicationException(string.Format("Object {0} expected a child named {1} but none was found.", parent.name, name));
        }
        else if (components.Length > 1)
        {
            throw new ApplicationException(string.Format("Object {0} expected a single child named {1} but multiple were found.", parent.name, name));
        }
        return components[0].gameObject;
    }

    /// <summary>
    /// Find a component of specified type in the scene and throw an exception if it was not found or multiple were found.
    /// </summary>
    public static ComponentT ExpectComponentInScene<ComponentT>() where ComponentT : Component
    {
        var components = UnityEngine.Object.FindObjectsOfType<ComponentT>();
        if (components.Length < 1)
        {
            throw new ApplicationException(string.Format("Tried to find an instance of {0} in the scene but it was not present.", typeof(ComponentT).Name));
        }
        else if (components.Length > 1)
        {
            throw new ApplicationException(string.Format("Tried to find a single instance of {0} in the scene but multiple were found.", typeof(ComponentT).Name));
        }
        return components[0];
    }
}
