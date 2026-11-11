// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.ComponentModel;
using PeopleDirectory.Core.ViewModels;

namespace PeopleDirectory.Core.Navigation;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase Current { get; }
    void NavigateTo(ViewModelBase viewModel);
}
