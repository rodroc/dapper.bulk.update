using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.Lambda.Core;

using MyProfileOnly;
using MyProfileOnly.Models;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyProfileOnly
{
    public class Function
    {
        static void Main(string[] args)
        {
            var input = new FunctionInput()
            {
                Deploy = true,
                InstanceIDs = new List<int>() { 1 }
            };
            Run(input);
        }

        public async static void Run(FunctionInput input)
        {
            try
            {
                LambdaLogger.Log("MyProfileOnly starting...");
                var service = new Service();
                var settings = service.GetSettings();
                IEnumerable<InstanceData> instances;
                if (input.Deploy)
                {
                    instances = Enumerable.Empty<InstanceData>();
                    instances = service.GetActiveInstances().Result;
                }
                else
                {
                    if (input.InstanceIDs.Count == 0)
                    {
                        LambdaLogger.Log("No specified instance ID input. Process aborted.");
                        return;
                    }
                    var instanceIDs = input.InstanceIDs;
                    LambdaLogger.Log("instances:" + string.Join(",", instanceIDs));
                    instances = service.GetInstancesByIDs(instanceIDs).Result;
                }

                await foreach (InstanceData instance in instances.ToAsyncEnumerable())
                {
                    LambdaLogger.Log(string.Format(@"BEGIN<---{0}:{1}---", instance.id, instance.database));

                    var userProfiles = service.GetUserProfilesWithoutEmail(instance).Result;

                    var userProfileCount = userProfiles.Count();
                    LambdaLogger.Log(string.Format(@"userProfiles={0}", userProfileCount));
                    if (userProfileCount > 0)
                    {
                        var invalidUserProfiles = userProfiles.Where(x => x.is_local == 0 && (x.profileEmail == null || x.profileEmail.Trim() == ""));
                        LambdaLogger.Log(string.Format(@"invalidUserProfiles={0}", invalidUserProfiles.Count()));

                        var profileOnly = new List<ProfileOnly>();
                        await foreach (UserProfile p in invalidUserProfiles.ToAsyncEnumerable())
                        {
                            var item = new ProfileOnly()
                            {
                                id_profile = p.id_profile
                            };
                            profileOnly.Add(item);
                        }
                        LambdaLogger.Log(string.Format(@"profileOnly={0}", profileOnly.Count()));
                        if (profileOnly.Count > 0)
                        {
                            var affected = service.BulkUpdate(instance, profileOnly).Result;
                            LambdaLogger.Log(string.Format(@"affected={0}", affected));
                        }
                    }
                    LambdaLogger.Log(string.Format(@"---{0}:{1}--->END", instance.id, instance.database));
                }
            }
            catch (System.Exception error)
            {
                LambdaLogger.Log(error.ToString());
                throw error;
            }
        }

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string FunctionHandler(FunctionInput input, ILambdaContext context)
        {
            Run(input);
            return "Done";
            // var message = input ?? "Hello from Lambda!";
            // // All log statements are written to CloudWatch by default. For more information, see
            // // https://docs.aws.amazon.com/lambda/latest/dg/nodejs-prog-model-logging.html
            // context.Logger.LogLine($"Processed message: {message}");
            // return message.ToUpper();
        }
    }
}
