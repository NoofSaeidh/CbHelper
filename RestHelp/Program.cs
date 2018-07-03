using Bogus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestHelp
{
    class Program
    {
        static void Main(string[] args)
        {
            var faker = new Faker();
            var endpoint = new EndpointInfo("http://localhost:444", "default", "18.200.001");
            using (var restClient = new AcumaticaRestClient(endpoint))
            {
                var loginInfo = new LoginInfo("admin", "123");
                using (new LoginLogout(restClient, loginInfo))
                {
                    try
                    {
                        var rand = new Random();
                        var summary = "catch_" + rand.Next();
                        var newActivity = new
                        {
                            Body = V("Value81dae4bc-1363-4989-81fe-84384121f19a"),
                            Date = V("2018-01-10T04:27:00"),
                            Internal = V(true),
                            Owner = V("EP00000004"),
                            Summary = V(summary),
                            Task = V("7843d0c7-c67e-e811-8340-d05099982024"),
                            TimeActivity = new
                            {
                                Billable = V(true),
                                BillableTime = V("01:00"),
                                EarningType = V("RG"),
                                Project = V("TMR0000001"),
                                ProjectTask = V("DEVELOP"),
                                Status = V("Open"),
                                TimeSpent = V("04:15"),
                                TrackTime = V(true),
                                Delete = V(false),
                            },
                            Type = V("M"),
                            Workgroup = V("Executive"),
                            Note = V("Note4a5ab6a3-eb56-4284-9cfa-700fc3118f43"),
                        };

                        dynamic activity = restClient.Put("Activity", newActivity);

                        var updateActivity = new
                        {
                            id = activity.id,

                            Body = V("24cc3455-0d79-4638-a168-e747c69b7d45"),
                            Date = V("2017-10-15T05:21:25.9671423"),
                            Internal = V(false),
                            Owner = V("EP00000004"),
                            Summary = V("93cfa55c-24db-4094-93cb-34c3c9e4d38a"),
                            TimeActivity = new
                            {
                                Billable = V(false),
                                EarningType = V("HL"),
                                Project = V("TMR0000002"),
                                ProjectTask = V("TESTING"),
                                Status = V("Open"),
                                TimeSpent = V("06:06"),
                                TrackTime = V(true),
                                Delete = V(false),
                            },
                            Type = V("C"),
                            Workgroup = V("CTO"),
                            Note = V("b731f580-cdcd-4ec3-bfb6-ace6f702e0ba"),
                            Delete = V(false),
                        };

                        dynamic resultActivity = restClient.Put("Activity", updateActivity);

                        //var tmp = restClient.Invoke("Email", "LinkEntityToActivity", new
                        //{

                        //    Entity = new
                        //    {
                        //        id = newEmail.id
                        //    },
                        //    Parameters = new
                        //    {
                        //        RelatedEntity = V("NEW"),
                        //        Type = V("PX.Objects.CR.BAccount")
                        //    }
                        //});
                    }
                    catch(RestException e)
                    {
                        var res = e.PXResponseMessage.ExceptionMessage;
                    }

                }
            }
        }
        [DebuggerStepThrough]
        static V V(object value) => new V(value);
    }
    public class V
    {
        [DebuggerStepThrough]
        public V(object value)
        {
            Value = value;
        }

        [JsonProperty("value")]
        public object Value { get; }
    }
}
