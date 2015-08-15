using System;

namespace AppInstall.UI
{
    public class Field<T>
    {
        /// <summary>
        /// Describes the meaning of the field
        /// </summary>
        public string Header { get; protected set; }

        /// <summary>
        /// Specifies if the editor of this field needs a large display space.
        /// </summary>
        public bool HasLargeEditor { get; protected set; }

        /// <summary>
        /// Specifies if the editor of this field has a built-in display of the value being edited.
        /// </summary>
        public bool EditorShowsValue { get; protected set; }

        /// <summary>
        /// Should return a human readable text representation of the field value.
        /// </summary>
        public Func<T, string> TextConstructor { get; protected set; }

        /// <summary>
        /// Should return null or construct a view that allows editing of the value.
        /// First argument: data item for which the editor is to be constructed.
        /// Second argument: action that should be invoked after the value has changed.
        /// </summary>
        public Func<T, Action, View> EditorConstructor { get; protected set; }
    }
}