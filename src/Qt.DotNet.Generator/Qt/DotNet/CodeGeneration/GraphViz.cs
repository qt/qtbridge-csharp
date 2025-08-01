/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
#if DEBUG

using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Rubjerg.Graphviz;

namespace Qt.DotNet.CodeGeneration
{
    public static class GraphViz
    {
        public static RootGraph FromDependencyGraph(DependencyGraph graph)
        {
            var graphViz = RootGraph.CreateNew(GraphType.Directed, "Assembly Dependency Graph");
            graphViz.SetAttribute("fontname", "Consolas");

            foreach (var node in graph) {
                var type = node.Key;
                var gvNode = graphViz.GetOrAddNode(type.GetHashCode().ToString());

                var baseTypes = type.GetInterfaces()
                    .OrderBy(x => x.Name)
                    .Prepend(type.BaseType)
                    .Where(x => graph.Keys.Contains(x))
                    .Select(x => $"BASE {TypeLabel(x)}");
                var ctors = node.Value
                    .OfType<ConstructorInfo>()
                    .OrderBy(x => x.GetParameters().Length)
                    .Select(x => $"CTOR ({string
                        .Join(',', x.GetParameters().Select(y => TypeLabel(y.ParameterType)))})");
                var methods = node.Value
                    .OfType<MethodInfo>()
                    .OrderBy(x => x.IsStatic).ThenBy(x => x.Name)
                    .Select(x => $@"
{(x.IsStatic ? "STATIC " : "")}METHOD {TypeLabel(x.ReturnType)} {x.Name}({string
    .Join(',', x.GetParameters().Select(y => TypeLabel(y.ParameterType)))})");
                var props = node.Value
                    .OfType<PropertyInfo>()
                    .OrderBy(x => (x.GetAccessors().FirstOrDefault()?.IsStatic == true))
                    .ThenBy(x => x.Name)
                    .Select(x => $@"
{(x.GetAccessors().FirstOrDefault()?.IsStatic == true ? "STATIC " : "")}
PROPERTY {TypeLabel(x.PropertyType)} {x.Name}");
                var fields = node.Value
                    .OfType<FieldInfo>()
                    .Where(x => !x.IsLiteral)
                    .OrderBy(x => x.IsStatic).ThenBy(x => x.Name)
                    .Select(x => $@"
{(x.IsStatic ? "STATIC " : "")}FIELD {TypeLabel(x.FieldType)} {x.Name}");
                var consts = node.Value
                    .OfType<FieldInfo>()
                    .Where(x => x.IsLiteral)
                    .OrderBy(x => x.Name)
                    .Select(x => $"CONST {TypeLabel(x.FieldType)} {x.Name}");
                var events = node.Value
                    .OfType<EventInfo>()
                    .OrderBy(x => (x.AddMethod.IsStatic)).ThenBy(x => x.Name)
                    .Select(x => $@"
{(x.AddMethod.IsStatic ? "STATIC " : "")}EVENT {TypeLabel(x.EventHandlerType)} {x.Name}");
                var nestedTypes = node.Value
                    .OfType<TypeInfo>()
                    .OrderBy(x => x.Name)
                    .Select(x => $"TYPE {TypeLabel(x)}");
                var members = baseTypes
                    .Union(ctors)
                    .Union(methods)
                    .Union(props)
                    .Union(fields)
                    .Union(consts)
                    .Union(events)
                    .Union(nestedTypes);

                gvNode.SetAttribute("shape", "plaintext");
                var membersHtml = new StringBuilder();
                foreach (var member in members) {
                    membersHtml.Append($@"
  <TR>
    <TD ALIGN=""LEFT"">{member}</TD>
  </TR>");
                }
                gvNode.SetAttributeHtml("label", @$"
<FONT FACE=""Consolas"" POINT-SIZE=""{(type.IsRootNode() ? 24 : 14)}"">
  <TABLE BORDER=""{(type.IsRootNode() ? 2 : 1)}"" CELLBORDER=""0"" CELLSPACING=""0"">
    <TR>
      <TD ALIGN=""LEFT"" BGCOLOR=""lightgray""
        ><SUP>{type.Namespace ?? "<B>ROOT</B>"}</SUP></TD>
    </TR>
    <TR>
      <TD BORDER=""{(members.Any() ? 1 : 0)}"" SIDES=""B""><B>{TypeLabel(type)}</B></TD>
    </TR>
{membersHtml}
  </TABLE>
</FONT>");
            }

            foreach (var nodeFrom in graph.Keys) {
                foreach (var nodeTo in graph.Connected(nodeFrom)) {
                    var edge = graphViz.GetOrAddEdge(
                        graphViz.GetNode(nodeFrom.GetHashCode().ToString()),
                        graphViz.GetNode(nodeTo.GetHashCode().ToString()));
                    edge.SetAttribute("color", "blue");
                }
            }

            return graphViz;
        }

        private static string TypeLabel(Type type)
        {
            if (type.IsRootNode())
                return type.Module.Name;
            var typeName = type.Name.Replace("<", "&lt;").Replace(">", "&gt;");
            if (type.DeclaringType != null)
                typeName = TypeLabel(type.DeclaringType) + "+" + typeName;

            return
                type switch
                {
                    { IsConstructedGenericType: true } => typeName.Split('`')[0]
                        + "&lt;"
                        + string.Join(',', type.GenericTypeArguments.Select(t => TypeLabel(t)))
                        + "&gt;",
                    _ => typeName.Split('`', StringSplitOptions.RemoveEmptyEntries)[0]
                };
        }
    }
}
#endif
