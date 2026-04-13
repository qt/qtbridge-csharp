// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.DotNet;

namespace Qt.Bridge.Models
{
    /// <summary>
    /// Describes basic per-item state used to determine whether a model item can be interacted
    /// with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement <see cref="IModelItem"/> on custom item types returned by <see
    /// cref="ListModel{T}"/> or <see cref="TableModel{T}"/> when enabled and selectable state
    /// should be controlled by the item itself instead of using the default model behavior.
    /// </para>
    /// <para>
    /// This interface is most useful with richer model items, where different rows or columns may
    /// have different interaction rules. For example, a totals item might be enabled but not
    /// selectable, or a placeholder item might be neither enabled nor selectable.
    /// </para>
    /// <code language="csharp"><![CDATA[
    /// public sealed class ContactItem : IModelItem, IDisplayable, IEditable
    /// {
    ///     public string Name { get; set; }
    ///     public string Email { get; set; }
    ///
    ///     public bool IsEnabled => true;
    ///     public bool IsSelectable => true;
    ///
    ///     public object DisplayValue => Name;
    ///
    ///     public bool IsEditable => true;
    ///     public object EditValue
    ///     {
    ///         get => Name;
    ///         set => Name = value?.ToString() ?? string.Empty;
    ///     }
    /// }
    /// ]]></code>
    /// <para>
    /// In a <see cref="ListModel{T}"/> or <see cref="TableModel{T}"/> based on
    /// <c>ContactItem</c>, the display role would use
    /// <see cref="IDisplayable.DisplayValue"/>, the edit role would use
    /// <see cref="IEditable.EditValue"/>, and enabled/selectable state would come from
    /// <see cref="IModelItem"/>.
    /// </para>
    /// </remarks>
    public interface IModelItem
    {
        /// <summary> Gets whether the item is enabled for user interaction. </summary>
        bool IsEnabled { get; }

        /// <summary> Gets whether the item can be selected. </summary>
        bool IsSelectable { get; }
    }

    /// <summary>
    /// Provides the value that should be shown for a custom model item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement <see cref="IDisplayable"/> on a custom item type used by <see
    /// cref="ListModel{T}"/> or <see cref="TableModel{T}"/> when the default displayed value
    /// should not be the object itself.
    /// </para>
    /// <para>
    /// This is commonly used when an item carries multiple properties, but one of them should be
    /// treated as the main visible value. For example, a contact item might expose both a name and
    /// an email address while showing only the name by default.
    /// </para>
    /// </remarks>
    public interface IDisplayable
    {
        /// <summary>
        /// Gets the value that should be exposed through the built-in display role.
        /// </summary>
        object DisplayValue { get; }
    }

    /// <summary>
    /// Provides editing support for a custom model item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement <see cref="IEditable"/> on a custom item type used by <see cref="ListModel{T}"/>
    /// or <see cref="TableModel{T}"/> when an item should participate in in-place editing through
    /// the built-in edit role.
    /// </para>
    /// <para>
    /// This is useful when the editable form is different from the displayed form. For example, an
    /// item might display a friendly label but expose a raw string or <see cref="DateTime"/> value
    /// for editing.
    /// </para>
    /// </remarks>
    public interface IEditable
    {
        /// <summary> Gets whether the item can currently be edited. </summary>
        bool IsEditable { get; }

        /// <summary> Gets or sets the value exposed through the built-in edit role. </summary>
        object EditValue { get; set; }
    }
}
