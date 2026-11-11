// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using PeopleDirectory.Core.Models;

namespace PeopleDirectory.Core.ViewModels;

public interface IViewModelFactory
{
    LoadingViewModel CreateLoading();
    UsersViewModel CreateUsers();
    UserDetailsViewModel CreateUserDetails(User user, UsersViewModel origin);
}
