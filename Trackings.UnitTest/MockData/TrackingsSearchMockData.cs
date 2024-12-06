using System.Collections.Generic;
using Trackings.Domain.Trackings;
using System;
using System.Diagnostics;
using MongoDB.Bson;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal.Execution;

namespace Trackings.UnitTest.MockData
{
    public static class TrackingsSearchMockData
    {
        public static SearchTestStructure ChooseScenario(int scenario)
        {
            if(scenario == 1)
            {
                return GetScenario1();
            }
            else if(scenario == 2)
            {
                return GetScenario2();
            }
            else if (scenario == 3)
            {
                return GetScenario3();
            }
            else if (scenario == 4)
            {
                return GetScenario4();
            }
            else if (scenario == 5)
            {
                return GetScenario5();
            }
            else if (scenario == 6)
            {
                return GetScenario6();
            }
            
            return GetScenario7();
        }

        private static SearchTestStructure GetScenario1()
        {
            var trackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            List<Guid> loIds = new List<Guid>
            {
                Guid.NewGuid()
            };

            trackings.Context.LoId = loIds.First();

            return new SearchTestStructure
            {
                trackingList = new List<Tracking>() { trackings },
                trackingSearchRequest = new TrackingsSearchRequest { LoIds = loIds },
                expectedReturn = 1
            };
        }

        private static SearchTestStructure GetScenario2()
        {
            var trackings = TrackingsMockData.GetTrackings(Guid.NewGuid());

            List<Guid> loIds = new List<Guid>
            {
                Guid.NewGuid()
            };

            return new SearchTestStructure
            {
                trackingList = new List<Tracking>() { trackings },
                trackingSearchRequest = new TrackingsSearchRequest { LoIds = loIds },
                expectedReturn = 0
            };
        }

        private static SearchTestStructure GetScenario3()
        {
            List<Guid> loIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            List<Guid> tpIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            var trackingsList = new List<Tracking>();

            foreach (var loId in loIds)
            {
                foreach (var tpId in tpIds)
                {
                    var trackings = TrackingsMockData.GetTrackings(Guid.NewGuid());
                    trackings.Context.LoId = loId;
                    trackings.Context.TrainingProgramId = tpId;

                    trackingsList.Add(trackings);
                }
            }

            return new SearchTestStructure
            {
                trackingList = trackingsList,
                trackingSearchRequest = new TrackingsSearchRequest { LoIds = loIds },
                expectedReturn = 9
            };
        }

        private static SearchTestStructure GetScenario4()
        {
            var searchStructure = GetScenario3();
            var eventId = Guid.NewGuid();

            Random rnd = new Random();
            int n = rnd.Next(9);

            for(int i=0; i<=n; i++)
            {
                searchStructure.trackingList[i].Context.EventId = eventId;
            }
            searchStructure.expectedReturn = n + 1;
            searchStructure.trackingSearchRequest.EventIds = new List<Guid> { eventId };

            return searchStructure;
        }

        private static SearchTestStructure GetScenario5()
        {
            var searchStructure = GetScenario3();
            var attemptId = Guid.NewGuid();

            Random rnd = new Random();
            int n = rnd.Next(9);

            for (int i = 0; i <= n; i++)
            {
                searchStructure.trackingList[i].Context.AttemptId = attemptId;
            }
            searchStructure.expectedReturn = n + 1;
            searchStructure.trackingSearchRequest.AttemptIds = new List<Guid> { attemptId };

            return searchStructure;
        }

        private static SearchTestStructure GetScenario6()
        {
            var searchStructure = GetScenario3();
            var entitlementId = Guid.NewGuid();

            Random rnd = new Random();
            int n = rnd.Next(9);

            for (int i = 0; i <= n; i++)
            {
                searchStructure.trackingList[i].Context.EntitlementId = entitlementId;
            }
            searchStructure.expectedReturn = n + 1;
            searchStructure.trackingSearchRequest.EntitlementIds = new List<Guid> { entitlementId };

            return searchStructure;
        }

        private static SearchTestStructure GetScenario7()
        {
            var searchStructure = GetScenario3();
            var userId = 1000;

            Random rnd = new Random();
            int n = rnd.Next(9);

            for (int i = 0; i <= n; i++)
            {
                searchStructure.trackingList[i].UserId = userId;
            }
            searchStructure.expectedReturn = n+1;
            searchStructure.trackingSearchRequest.UserIds = new List<int> { userId };

            return searchStructure;
        }
    }

    public class SearchTestStructure
    {
        public List<Tracking> trackingList { get; set;}
        public TrackingsSearchRequest trackingSearchRequest { get; set;}
        public int expectedReturn { get; set; }
    }
}