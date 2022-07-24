using System;
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace Infra.Stacks
{
    public class MinimalApiContainerLambdaStack : Stack
    {
        internal MinimalApiContainerLambdaStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var lambdaFunctionName = (string)this.Node.TryGetContext("lambdaFunctionName");
            if (string.IsNullOrWhiteSpace(lambdaFunctionName))
            {
                throw new Exception($"{nameof(lambdaFunctionName)} context missing");
            }

            var ecrRepoName = (string)this.Node.TryGetContext("ecrRepoName");
            if (string.IsNullOrWhiteSpace(ecrRepoName))
            {
                throw new Exception($"{nameof(ecrRepoName)} context missing");
            }

            var lambdaFunctionImageTag = (string)this.Node.TryGetContext("lambdaFunctionImageTag");
            if (string.IsNullOrWhiteSpace(lambdaFunctionImageTag))
            {
                throw new Exception($"{nameof(lambdaFunctionImageTag)} context missing");
            }

            var lambda = new DockerImageFunction(this, "TodoApiLambda", new DockerImageFunctionProps
            {
                FunctionName = lambdaFunctionName,
                Code = DockerImageCode.FromEcr(Repository.FromRepositoryName(this, "ECRRepo", ecrRepoName), new EcrImageCodeProps
                {
                    TagOrDigest = lambdaFunctionImageTag,
                }),
                Timeout = Duration.Seconds(10),
            });

            var lambdaUrl = lambda.AddFunctionUrl(new FunctionUrlOptions
            {
                AuthType = FunctionUrlAuthType.NONE,
            });

            _ = new CfnOutput(this, "LambdaFunctionUrl", new CfnOutputProps
            {
                Description = "Reach your Minimal API on this URL!",
                Value = lambdaUrl.Url,
            });
        }
    }
}
