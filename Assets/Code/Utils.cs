using System;
using System.Collections;
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
    /// Expect a component to exist on a specified GameObject and throw an exception if it doesn't.
    /// Unity's built-in GameObject.GetComponet just returns null if the component doesn't exist, which can lead to 
    /// NullRefrenceExceptions later on if a component has been removed in the inspector but is still referenced in code.
    /// This way, we get an early warning as soon as we look for the component, which makes these kind of errors easier to
    /// track down.
    /// </summary>
    public static ComponentT ExpectComponent<ComponentT>(this GameObject gameObject) where ComponentT : Component
    {
        var component = gameObject.GetComponent<ComponentT>();
        if (component == null)
        {
            throw new ApplicationException(string.Format("Tried to get component of type {0} on object {1} but it could not be found", typeof(ComponentT).Name, gameObject.name));
        }

        return component;
    }

    /// <summary>
    /// Find a component of specified type in the scene and throw an exception if it was not found or multiple were found.
    /// </summary>
    public static ComponentT ExpectComponentInGame<ComponentT>() where ComponentT : Component
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

    /// <summary>
    /// Find a component of specified type in the scene and throw an exception if it was not found or multiple were found.
    /// </summary>
    public static ComponentT ExpectComponentInGame<ComponentT>(string name) where ComponentT : Component
    {
        var components = UnityEngine.Object.FindObjectsOfType<ComponentT>()
            .Where(component => component.gameObject.name == name)
            .ToArray();

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

    /// <summary>
    /// Coroutine that can perform an action after a timer has elapsed.
    /// </summary>
    public static IEnumerator DoAfterSeconds(float time, Action action)
    {
        yield return new WaitForSeconds(time);

        action();
    }

    /// <summary>
    /// Coroutine that can perform an action at the end of the frame.
    /// </summary>
    public static IEnumerator DoOnEndOfFrame(Action action)
    {
        yield return new WaitForEndOfFrame();

        action();
    }

    /// <summary>
    /// Returns the modulo of two floats. Needed because C#'s '%' operator
    /// actually returns the remainder of integer division as opposed to a modulo.
    /// See http://stackoverflow.com/questions/1082917/mod-of-negative-number-is-melting-my-brain 
    /// </summary>
    public static float Mod(float a, float b)
    {
        return (a % b + b) % b;
    }

    /// <summary>
    /// "Smoother" smoothstep. Taken from https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
    /// Assumes value between 0 and 1.
    /// </summary>
    public static float SmootherStep(float value)
    {
        return value*value*value * (value * (6f*value - 15f) + 10f);
    }

    /// <summary>
    /// Ease out. Taken from https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
    /// Assumes value between 0 and 1.
    /// </summary>
    public static float Sinerp(float value)
    {
        return Mathf.Sin(value * Mathf.PI * 0.5f);
    }

    /// <summary>
    /// Super-smooth ease out.
    /// </summary>
    public static float SmoothSinerp(float value)
    {
        return Utils.Sinerp(Utils.SmootherStep(value));
    }
}
