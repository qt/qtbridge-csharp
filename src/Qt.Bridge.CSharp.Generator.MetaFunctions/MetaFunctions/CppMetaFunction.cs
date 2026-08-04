// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    public abstract class CppMetaFunction : MetaFunction
    {
        private static HashSet<string> Keywords { get; } = new([
            "alignas", "alignof", "and", "and_eq", "asm", "atomic_cancel", "atomic_commit",
            "atomic_noexcept", "auto", "bitand", "bitor", "bool", "break", "case", "catch", "char",
            "char8_t", "char16_t", "char32_t", "class", "compl", "concept", "const", "consteval",
            "constexpr", "constinit", "const_cast", "continue", "contract_assert", "co_await",
            "co_return", "co_yield", "decltype", "default", "delete", "do", "double",
            "dynamic_cast", "else", "emit", "enum", "explicit", "export", "extern", "false",
            "final", "float", "for", "friend", "goto", "if", "import", "inline", "int", "long",
            "module", "mutable", "namespace", "new", "noexcept", "not", "not_eq", "nullptr",
            "operator", "or", "or_eq", "private", "protected", "public", "reflexpr", "register",
            "reinterpret_cast", "requires", "return", "short", "signals", "signed", "slots",
            "sizeof", "static", "static_assert", "static_cast", "struct", "switch", "synchronized",
            "template", "this", "thread_local", "throw", "true", "try", "typedef", "typeid",
            "typename", "union", "unsigned", "using", "virtual", "void", "volatile", "wchar_t",
            "while", "xor", "xor_eq", "__alignof", "_asm", "__asm", "__assume", "__based",
            "__cdecl", "__declspec", "__event", "__except", "__fastcall", "__finally",
            "__forceinline", "__hook", "__if_exists", "__if_not_exists", "__inline", "__int16",
            "__int32", "__int64", "__int8", "__interface", "__leave", "__m128", "__m128d",
            "__m128i", "__m64", "__multiple_inheritance", "__ptr32", "__ptr64", "__raise",
            "__restrict", "__single_inheritance", "__sptr", "__stdcall", "__super", "__thiscall",
            "__unaligned", "__unhook", "__uptr", "__uuidof", "__vectorcall",
            "__virtual_inheritance", "__w64", "__wchar_t",
            ]);
        protected override string Sanitize(string evalResult)
        {
            evalResult = base.Sanitize(evalResult);
            if (evalResult.StartsWith('_'))
                evalResult = "z" + evalResult;
            if (Keywords.Contains(evalResult))
                evalResult += "_";
            return evalResult;
        }
    }
}
