# Welcome to a .NET 6 Minimal API on a containerised Lambda 👋

The project consists of 3 main interlinked components:

1. A .NET 6 Minimal API project, which lives in the `Api/` folder. The folder contains a `Dockerfile` which bakes an image that is able to run the API in a Docker-based Lambda function.
1. An AWS CDK project, which lives in the `Infra/` folder. The project hosts the Lambda function's infrastructure as code.
1. A GitHub workflow, which lives in the `.github/workflows/` folder. The workflow defines a pipeline for building and deploying the Minimal API into its infrastructure.

## Where did the inspiration come from?

A while ago I wrote [a blog post](https://awstip.com/containerised-lambda-a-nice-use-case-a8e5133699ac) about a nice use case for containerised Lambda. In it I go over the design of a solution we implemented for a [Concentrix Catalyst](https://catalyst.concentrix.com/) client using a Bitbucket repo, Jenkins pipelines and Terraform for IaC.

This repository was born out of the spirit of open-sourcing an implementation example of that solution design, with perhaps more readily-accessible technologies. The design is essentially the same, only with different providers. Have a quick look at that post if you're interested in the background of the idea.

## How do I use this? The short answer

Go ahead and fork the repo.

Then start by have a look inside the `.github/workflows/pipeline.yml` file. Find the `env` section near the top. For simplicity, long-lived AWS credentials - `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` - are set up as [GitHub secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets) and used as global environment variables. You have to set your own. Also, change the `AWS_REGION` variable to the region code you want to deploy your resources to.

Still in the same section of the pipeline file, modify the `ECR_REPO_NAME` variable to your liking. Per its name, it defines the name of a private ECR repository which will be created in your AWS account. You can also leave the default.

Next, find the `Infra/cdk.json` file. For `lambdaFunctionName`, replace `containerised-dotnet-minimal-api` with the name you want to give your Lambda function. Or just leave it as is.

If you made any changes, commit and push them to the `main` branch and that's pretty much it! Head over to the **Actions** tab of your repository and you should see the **Pipeline** running. In case you didn't make changes, you can run the workflow manually instead. If all goes well, by the end you'll have the `TodoApi` running in a Dockerised Lambda function in your account. 🎉

Have a look at the logs for the *Test Lambda* job -> *cURL Lambda URL* step to find the URL where you can reach your Lambda function. As a convenience, a Postman collection is provided in `Api/postman_collection.json` with all the operations you can do against it. Merely import it into Postman and replace the `lambdaUrl` collection variable with the URL of your function.

If anything goes wrong and you're stuck, feel free to open an issue. Keep reading for details about each component.

## The deets

Here I detail the main steps I undertook to put all the concepts together.

### First step, build a Lambda container image that is able to run a Minimal API ✅

#### The API itself

Well first things first: to run a Minimal API in a Lambda function, we need a Minimal API. I took advantage of [this .NET guide](https://docs.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-6.0&tabs=visual-studio) and [this repository](https://github.com/davidfowl/CommunityStandUpMinimalAPI) and mashed them together to create a simple RESTful Minimal API that allows CRUD operations on todo items.

Disclaimer: it's obviously not a brilliant idea to have an in-memory database with Entity Framework running on a Lambda, for reasons which include the ephemeral nature of Lambda functions and the slow start-up time of in-memory EF. This is only done for demonstration purposes (it's more fun to have actual data storage than static responses) and because I was too lazy to take it further.

Keep in mind that the tests included (in the `TodoApi.Tests` project) are also very *minimal* (ha), but they show how easy integration testing comes with Minimal APIs.

#### The Lambda-isation

There are quite a few gotchas I needed to work through for getting the Minimal API container to run on Lambda. I won't do a deep dive into them, I'll just call out your attention to these properties in the `Api/TodoApi/TodoApi.csproj` file:

``` xml
<GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
<AWSProjectType>Lambda</AWSProjectType>
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
<PublishReadyToRun>true</PublishReadyToRun>
<RuntimeIdentifier>linux-x64</RuntimeIdentifier>
<SelfContained>False</SelfContained>
```

These are all in one way or another related to enabling/improving container-based .NET on Lambda.

In terms of the code, the only line that is adjusted for a Lambda environment is

``` csharp
.AddAWSLambdaHosting(LambdaEventSource.HttpApi)
```

when building the web app service collection on `Api/TodoApi/Program.cs`. You can adjust it to match whatever is fronting your Lambda, which in our case is nothing but a function URL, so the `HttpApi` option works.

#### The Docker-isation

The Dockerfile itself is pretty standard for a .NET application. The most important points are at the end:

``` text
WORKDIR /var/task

# .NET 6 Minimal API is an executable assembly
ENTRYPOINT ["dotnet", "TodoApi.dll"]
```

The working directory is specific to Lambda. And the entrypoint is slightly simpler than you would see for a traditional Lambda function.

#### Is it working?

As I tweaked the project, I was able to validate it by manually building and pushing the Docker image to a private ECR repo on my AWS account, then manually creating a Lambda function via the Console to use the latest image as its source.

After doing any updates, I would run a test from within the Lambda Console, using the `Api/lambda-test-event.json` as the test event JSON. Rinse and repeat until I arrived at the current working version.

### Second step, move to IaC ✅

Once I saw everything working as expected manually, it was time to make it repeatable and enable automation by porting over to Infrastructure-as-Code. Picking the AWS CDK was an easy enough choice, as it lends itself well to this kind of PoC, maintaining the flexibility to evolve into a more complete solution.

The `Infra/Program.cs` file picks up the default AWS credentials from environment variables, but feel free to adapt it to your needs. Provided the AWS CLI or environment is set up correctly (as it is in the accompanying pipeline), things should just work.

The CDK project overall is quite simple. It manages a single `MinimalApiContainerLambdaStack` stack, which defines a Lambda function that runs an ECR-stored image, a function URL attached to the function and the URL itself as an output. Note that the URL has **no authentication**, meaning it's public. As such, you can experiment with it from anywhere, but keep in mind the dangers of having it exposed to the whole Internet. Again, I kept it that way only for demonstration.

Running the project relies on 3 bits of self-explanatory [context data](https://docs.aws.amazon.com/cdk/v2/guide/context.html): `lambdaFunctionName`, `ecrRepoName`, and `lambdaFunctionImageTag`. In this solution, I show a mix of the `context` property in the `Infra/cdk.json` file and the *--context (-c)* option of the CLI (via the deployment pipeline). As we'll see in the CI/CD section below, the sources are the `cdk.json` file for the Lambda function name, a GitHub workflow global environment variable for the ECR repo name and a variable that is computed during the pipeline run for the Lambda image tag.

By this point I was able to see the CDK in action by configuring my CLI and running commands such as `cdk diff`, `cdk synth`, and especially `cdk deploy` (with valid context data) from within the `Infra/` folder.

### Third step, wire up CI/CD ✅

I trust the `.github/workflows/pipeline.yml` file is not overly difficult to follow, so I won't go into step-by-step detail. Rather, here is a high-level summary of the features of the pipeline it defines:

- Triggered either by pushes to the `main` branch, or manually (e.g. via GitHub UI or API).
- Sets up a few global environment variables (explained in previous sections).
- First runs an **Initialise** job which calculates a version with which to tag the Docker image it will build and deploy. The version relies on the date and run number.
- Then goes on to **Build and deploy Lambda image**. There are a few sub-steps involved:
  - Authenticate with ECR.
  - Build a new version of the Docker image with the latest source.
  - Create an ECR repository with the configured name, if one doesn't exist yet.
  - Tag the local Docker image with the calculated version and the *latest* tags.
  - Push the tagged image to the ECR repository.
- After so, the **Upgrade Lambda** job runs the CDK project with the latest configuration. It also outputs the Lambda URL it creates.
- Finally, the **Test Lambda** job runs a small validation test by hitting the Lambda URL and ensuring it returns a `200 OK` status.

## What this project does *not* currently address

The solution is *not* production ready, although it certainly forms a nice basis to start from. Here are a few things that are not properly addressed:

- The pipeline uses long term AWS credentials for simplicity. I encourage you to visit [this link](https://github.com/aws-actions/configure-aws-credentials) and figure out how you can create short-lived credentials with an IAM role instead, to tighten up security.
- Similarly, the Lambda function URL has no auth whatsoever, which might expose you to unexpected traffic and costs. An enhancement might be to front the Lambda with an API Gateway and take advantage of its richer features for things like authorisarion/authentication.
- The Lambda execution role is left as default, which creates a basic role that can write logs to CloudWatch and that's all. In reality you'd probably want to create the role in the CDK with the permissions it needs to fulfil its responsibilities (perhaps talk to RDS for instance).
- A single environment is currently considered. To support multiple ones a branching strategy for, among other things, deciding when to deploy or only build but not deploy, would need to be devised, and the pipelines updated accordingly.
- On the API side, there are probably quite a few Docker optimisations that could be applied to speed up builds and so on. Not to mention the usage of in-memory EF, of course.

## Cleaning up

To remove all the AWS resources generated just hop on the `Infra/` folder and run `cdk destroy`. The only resource that is not created by the CDK stack is the ECR repo, so you'll have to delete it manually if you wish.
