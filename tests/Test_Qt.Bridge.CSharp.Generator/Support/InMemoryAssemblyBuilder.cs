// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Test_Qt.Bridge.CSharp.Generator.Support
{
    internal sealed class AssemblyConfig
    {
        internal string AssemblyName { get; init; }
        internal string TypeName { get; init; }
        internal string MethodName { get; init; }
    }

    /// <summary>
    /// Configuration for the method's return value. By overriding <c>ReturnIl</c>, you can
    /// provide IL instructions to compute and push the return value onto the evaluation stack
    /// before emitting <c>ret</c>.
    /// </summary>
    internal sealed class ReturnConfig
    {
        internal Action<ReturnTypeEncoder> EncodeType { get; init; }
        internal Action<InstructionEncoder> ReturnIl { get; init; } = il => il.OpCode(ILOpCode.Ret);
    }

    /// <summary>
    /// Configuration for the method's parameters.
    /// </summary>
    internal sealed class ParameterConfig
    {
        internal Action<ParametersEncoder> EncodeTypes { get; init; }
        internal IReadOnlyList<string> Names { get; init; } = [];
    }

    /// <summary>
    /// Dynamically builds an in-memory .NET assembly containing a single static method with a
    /// specified return type, the given parameters, and parameter names.
    /// By setting <c>includeParamRows</c> to <c>false</c>, produces a lean assembly with no
    /// Param table entries, while using the default <c>true</c> adds Param rows for the return
    /// value and each argument, making names and attributes visible in metadata.
    /// </summary>
    internal static class InMemoryAssemblyBuilder
    {
        internal static void Build(AssemblyConfig assemblyConfig, ReturnConfig returnConfig,
            ParameterConfig parameterConfig, Stream outputStream, bool includeParamRows = true)
        {
            ArgumentNullException.ThrowIfNull(assemblyConfig);
            ArgumentException.ThrowIfNullOrEmpty(assemblyConfig.AssemblyName);
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyConfig.TypeName);
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyConfig.MethodName);

            ArgumentNullException.ThrowIfNull(returnConfig);
            ArgumentNullException.ThrowIfNull(returnConfig.EncodeType);
            ArgumentNullException.ThrowIfNull(parameterConfig);

            var paramCount = parameterConfig.Names.Count;
            if (paramCount > 0)
                ArgumentNullException.ThrowIfNull(parameterConfig.EncodeTypes);

            // Initialize metadata builder
            var metadataBuilder = new MetadataBuilder();

            // Add module
            _ = metadataBuilder.AddModule(
                generation: 0,
                moduleName: metadataBuilder.GetOrAddString(assemblyConfig.AssemblyName + ".dll"),
                mvid: metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
                encId: metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
                encBaseId: metadataBuilder.GetOrAddGuid(Guid.NewGuid()));

            // Add assembly
            _ = metadataBuilder.AddAssembly(
                name: metadataBuilder.GetOrAddString(assemblyConfig.AssemblyName),
                version: new Version(1, 0, 0, 0),
                culture: default,
                publicKey: default,
                flags: 0,
                hashAlgorithm: AssemblyHashAlgorithm.None);

            var sysRuntimeName = Assembly.Load("System.Runtime").GetName();
            ArgumentException.ThrowIfNullOrEmpty(sysRuntimeName.Name);
            ArgumentNullException.ThrowIfNull(sysRuntimeName.Version);

            // Public key token must be present to unify identities
            var pkt = sysRuntimeName.GetPublicKeyToken();
            BlobHandle publicKeyOrToken = default;
            if (pkt is { Length: > 0 })
                publicKeyOrToken = metadataBuilder.GetOrAddBlob(pkt);

            // Optional: culture if available
            StringHandle culture = default;
            if (!string.IsNullOrEmpty(sysRuntimeName.CultureName))
                culture = metadataBuilder.GetOrAddString(sysRuntimeName.CultureName);

            // Add the AssemblyRef for System.Runtime
            var systemRuntimeRef = metadataBuilder.AddAssemblyReference(
                name: metadataBuilder.GetOrAddString(sysRuntimeName.Name),
                version: sysRuntimeName.Version,
                culture: culture,
                publicKeyOrToken: publicKeyOrToken,
                flags: 0,
                hashValue: default);

            // Build method signature using the provided configurations
            var signatureBuilder = new BlobBuilder();
            var signatureEncoder = new BlobEncoder(signatureBuilder);
            signatureEncoder.MethodSignature(isInstanceMethod: false)
                .Parameters(paramCount, returnConfig.EncodeType, p =>
                {
                    if (paramCount > 0)
                        parameterConfig.EncodeTypes(p);
                });

            // IL body
            var instructionEncoder = new InstructionEncoder(new BlobBuilder());
            returnConfig.ReturnIl(instructionEncoder); // default: just 'ret'

            var ilBuilder = new BlobBuilder();
            var methodBodyStreamEncoder = new MethodBodyStreamEncoder(ilBuilder);
            var methodBodyHandle = methodBodyStreamEncoder.AddMethodBody(instructionEncoder);

            // Optional: Add parameter rows (must be added before the method)
            ParameterHandle firstParameterHandle;
            switch (includeParamRows) {
            case true when paramCount > 0:
                // Sequence 0 = return parameter
                firstParameterHandle = metadataBuilder.AddParameter(
                    attributes: ParameterAttributes.None,
                    name: default,
                    sequenceNumber: 0);

                // Add parameters for the method
                var paramSequence = 1;
                foreach (var paramName in parameterConfig.Names) {
                    var nameHandle = string.IsNullOrEmpty(paramName)
                        ? default
                        : metadataBuilder.GetOrAddString(paramName);
                    _ = metadataBuilder.AddParameter(
                        attributes: ParameterAttributes.None,
                        name: nameHandle,
                        sequenceNumber: paramSequence++);
                }

                break;
            case true when paramCount == 0:
                firstParameterHandle = default; // omit Param table entries entirely
                break;
            default:
                var nextParamRow = metadataBuilder.GetRowCount(TableIndex.Param) + 1;
                firstParameterHandle = paramCount == 0
                    ? default
                    : MetadataTokens.ParameterHandle(nextParamRow);
                break;
            }

            // Order here is important

            // Compute starting row indices before emitting TypeDefs
            var nextFieldRow = metadataBuilder.GetRowCount(TableIndex.Field) + 1;
            var nextMethodRow = metadataBuilder.GetRowCount(TableIndex.MethodDef) + 1;

            // <Module> type (no base type, no fields, no methods)
            _ = metadataBuilder.AddTypeDefinition(
                attributes: 0,
                @namespace: default,
                name: metadataBuilder.GetOrAddString("<Module>"),
                baseType: default,
                fieldList: MetadataTokens.FieldDefinitionHandle(nextFieldRow),
                methodList: MetadataTokens.MethodDefinitionHandle(nextMethodRow));

            // Split "Namespace.Type" into namespace + simple name
            var (ns, simpleName) = SplitNamespaceAndName(assemblyConfig.TypeName);

            var systemObjectTypeRef = metadataBuilder.AddTypeReference(systemRuntimeRef,
                metadataBuilder.GetOrAddString("System"), metadataBuilder.GetOrAddString("Object"));

            // User type, it will own methods starting at nextMethodRow
            _ = metadataBuilder.AddTypeDefinition(
                attributes: TypeAttributes.Class | TypeAttributes.Public
                | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                @namespace: string.IsNullOrEmpty(ns) ? default : metadataBuilder.GetOrAddString(ns),
                name: metadataBuilder.GetOrAddString(simpleName),
                baseType: systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(nextFieldRow),
                methodList: MetadataTokens.MethodDefinitionHandle(nextMethodRow));

            // Emit the MethodDef(s) that belong to the user type
            _ = metadataBuilder.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                implAttributes: MethodImplAttributes.IL,
                name: metadataBuilder.GetOrAddString(assemblyConfig.MethodName),
                signature: metadataBuilder.GetOrAddBlob(signatureBuilder),
                bodyOffset: methodBodyHandle,
                parameterList: firstParameterHandle);

            // Build PE
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.Dll);
            var metadataRootBuilder = new MetadataRootBuilder(metadataBuilder);
            var managedPeBuilder = new ManagedPEBuilder(
                header: peHeaderBuilder,
                metadataRootBuilder: metadataRootBuilder,
                ilStream: ilBuilder,
                strongNameSignatureSize: 0,
                flags: CorFlags.ILOnly);

            // Write into the specified stream
            var peBlobBuilder = new BlobBuilder();
            managedPeBuilder.Serialize(peBlobBuilder);
            peBlobBuilder.WriteContentTo(outputStream);
        }

        private static (string Namespace, string Name) SplitNamespaceAndName(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
                return ("", "");

            // Split only on the last dot so we keep nested markers (e.g., "Outer+Inner")
            var lastDot = fullTypeName.LastIndexOf('.');
            if (lastDot < 0)
                return ("", fullTypeName);

            var ns = fullTypeName[..lastDot];
            var name = fullTypeName[(lastDot + 1)..]; // may contain '+' for nested types
            return (ns, name);
        }
    }
}
