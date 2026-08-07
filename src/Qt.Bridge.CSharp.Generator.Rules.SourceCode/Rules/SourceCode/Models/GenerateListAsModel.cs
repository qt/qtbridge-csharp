// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using System.Text;
using Qt.Bridge.Utils.Text;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode.Models
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateListAsModel : Class.GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src)
            => src is Type type && type.IsList(out _) && !type.IsObservableList(out _)
            && type.ExportAsSourceCode();
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type || !type.IsList(out var itemType))
                return Error();

            // normalize the effective item type
            (itemType, var isArray) = ResolveListLike(type, itemType);

            // Gather public readable instance properties of item type. We skip indexers and static
            // properties. For built-ins we keep it empty.
            var typeProperties = Array.Empty<PropertyInfo>();
            if (!itemType.IsValue()) {
                typeProperties = itemType.GetProperties()
                    .Where(p => !p.IsStatic())
                    .Where(p => p.GetIndexParameters().Length == 0) // no indexers
                    .Where(p => p.GetGetMethod() != null)
                    .Where(p => !p.IsIgnored())
                    .ToArray();
            }

            // Build role-name list from property names (camelCase), avoiding duplicates and
            // clashing with the reserved "item" role.
            var rawRoleNames = typeProperties
                .Select(p => p.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel))
                .Where(n => !string.Equals(n, "item", StringComparison.Ordinal))
                .ToList();
            var distinctRoleNames = new List<string>(rawRoleNames.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in rawRoleNames) {
                var candidate = name;
                var suffix = 2;
                while (!seen.Add(candidate))
                    candidate = name + suffix++;
                distinctRoleNames.Add(candidate);
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Includes) is not { } includes)
                return Error();
            includes += "#include <QAbstractListModel>";

            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMemberDeclaration)
                return Error();

            // rowCountOverride is a temporary override used only during remove deltas.
            // Rationale:
            //   When a .NET ObservableCollection<T> raises CollectionChanged(Remove),
            //   the underlying Count has ALREADY been decremented. However, Qt's
            //   beginRemoveRows()/endRemoveRows() contract requires the model to still
            //   report the PRE-REMOVAL row count until endRemoveRows() completes.
            //   To satisfy this, we set d->rowCountOverride = (currentCount + removed)
            //   just for the duration of the remove notification, so rowCount() returns
            //   the "old" count and Qt invariants stay intact (avoids assertions).
            privateMemberDeclaration += "qint32 rowCountOverride = -1;";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(QObjectBaseClass) is not { } qObjectBase)
                return Error();
            qObjectBase.Reset();
            qObjectBase += "public QAbstractListModel";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
int rowCount(const QModelIndex &parent = QModelIndex()) const override;
{Blank}";
            methods += $@"
QVariant data(const QModelIndex &index, int role) const override;
{Blank}";
            methods += $@"
QHash<int, QByteArray> roleNames() const override;
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();

            // roles initializer: "item" + property roles
            var rolesInitializer = new StringBuilder("{ { Qt::UserRole, \"item\" }");
            for (var i = 0; i < distinctRoleNames.Count; ++i)
                rolesInitializer.Append($", {{ Qt::UserRole + {i + 1}, \"{distinctRoleNames[i]}\" }}");
            rolesInitializer.Append(" }");

            var itemAccessor = isArray ? "get" : "item";
            var sizeAccessor = isArray ? "length" : "count";
            var (propertyGuard, dataBranches) = EmitGuardAndBranches(distinctRoleNames,
                itemAccessor, itemType, typeProperties);

            implementation += $@"
QHash<int, QByteArray> {type.MFn(Ns | Name)}::roleNames() const
{{
    static QHash<int, QByteArray> roles {rolesInitializer};
    return roles;
}}

int {type.MFn(Ns | Name)}::rowCount(const QModelIndex &parent) const
{{
    // If a remove delta is in progress, report the pre-removal count so that
    // Qt's beginRemoveRows()/endRemoveRows() contract holds.
    if (d && d->rowCountOverride >= 0)
        return d->rowCountOverride;
    return {sizeAccessor}(false);
}}

QVariant {type.MFn(Ns | Name)}::data(const QModelIndex &index, int role) const
{{
    if (index.row() < 0 || index.row() >= {sizeAccessor}())
        return QVariant();

    if (role == Qt::UserRole)
        return QVariant::fromValue({itemAccessor}(index.row()));
{propertyGuard}{dataBranches}
    return QVariant();
}}
{Blank}";
            return Ok;
        }

        private static (Type ItemType, bool IsTrueArray) ResolveListLike(Type t, Type currentItemType)
        {
            if (t.IsArray)
                return (t.GetElementType(), true);

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ArraySegment<>))
                return (t.GetGenericArguments()[0], false);

            var types = t.IsInterface ? new[] { t }.Concat(t.GetInterfaces()) : t.GetInterfaces();
            foreach (var type in types) {
                if (!type.IsGenericType)
                    continue;
                var def = type.GetGenericTypeDefinition();
                if (def == typeof(IList<>) || def == typeof(ICollection<>)
                    || def == typeof(IEnumerable<>) || def == typeof(IReadOnlyList<>)) {
                    return (type.GetGenericArguments()[0], false);
                }
            }

            return typeof(System.Collections.IList).IsAssignableFrom(t)
                ? (typeof(object), false)
                : (currentItemType, false);
        }

        private static (string, string) EmitGuardAndBranches(List<string> distinctRoleNames,
            string itemAccessor, Type itemType, PropertyInfo[] typeProperties)
        {
            // A guard is only needed if the list item is a QObject (not a value type)
            // AND there are property roles to access.
            var needsGuard = !itemType.IsValue() && distinctRoleNames.Count > 0;

            // Generate the guard block (if needed) for property roles:
            // Checks if the QObject is null or invalid before accessing properties.
            var propertyGuard = needsGuard ? $@"
    // Guard for property roles: lists may contain null or invalid objects
    auto *it = {itemAccessor}(index.row());
    if (!it || !it->isValid())
        return QVariant();
"
                : "";

            // Generate safe property branches
            var dataBranches = new StringBuilder();
            foreach (var typeProperty in typeProperties) {
                var roleName = typeProperty.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel);
                var roleIdx = distinctRoleNames.FindIndex(n =>
                    string.Equals(n, roleName, StringComparison.Ordinal));
                if (roleIdx < 0)
                    continue;

                // Target expression for the getter:
                // With guard: "it->" (checked for validity)
                // Without guard: "{itemAccessor}(index.row())->" (direct access)
                var getterTarget = needsGuard ? "it->" : $"{itemAccessor}(index.row())->";
                var getterName = distinctRoleNames[roleIdx];

                dataBranches.AppendLine($@"
    if (role == Qt::UserRole + {roleIdx + 1})
        return QVariant::fromValue({getterTarget}{getterName}());");
            }

            return (propertyGuard, dataBranches.ToString());
        }
    }
}
