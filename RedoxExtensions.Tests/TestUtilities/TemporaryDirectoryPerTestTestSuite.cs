//
// Copyright (C) 2014 ANSYS, Inc. and its subsidiaries.  All Rights Reserved.
//
// $LastChangedDate: 2014-10-26 18:37:59 -0400 (Sun, 26 Oct 2014) $
// $LastChangedRevision: 775121 $
// $LastChangedBy: rlin $
//


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;

namespace RedoxExtensions.Tests.TestUtilities
{
    public abstract class TemporaryDirectoryPerTestTestSuite
    {
        private string _testTemporaryDirectory;

        protected string TestTemporaryDirectory
        {
            get
            {
                return this._testTemporaryDirectory;
            }
        }

        [SetUp]
        public virtual void TestSetup()
        {
            this._testTemporaryDirectory = Path.Combine(Path.GetTempPath(), "RedoxExtensions-" + Path.GetRandomFileName());
            Directory.CreateDirectory(this._testTemporaryDirectory);
        }

        [TearDown]
        public virtual void TestTeardown()
        {
            if (Directory.Exists(this._testTemporaryDirectory))
            {
                Directory.Delete(this._testTemporaryDirectory, true);
            }

            this._testTemporaryDirectory = string.Empty;
        }

        protected string CreateFile(string fileName)
        {
            var fullFileName = Path.Combine(this._testTemporaryDirectory, fileName);
            File.Create(fullFileName).Close();
            return fullFileName;
        }
    }
}
