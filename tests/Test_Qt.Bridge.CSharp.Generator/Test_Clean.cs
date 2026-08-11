// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    [TestClass]
    public class Test_Clean
    {
        private static void Assert_Exists(DirectoryInfo root, params string[] paths)
        {
            if (paths is not { Length: > 0 })
                return;
            Assert.IsTrue(paths.All(path =>
                File.Exists(Path.Combine(root.FullName, path))
                    || Directory.Exists(Path.Combine(root.FullName, path))));
        }

        private static void Assert_Removed(DirectoryInfo root, params string[] paths)
        {
            if (paths is not { Length: > 0 })
                return;
            Assert.IsFalse(paths.All(path =>
                File.Exists(Path.Combine(root.FullName, path))
                    || Directory.Exists(Path.Combine(root.FullName, path))));
        }

        private static void MakeTree(DirectoryInfo root)
        {
            // ROOT/
            //     ├── F
            //     └── a/
            //          ├── F
            //          └── b/
            //               ├── F
            //               ├── c/
            //               │    ├── d/
            //               │    │    └── F
            //               │    └── e/
            //               └── x/
            //                    ├── F
            //                    ├── y/
            //                    │    └── F
            //                    └── z/
            //                         └── F

            var f = new FileInfo(Path.Combine(root.FullName, "F"));
            File.WriteAllText(f.FullName, "foo");

            var a = root.CreateSubdirectory("a");
            var a_f = new FileInfo(Path.Combine(a.FullName, "F"));
            File.WriteAllText(a_f.FullName, "foo");

            var a_b = a.CreateSubdirectory("b");
            var a_b_f = new FileInfo(Path.Combine(a_b.FullName, "F"));
            File.WriteAllText(a_b_f.FullName, "foo");

            var a_b_c = a_b.CreateSubdirectory("c");

            var a_b_c_d = a_b_c.CreateSubdirectory("d");
            var a_b_c_d_f = new FileInfo(Path.Combine(a_b_c_d.FullName, "F"));
            File.WriteAllText(a_b_c_d_f.FullName, "foo");

            var a_b_c_e = a_b_c.CreateSubdirectory("e");

            var a_b_x = a_b.CreateSubdirectory("x");
            var a_b_x_f = new FileInfo(Path.Combine(a_b_x.FullName, "F"));
            File.WriteAllText(a_b_x_f.FullName, "foo");

            var a_b_x_y = a_b_x.CreateSubdirectory("y");
            var a_b_x_y_f = new FileInfo(Path.Combine(a_b_x_y.FullName, "F"));
            File.WriteAllText(a_b_x_y_f.FullName, "foo");

            var a_b_x_z = a_b_x.CreateSubdirectory("z");
            var a_b_x_z_f = new FileInfo(Path.Combine(a_b_x_z.FullName, "F"));
            File.WriteAllText(a_b_x_z_f.FullName, "foo");

            // Dirs
            Assert_Exists(root,
                "a", "a/b", "a/b/c", "a/b/c/d", "a/b/c/e", "a/b/x", "a/b/x/y", "a/b/x/z");

            // Files
            Assert_Exists(root,
                "F", "a/F", "a/b/F", "a/b/c/d/F", "a/b/x/F", "a/b/x/y/F", "a/b/x/z/F");
        }

        [TestMethod,

            DataRow(/* targetPath */ "a",
/*   ignored */ null,
/* generated */ null,
/* remaining */ new[] { "F", "a" },
/* cleanedUp */ new[] { "a/F", "a/b" }),

            DataRow(/* targetPath */ "a/b",
/*   ignored */ null,
/* generated */ null,
/* remaining */ new[] { "F", "a", "a/F", "a/b" },
/* cleanedUp */ new[] { "a/b/F", "a/b/c", "a/b/x" }),

            DataRow(/* targetPath */ "a/b",
/*   ignored */ null,
/* generated */ new[] { "a/b/c/d/F" },
/* remaining */ new[] { "F", "a", "a/F", "a/b", "a/b/c", "a/b/c/d", "a/b/c/d/F" },
/* cleanedUp */ new[] { "a/b/F", "a/b/c/e", "a/b/x" }),

            DataRow(/* targetPath */ "a/b",
/*   ignored */ new[] { "c/e" },
/* generated */ new[] { "a/b/c/d/F" },
/* remaining */ new[] { "F", "a", "a/F", "a/b", "a/b/c", "a/b/c/d/F", "a/b/c/e" },
/* cleanedUp */ new[] { "a/b/F", "a/b/x" }),

            DataRow(/* targetPath */ "a",
/*   ignored */ new[] { "b", "F" },
/* generated */ null,
/* remaining */ new[] { "F", "a/F", "a/b/F", "a/b/c/d/F", "a/b/c/e", "a/b/x/F", "a/b/x/y/F" },
/* cleanedUp */ null)]

        public void Clean(
            string targetPath, string[] ignored, string[] generated,
            string[] remaining, string[] cleanedUp)
        {
            var root = new DirectoryInfo(
                Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            try {

                try {
                    root.Create();
                } catch (Exception) {
                    Assert.Inconclusive();
                }

                MakeTree(root);

                Qt.Bridge.CodeGeneration.Generator.Clean(Path.Combine(root.FullName, targetPath),
                    ignored, generated
                        ?.Select(p => new FileInfo(Path.Combine(root.FullName, p)))?.ToArray());

                Assert_Exists(root, remaining);

                Assert_Removed(root, cleanedUp);

            } finally {
                if (root.Exists)
                    root.Delete(true);
            }
        }
    }
}
