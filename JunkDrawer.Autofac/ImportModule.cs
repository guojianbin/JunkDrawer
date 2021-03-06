﻿#region license
// JunkDrawer.Autofac
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
using System.IO;
using System.Linq;
using Autofac;
using Transformalize.Configuration;
using Transformalize.Context;
using Transformalize.Contracts;
using Transformalize.Desktop;
using Transformalize.Provider.Ado;
using Transformalize.Provider.Excel;
using Transformalize.Provider.File;
using Transformalize.Provider.MySql;
using Transformalize.Provider.PostgreSql;
using Transformalize.Provider.SqlServer;
using Transformalize.Provider.SQLite;


namespace JunkDrawer.Autofac {
    public class ImportModule : Module {

        private static readonly IDictionary<int, string> Cache = new Dictionary<int, string>();

        protected override void Load(ContainerBuilder builder) {

            // an input connection
            builder.Register((c, p) => c.Resolve<Cfg>(p).Input()).As<Connection>();

            // a cleaned up file info
            builder.Register((c, p) => {
                var connection = c.Resolve<Connection>();
                return new FileInfo(Path.IsPathRooted(connection.File) ? connection.File : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, connection.File));
            }).As<FileInfo>();

            // an input connection context
            builder.Register<IConnectionContext>((c, p) => new ConnectionContext(c.Resolve<IContext>(p), c.Resolve<Connection>())).As<IConnectionContext>();

            // a schema reader for file or excel
            builder.Register<ISchemaReader>((c, p) => {

                var context = c.Resolve<IConnectionContext>();

                var cfg = context.Connection.Provider == "file" ?
                    new FileInspection(context, c.Resolve<FileInfo>()).Create() :
                    new ExcelInspection(context, c.Resolve<FileInfo>()).Create();
                var process = c.Resolve<Process>(p.Concat(new[] { new NamedParameter("cfg", cfg) }));
                process.Pipeline = "parallel.linq";

                return new SchemaReader(context, new RunTimeRunner(context), process);
            }).As<ISchemaReader>();


            builder.Register<IRunTimeExecute>((c, p) => new RunTimeExecutor(c.Resolve<IConnectionContext>())).As<IRunTimeExecute>();

            // when you resolve a process, you need to add a cfg parameter
            builder.Register((c, p) => {
                var parameters = new List<global::Autofac.Core.Parameter>();
                parameters.AddRange(p);
                var cfg = new ConfigurationCreator(c.Resolve<Cfg>(p), c.Resolve<ISchemaReader>(p)).Create();
                parameters.Add(new NamedParameter("cfg", cfg));
                return c.Resolve<Process>(parameters);
            }).Named<Process>("import");

            // Connection Factory
            builder.Register<IConnectionFactory>(c => {
                var output = c.Resolve<Cfg>().Output();
                switch (output.Provider) {
                    case "sqlserver":
                        return new SqlServerConnectionFactory(output);
                    case "mysql":
                        return new MySqlConnectionFactory(output);
                    case "postgresql":
                        return new PostgreSqlConnectionFactory(output);
                    case "sqlite":
                        return new SqLiteConnectionFactory(output);
                    default:
                        return new NullConnectionFactory();
                }
            }).As<IConnectionFactory>().InstancePerLifetimeScope();


            // Final product is Importer
            builder.Register((c, p) => {
                var context = c.Resolve<IConnectionContext>();
                var key = c.Resolve<Request>(p).ToKey(c.Resolve<Cfg>());
                var process = c.Resolve<Process>();
                if (Cache.ContainsKey(key)) {
                    process.Load(Cache[key]);
                    context.Info("Using cached delimiter, header, type, and string-length inspection.");
                } else {
                    process = c.ResolveNamed<Process>("import", p);
                    Cache[key] = process.Serialize();
                    context.Info("Cached delimiter, header, type, and string-length inspection.");
                }
                return new Importer(process, c.Resolve<IRunTimeExecute>(p), c.Resolve<IConnectionFactory>());
            }).As<Importer>();

        }

    }
}
