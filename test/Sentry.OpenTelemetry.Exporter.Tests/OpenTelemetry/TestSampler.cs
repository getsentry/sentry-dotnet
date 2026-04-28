// <copyright file="TestSampler.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry.Tests.OpenTelemetry
{
    // This is a copy of OpenTelemetry.Tests.TestSampler:
    // https://github.com/open-telemetry/opentelemetry-dotnet/blob/b23b1460e96efb5ecd78d1b36c2e00e84de7086b/test/OpenTelemetry.Tests/Shared/TestSampler.cs
    internal class TestSampler : Sampler
    {
        public Func<SamplingParameters, SamplingResult> SamplingAction { get; set; }

        public SamplingParameters LatestSamplingParameters { get; private set; }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            LatestSamplingParameters = samplingParameters;
            return SamplingAction?.Invoke(samplingParameters) ?? new SamplingResult(SamplingDecision.RecordAndSample);
        }
    }
}
