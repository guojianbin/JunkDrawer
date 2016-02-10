#region license
// JunkDrawer
// Copyright 2013 Dale Newman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
using System.Linq;
using Autofac;
using Pipeline;
using Pipeline.Configuration;
using Pipeline.Contracts;

namespace JunkDrawer.Autofac {
    public class PipelineAction : IAction {
        private readonly ContainerBuilder _builder;
        private readonly Root _root;

        public PipelineAction(ContainerBuilder builder, Root root) {
            _builder = builder;
            _root = root;
        }

        public ActionResponse Execute() {
            var response = new ActionResponse();
            using (var scope = _builder.Build().BeginLifetimeScope()) {
                // resolve, (use), and release
                var container = scope;
                foreach (var controller in _root.Processes.Where(p => p.Enabled).Select(p => container.ResolveNamed<IProcessController>(p.Key))) {
                    controller.PreExecute();
                    controller.Execute();
                    controller.PostExecute();
                }
            }
            return response;
        }
    }
}