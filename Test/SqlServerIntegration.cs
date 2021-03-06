﻿#region license
// Test
// Copyright 2013 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Dapper;
using JunkDrawer;
using JunkDrawer.Autofac;
using NUnit.Framework;

namespace Test {

    [TestFixture]
    public class SqlServerIntegration {

        private const string ConnectionString = "server=localhost;database=junk;trusted_connection=true;";

        [Test]
        public void Demo() {

            Response response;
            var request = new Request(@"C:\Code\JunkDrawer\Test\sample.txt");
            using (var scope = new Bootstrapper(request)) {
                response = scope.Resolve<Importer>().Import();
            }

            Assert.AreEqual("sample", response.View);
            Assert.AreEqual(4, response.Records);

        }

        [Test]
        public void CommaDelimited() {

            const string content = @"Name,WebSite,Created
Google|,http://www.google.com|,9/4/98
Apple|,http://www.apple.com|,4/1/1976
Microsoft|,http://www.microsoft.com|,4/4/1975";

            var fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<Company>();

            using (var cn = new SqlConnection(ConnectionString)) {
                companies.AddRange(cn.Query<Company>("SELECT Name,Website,Created FROM " + response.View + ";"));
            }

            Assert.AreEqual(3, companies.Count);

            var google = companies.First(c => c.Name == "Google|");
            var apple = companies.First(c => c.Name == "Apple|");

            Assert.AreEqual("http://www.google.com|", google.WebSite);
            Assert.AreEqual(Convert.ToDateTime("4/1/1976"), apple.Created);

        }


        [Test]
        public void CommaDelimitedNoNames() {

            const string content = @"Google,http://www.google.com,9/4/98
Apple,http://www.apple.com,4/1/1976
Microsoft,http://www.microsoft.com,4/4/1975";

            var fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<Abc>();

            using (var cn = new SqlConnection(ConnectionString)) {
                companies.AddRange(cn.Query<Abc>("SELECT A,B,C FROM " + response.View + ";"));
            }

            Assert.AreEqual(3, companies.Count);

            var google = companies.First(c => c.A == "Google");
            var apple = companies.First(c => c.A == "Apple");

            Assert.AreEqual("http://www.google.com", google.B);
            Assert.AreEqual(Convert.ToDateTime("4/1/1976"), apple.C);

        }


        [Test]
        public void CommaSeparatedValues() {

            const string content = @"Name,WebSite,Created
""Google"",http://www.google.com,9/4/98
Apple,http://www.apple.com,""April, 1 1976""
Microsoft,""http://www.microsoft.com"",4/4/1975
""Nike, Inc."",http://www.nike.com,1/25/1964";

            var fileName = Path.GetTempFileName().Replace(".tmp", ".csv");
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<Company>();

            using (var cn = new SqlConnection(ConnectionString)) {
                companies.AddRange(cn.Query<Company>("SELECT Name,Website,Created FROM " + response.View + ";"));
            }

            Assert.AreEqual(4, companies.Count);

            var google = companies.First(c => c.Name == "Google");
            var apple = companies.First(c => c.Name == "Apple");
            var nike = companies.First(c => c.Name == "Nike, Inc.");

            Assert.AreEqual("http://www.google.com", google.WebSite);
            Assert.AreEqual(Convert.ToDateTime("4/1/1976"), apple.Created);
            Assert.AreEqual(Convert.ToDateTime("1/25/1964"), nike.Created);
        }


        [Test]
        public void CsvWithQuotedColumnNames() {

            const string content = @"""Name"",""WebSite"",""Created""
""Google"",http://www.google.com,9/4/98
Apple,http://www.apple.com,""April, 1 1976""
Microsoft,""http://www.microsoft.com"",4/4/1975
""Nike, Inc."",http://www.nike.com,1/25/1964";

            var fileName = Path.GetTempFileName().Replace(".tmp", ".csv");
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<Company>();

            using (var cn = new SqlConnection(ConnectionString)) {
                companies.AddRange(cn.Query<Company>("SELECT Name,Website,Created FROM " + response.View + ";"));
            }

            Assert.AreEqual(4, companies.Count);

        }

        [Test]
        public void CsvWithMissingAndQuotedColumnNames() {

            const string content = @",,""Created""
""Google"",http://www.google.com,9/4/98
Apple,http://www.apple.com,""April, 1 1976""
Microsoft,""http://www.microsoft.com"",4/4/1975
""Nike, Inc."",http://www.nike.com,1/25/1964";

            var fileName = Path.GetTempFileName().Replace(".tmp", ".csv");
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<Company>();

            using (var cn = new SqlConnection(ConnectionString)) {
                companies.AddRange(cn.Query<Company>("SELECT [A],[B],Created FROM " + response.View + ";"));
            }

            Assert.AreEqual(4, companies.Count);

        }

        [Test]
        public void ExcelXls() {

            const string fileName = @"C:\Code\JunkDrawer\Test\Files\Excel.xls";

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<CompanyForOldExcel>();

            using (var cn = new SqlConnection(ConnectionString)) {
                var sql = "SELECT Name,Website,Created FROM " + response.View + ";";
                companies.AddRange(cn.Query<CompanyForOldExcel>(sql));
            }

            Assert.AreEqual(4, companies.Count);

            var google = companies.First(c => c.Name == "Google");
            var apple = companies.First(c => c.Name == "Apple");
            var nike = companies.First(c => c.Name == "Nike, Inc.");

            Assert.AreEqual("http://www.google.com", google.WebSite);
            Assert.AreEqual(27851, apple.Created);
            Assert.AreEqual(23401, nike.Created);
        }

        [Test]
        public void ExcelXlsx() {

            const string fileName = @"C:\Code\JunkDrawer\Test\Files\Excel.xlsx";

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<Company>();

            using (var cn = new SqlConnection(ConnectionString)) {
                var sql = "SELECT Name,Website,Created FROM " + response.View + ";";
                companies.AddRange(cn.Query<Company>(sql));
            }

            Assert.AreEqual(4, companies.Count);

            var google = companies.First(c => c.Name == "Google");
            var apple = companies.First(c => c.Name == "Apple");
            var nike = companies.First(c => c.Name == "Nike, Inc.");

            Assert.AreEqual("http://www.google.com", google.WebSite);
            Assert.AreEqual(Convert.ToDateTime("4/1/1976"), apple.Created);
            Assert.AreEqual(Convert.ToDateTime("1/25/1964"), nike.Created);
        }

        [Test]
        public void PipeDelimited() {

            const string content = @"Name|WebSite|Created
Google|http://www.google.com|9/4/98
Apple|http://www.apple.com|4/1/1976
Microsoft|http://www.microsoft.com|4/4/1975";

            var fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                var importer = scope.Resolve<Importer>();
                response = importer.Import();
            }

            var companies = new List<Company>();

            using (var cn = new SqlConnection(ConnectionString)) {
                companies.AddRange(cn.Query<Company>("SELECT Name,Website,Created FROM " + response.View + ";"));
            }

            Assert.AreEqual(3, companies.Count);

            var google = companies.First(c => c.Name == "Google");
            var apple = companies.First(c => c.Name == "Apple");
            var ms = companies.First(c => c.Name == "Microsoft");

            Assert.AreEqual("http://www.google.com", google.WebSite);
            Assert.AreEqual(Convert.ToDateTime("4/1/1976"), apple.Created);
            Assert.AreEqual(Convert.ToDateTime("4/4/1975"), ms.Created);
        }

        [Test]
        public void TabDelimited() {

            const string content = @"Name	WebSite	Created
Google	http://www.google.com	9/4/98
Apple	http://www.apple.com	4/1/1976
Microsoft	http://www.microsoft.com	4/4/1975";

            var fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                response = scope.Resolve<Importer>().Import();
            }

            var companies = new List<Company>();

            using (var cn = new SqlConnection(ConnectionString)) {
                companies.AddRange(cn.Query<Company>("SELECT Name,Website,Created FROM " + response.View + ";"));
            }

            Assert.AreEqual(3, companies.Count);

            var google = companies.First(c => c.Name == "Google");
            var apple = companies.First(c => c.Name == "Apple");
            var ms = companies.First(c => c.Name == "Microsoft");

            Assert.AreEqual("http://www.google.com", google.WebSite);
            Assert.AreEqual(Convert.ToDateTime("4/1/1976"), apple.Created);
            Assert.AreEqual(Convert.ToDateTime("4/4/1975"), ms.Created);
        }

        [Test]
        public void FixedWidth() {

            const string content = @"Name     WebSite                 Created
Google   http://www.google.com   9/4/98
Apple    http://www.apple.com    4/1/1976
Microsofthttp://www.microsoft.com4/4/1975";

            var fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, content);

            Response response;
            var request = new Request(fileName);
            using (var scope = new Bootstrapper(request)) {
                response = scope.Resolve<Importer>().Import();
            }

            var lines = new List<string>();

            using (var cn = new SqlConnection(ConnectionString)) {
                lines.AddRange(cn.Query<string>("SELECT [Name     WebSite                 Created] FROM " + response.View + ";"));
            }

            Assert.AreEqual(3, lines.Count);

            Assert.NotNull(lines.FirstOrDefault(l => l.StartsWith("Google")));
            Assert.NotNull(lines.FirstOrDefault(l => l.StartsWith("Apple")));
            Assert.NotNull(lines.FirstOrDefault(l => l.StartsWith("Microsoft")));
        }

    }
}
