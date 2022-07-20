# First step, building a Lambda container that is able to run a Minimal API ✅

- Grabbed the `https://github.com/davidfowl/CommunityStandUpMinimalAPI` repo
- Obviously it's a terrible idea to have the in-memory DB with EF on a Lambda, only done for demonstration purposes
- `lambda-test-event.json` as a sample event for testing the Lambda is responding
- Lots of optimisations can be applied to the Dockerfile alone, tests are very *minimal*, etc.
