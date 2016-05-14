using System;
using System.Windows;

/// <summary>
/// Interface Descriptions
/// </summary>
namespace FaceSortUI
{
    /// <summary>
    /// Interface for any container that can hold a collection of faces
    /// The purpose is to give Groups and the MainCanvas a common base
    /// </summary>
	public interface IFaceContainer
	{
        /// <summary>
        /// Make a face known to me
        /// </summary>
        /// <param name="face">Face to add</param>
        void AddFace(Face face);

        /// <summary>
        /// Remove a face from my Children List. There are two levels of
        /// removal,controlled by the flag doDestroy 
        /// </summary>
        /// <param name="face">Face to Remove</param>
        /// <param name="doDestroy">When false just remove from the child list. When true means face should be destroyed
        /// so remove from internall _AllFaceList too</param>
        void RemoveFace(Face face, bool doDestroy);

	}

    /// <summary>
    /// Interface for any displayable object. The purpose of this
    /// interface is to give Face and Group objectts a common base
    /// </summary>
    public interface IDisplayableElement
    {
        /// <summary>
        /// Returns reference to my logical parent
        /// </summary>
        IDisplayableElement MyParent { get;}
        /// <summary>
        /// Returns my unique ID
        /// </summary>
        int MyID { get;}
        /// <summary>
        /// Synchronizes the logical tree of ID's from object references
        /// Typically called before  Serialization
        /// </summary>
        void SyncParentID();
        /// <summary>
        /// Synchronizes the logical tree of references from
        /// ID's. Typically called after deserialization.
        /// </summary>
        /// <param name="backgroundCanvas">The main canvas</param>
        void RebuildTree(BackgroundCanvas backgroundCanvas);
        /// <summary>
        /// Return my position relative to the main canvas
        /// </summary>
        /// <returns>Location </returns>
        Point GetPositionRelativeToCanvas();

        /// <summary>
        /// Sync current display with current
        /// options selected in OptionsDialog
        /// </summary>
        void SyncDisplayOptions();
    }

}
