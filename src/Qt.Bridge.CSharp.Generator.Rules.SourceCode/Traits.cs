// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

global using Traits = Qt.Bridge.CodeGeneration.TraitFlags;

namespace Qt.Bridge.CodeGeneration
{
    internal enum TraitIndex
    {
        Name, Src, Dir, Event, File, Fqn, Func, Get,
        Item, Ns, Private, Set, Signal, Target, Version, NoArgs,
        Handler, Var, Init, Enum, Star, Arg
    }

    [Flags]
    public enum TraitFlags
    {
        Default = 0,
        Name = 1 << TraitIndex.Name,
        Src = 1 << TraitIndex.Src,
        Dir = 1 << TraitIndex.Dir,
        Event = 1 << TraitIndex.Event,
        File = 1 << TraitIndex.File,
        Fqn = 1 << TraitIndex.Fqn,
        Func = 1 << TraitIndex.Func,
        Get = 1 << TraitIndex.Get,
        Item = 1 << TraitIndex.Item,
        Ns = 1 << TraitIndex.Ns,
        Private = 1 << TraitIndex.Private,
        Set = 1 << TraitIndex.Set,
        Signal = 1 << TraitIndex.Signal,
        Target = 1 << TraitIndex.Target,
        Version = 1 << TraitIndex.Version,
        NoArgs = 1 << TraitIndex.NoArgs,
        Handler = 1 << TraitIndex.Handler,
        Var = 1 << TraitIndex.Var,
        Init = 1 << TraitIndex.Init,
        Enum = 1 << TraitIndex.Enum,
        Star = 1 << TraitIndex.Star,
        Arg = 1 << TraitIndex.Arg
    }
}
