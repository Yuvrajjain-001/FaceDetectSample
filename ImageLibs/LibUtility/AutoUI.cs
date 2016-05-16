using System;

namespace Dpu.Utility
{  
    /// <summary>
    /// A callback when an observable object has changed,
    /// typically invoked by calling Update() on the object.
    /// </summary>
    public delegate void DataChanged(object sender);

    /// <summary>
    /// An object that can be observed for changes
    /// </summary>
    public interface IObservable 
    {
        event DataChanged Changed;
        void Refresh();
    }

    /// <summary>
    /// A tagging interface indicating that an object has "action"
    /// methods.  Action methods should be public, have no arguments
    /// and be tagged with the [Action] attribute.
    /// </summary>
    public interface IActions { }

    /// <summary>
    /// A tagging interface indicating that an object has "variable"
    /// fields/properties.  Variables should be public, be of a basic
    /// type (int, string, bool, enum), should be gettable and settable,
    /// and should be tagged with the [Variable] attribute.
    /// </summary>
    public interface IVariables { }


    /// <summary>
    /// An example has an Id.  Also, every field/property
    /// with the Variable attribute will get rendered in
    /// the examples list.
    /// </summary>
    public interface IDataItem : IVariables
    {
        int Id { get; }
    }

    /// <summary>
    /// A custom attribute which labels properties to be exported as variables.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class VariableAttribute : Attribute
    {
        public bool Visible = true;
        public string DefaultValue = "";
        public string Description = "";

        public VariableAttribute() {}
    }
    
    /// <summary>
    /// A custom attribute which labels methods to be exported as actions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionAttribute : Attribute
    {
        public int DisplayOrder = int.MaxValue;
        public string Shortcut = "None";
        public string Description = "";

        public ActionAttribute() {}
    }

    /// <summary>
    /// This proxy can hide an observable, and call
    /// back the observer on the control's thread
    /// </summary>
    public class ObservationWrapper : IObservable
    {
        public object UserData;
        public ObservationWrapper(object userData) { UserData = userData; }
        public event DataChanged Changed;
        public void Refresh()
        {
            object[] args = new object[] { this };
            if(Changed != null)
            {
                foreach(Delegate del in Changed.GetInvocationList())
                {
                    del.DynamicInvoke(args);
                }
            }
        }
    }


    /// <summary>
    /// This proxy can hide an observable, and call
    /// back the observer on the control's thread
    /// </summary>
    public class BaseObservable : IObservable
    {
        public event DataChanged Changed;
        public void Refresh()
        {
            object[] args = new object[] { this };
            if(Changed != null)
            {
                foreach(Delegate del in Changed.GetInvocationList())
                {
                    del.DynamicInvoke(args);
                }
            }
        }
    }

    /// <summary>
    /// A custom attribute which labels properties to be exported as variables.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SubActionsAttribute : Attribute
    {
        public int DisplayOrder = int.MaxValue;
        public SubActionsAttribute() {}
    }

    // Define a custom attribute with one named variable.
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayOnlyAttribute : Attribute
    {
        public DisplayOnlyAttribute() {}
    }
}
