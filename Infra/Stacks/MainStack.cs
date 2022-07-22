using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace Infra.Stacks
{
    public class MainStack : Stack
    {
        internal MainStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var lambda = new DockerImageFunction(this, "TodoApiLambda", new DockerImageFunctionProps
            {
                FunctionName = "junio-test-minimal-api",
                Code = DockerImageCode.FromEcr(Repository.FromRepositoryName(this, "ECRRepo", "junio-test")),
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
