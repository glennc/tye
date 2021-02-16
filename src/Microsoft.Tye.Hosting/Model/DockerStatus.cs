// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.Tye.Hosting.Model
{
    public class DockerStatus : ReplicaStatus
    {
        public DockerStatus(Service service, string name) : base(service, name)
        {
        }

        public string? DockerCommand { get; set; }

        public string? DockerNetwork { get; set; }

        public string? DockerNetworkAlias { get; set; }

        public string? ContainerId { get; set; }

        public async Task Restart()
        {
            var result = await ProcessUtil.RunAsync(
                "docker",
                $"restart {ContainerId}",
                throwOnError: false,
                outputDataReceived: data => Service.Logs.OnNext($"[{ContainerId}]: {data}"),
                errorDataReceived: data => Service.Logs.OnNext($"[{ContainerId}]: {data}"));
            
            if(result.ExitCode == 0)
            {
                Service.Restarts += 1;
            }
        }
    }
}
