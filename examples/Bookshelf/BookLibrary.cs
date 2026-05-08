// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Resources;

namespace Bookshelf
{
    /// <summary>
    /// QML-visible backend for the Bookshelf example.
    ///
    /// Resource system usage demonstrated here:
    ///
    ///   about.txt  - direct QtResource item; read via Qt.Resources.ReadAllText().
    ///
    ///   synopsis/*.txt  - resolved from Books.resx ResXFileRef entries. Most are packaged as
    ///   native Qt resources only (AccessMode=Default), so C# reads them at runtime through
    ///   Qt.Resources.ReadAllText() using the well-known qrc:/ alias.
    ///
    ///   synopsis/history.txt  - same Qt resource path, but also AccessMode=ManagedAndNative.
    ///   Books.resx is therefore re-added to EmbeddedResource, so the UI can also read
    ///   SynopsisHistory via ResourceManager. VerifyManagedSynopsis() below proves both
    ///   paths agree.
    /// </summary>
    public class BookLibrary
    {
        private const string AssemblyId = "Bookshelf";
        private const string AboutUrl = $"qrc:/assemblies/{AssemblyId}/about.txt";
        private static readonly ResourceManager ManagedResources =
            new("Bookshelf.Books", typeof(BookLibrary).Assembly);

        private string about;

        /// <summary> Text loaded from the packaged about.txt Qt resource. </summary>
        public string About => about ??= LoadAbout();

        /// <summary>
        /// Loads a book synopsis from the packaged Qt resource store.
        /// All four synopses are reachable this way, including the entry that is also
        /// available through managed resources.
        /// </summary>
        public string GetSynopsis(string bookId)
        {
            var url = $"qrc:/assemblies/{AssemblyId}/synopsis/{bookId}.txt";
            return Qt.Resources.Exists(url) ? Qt.Resources.ReadAllText(url) : "";
        }

        /// <summary>
        /// Loads a synopsis through managed <see cref="ResourceManager"/> when that entry was
        /// packaged with <c>AccessMode=ManagedAndNative</c>. Returns an empty string when no
        /// managed copy exists.
        /// </summary>
        public string GetManagedSynopsis(string bookId)
        {
            return bookId switch {
                "history" => ManagedResources.GetString("SynopsisHistory") ?? "",
                _ => ""
            };
        }

        /// <summary>
        /// Returns the synopsis text that should be shown in the UI. Prefer the managed resource
        /// path when available so the example can show that the same content is reachable through
        /// both qrc:/ and ResourceManager.
        /// </summary>
        public string GetDisplaySynopsis(string bookId)
        {
            var managed = GetManagedSynopsis(bookId);
            return string.IsNullOrEmpty(managed) ? GetSynopsis(bookId) : managed;
        }

        /// <summary>
        /// Returns true when the selected synopsis is available through managed resources.
        /// </summary>
        public bool HasManagedSynopsis(string bookId)
        {
            return !string.IsNullOrEmpty(GetManagedSynopsis(bookId));
        }

        /// <summary>
        /// Returns true when the synopsis for <paramref name="bookId"/> can be read
        /// via managed <c>ResourceManager</c> and the result matches the native Qt
        /// resource - confirming that <c>AccessMode=ManagedAndNative</c> is in effect.
        /// Returns false for books whose synopsis has <c>AccessMode=Default</c> (native
        /// only) because <c>ResourceManager</c> cannot reach them.
        /// </summary>
        public bool VerifyManagedSynopsis(string bookId)
        {
            var managed = GetManagedSynopsis(bookId);
            if (string.IsNullOrEmpty(managed))
                return false;
            var native  = GetSynopsis(bookId);
            return managed.Trim() == native.Trim();
        }

        private static string LoadAbout()
        {
            return Qt.Resources.Exists(AboutUrl) ? Qt.Resources.ReadAllText(AboutUrl) : "";
        }
    }
}
