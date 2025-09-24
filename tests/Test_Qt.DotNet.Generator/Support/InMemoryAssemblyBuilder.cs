/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Test_Qt.DotNet.Generator.Support
{
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
        internal static byte[] Build(string assemblyName, string moduleName, string typeName,
            string methodName, ReturnConfig returnConfig, ParameterConfig parameterConfig,
            bool includeParamRows = true)
        {
            ArgumentException.ThrowIfNullOrEmpty(assemblyName);
            ArgumentException.ThrowIfNullOrEmpty(moduleName);
            ArgumentException.ThrowIfNullOrEmpty(typeName);
            ArgumentException.ThrowIfNullOrEmpty(methodName);

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
                moduleName: metadataBuilder.GetOrAddString(moduleName),
                mvid: metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
                encId: metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
                encBaseId: metadataBuilder.GetOrAddGuid(Guid.NewGuid()));

            // Add assembly
            _ = metadataBuilder.AddAssembly(
                name: metadataBuilder.GetOrAddString(assemblyName),
                version: new Version(1, 0, 0, 0),
                culture: default,
                publicKey: default,
                flags: 0,
                hashAlgorithm: AssemblyHashAlgorithm.None);

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

            // Optional: Add parameter rows (must be added BEFORE the method)
            var firstParameterHandle = default(ParameterHandle);
            if (includeParamRows) {
                // Sequence 0 = return parameter
                metadataBuilder.AddParameter(
                    attributes: ParameterAttributes.None,
                    name: default,
                    sequenceNumber: 0);

                // Add parameters for the method
                var paramSequence = 1;
                foreach (var paramName in parameterConfig.Names) {
                    var nameHandle = string.IsNullOrEmpty(paramName)
                        ? default
                        : metadataBuilder.GetOrAddString(paramName);
                    var paramHandle = metadataBuilder.AddParameter(
                        attributes: ParameterAttributes.None,
                        name: nameHandle,
                        sequenceNumber: paramSequence++);
                    if (firstParameterHandle.IsNil)
                        firstParameterHandle = paramHandle;
                }
            }

            // Compute first MethodDef row for TypeDef.MethodList
            var firstMethodRow = MetadataTokens.MethodDefinitionHandle(
                metadataBuilder.GetRowCount(TableIndex.MethodDef) + 1);

            // Add TypeDef with provided type name
            _ = metadataBuilder.AddTypeDefinition(
                attributes: TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
                @namespace: default,
                name: metadataBuilder.GetOrAddString(typeName),
                baseType: default,
                fieldList: default,
                methodList: firstMethodRow);

            // Add MethodDef with provided method name
            _ = metadataBuilder.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                implAttributes: MethodImplAttributes.IL,
                name: metadataBuilder.GetOrAddString(methodName),
                signature: metadataBuilder.GetOrAddBlob(signatureBuilder),
                bodyOffset: methodBodyHandle,
                parameterList: firstParameterHandle);

            // Build PE
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll);
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
            return peBlobBuilder.ToArray();
        }
    }
}
