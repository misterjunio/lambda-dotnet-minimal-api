# Welcome to a .NET 6 Minimal API on a containerised Lambda 👋

## First step, build a Lambda container image that is able to run a Minimal API ✅

- Grabbed the `https://github.com/davidfowl/CommunityStandUpMinimalAPI` repo
- Obviously it's a terrible idea to have the in-memory DB with EF on a Lambda, only done for demonstration purposes
- `lambda-test-event.json` as a sample event for testing the Lambda is responding
- Lots of optimisations can be applied to the Dockerfile alone, tests are very *minimal* (ha), etc.

## Second step, move to IaC ✅

- Using CDK environment variables, should be all good if the CDK CLI is setup corretly
- Hardcoded function and ECR repo names for now
- Lambda function URL with no auth whatsoever as an output
- Postman collection provided to test the API, just replace the `lambdaUrl` collection variable with your URL

## Third step, wire up CI/CD ⏳

- Set up versioning stuff ✅
- Build the Docker image ✅
- Create ECR repo if it doesn't exist ✅ - using long term credentials for simplicity, encourage to visit [this link](https://github.com/aws-actions/configure-aws-credentials) and create short-lived credentials with an IAM role
- Push new image to the repo ✅
- Run CDK deployment ⏳
- Short validation test ⏳
